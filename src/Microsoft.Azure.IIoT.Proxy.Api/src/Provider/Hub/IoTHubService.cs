// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Provider.Legacy {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Proxy.Exceptions;
    using Microsoft.Azure.IIoT.Proxy.Models;
    using Microsoft.Azure.IIoT.Proxy.Serializer;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    /// <summary>
    /// Resolves names through registry manager and provides remoting and streaming
    /// through devices methods.
    /// </summary>
    public class IoTHubService : IRemotingService, INameService, IStreamService, IDisposable {

        internal const string _apiVersion = "2016-11-14";
        internal static readonly string _clientId = $"Microsoft.Azure.IIoT.Proxy/{VersionEx.Assembly}";

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="hubConnectionString"></param>
        public IoTHubService(ConnectionString hubConnectionString) :
            this(hubConnectionString.ToString()) {
        }

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="connectionString"></param>
        public IoTHubService(string connectionString = null) {
            if (string.IsNullOrEmpty(connectionString)) {
                connectionString = Environment.GetEnvironmentVariable("_HUB_CS");
            }
            if (string.IsNullOrEmpty(connectionString)) {
                throw new ArgumentException("You must provide the iothubowner connection " +
                    "string, which can be obtained from the Azure IoT Hub portal. You must " +
                    "then either initialize the DefaultProvider class, or set the _HUB_CS " +
                    "environment variable.");
            }
            _hubConnectionString = ConnectionString.Parse(connectionString);

            _updateQueue = new BufferBlock<Tuple<INameRecord, NameServiceOperation>>(
                new DataflowBlockOptions {
                    NameFormat = "Update (in IoTHubService) Id={1}",
                    EnsureOrdered = false,
                    MaxMessagesPerTask = DataflowBlockOptions.Unbounded,
                    CancellationToken = _open.Token
                });
            _listeners = new BroadcastBlock<Tuple<INameRecord, NameServiceEvent>>(null,
                new DataflowBlockOptions {
                    NameFormat = "Notify (in IoTHubService) Id={1}",
                    EnsureOrdered = false,
                    MaxMessagesPerTask = DataflowBlockOptions.Unbounded,
                    CancellationToken = _open.Token
                });

            Write = DataflowBlockEx.Encapsulate(_updateQueue, _listeners);

            _registryLoaded = new TaskCompletionSource<bool>();
            _cache = new ConcurrentDictionary<Reference, IoTHubRecord>();
            _cacheWorker = CacheWorker();
        }

        /// <summary>
        /// Close name service and release resources
        /// </summary>
        public void Dispose() {
            _open.Cancel();
            Write.Complete();
            _cacheWorker.Wait();
            _http.Dispose();
        }

        #region IStreamService

        /// <summary>
        /// Create stream connection through iot hub methods.
        /// </summary>
        /// <param name="streamId">Local reference address of the stream</param>
        /// <param name="remoteId">Remote reference of link</param>
        /// <param name="proxy">The proxy server</param>
        /// <returns></returns>
        public Task<IConnection> CreateConnectionAsync(Reference streamId,
            Reference remoteId, INameRecord proxy, CodecId encoding) {

            IConnection conn = new IoTHubStream(_hubConnectionString, streamId, remoteId, proxy, null);

            // TODO: Revisit:  At this point we could either a) look up a host from the registry
            // then use it to create a dedicated stream with connection string or b) create an
            // adhoc dr stream record in the registry.

            return Task.FromResult(conn);
        }

        #endregion

        #region IRemotingService

        /// <summary>
        /// Invoke method on proxy
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Message> CallAsync(INameRecord proxy, Message request, TimeSpan timeout,
            CancellationToken ct) {
            using (var invoker = new IoTHubInvoker(_hubConnectionString)) {
                return await invoker.CallAsync(proxy, request, timeout, ct);
            }
        }

        #endregion

        #region INameService

        /// <summary>
        /// The write target block exposed by the name service.
        /// </summary>
        public IPropagatorBlock<Tuple<INameRecord, NameServiceOperation>, Tuple<INameRecord, NameServiceEvent>> Write {
            get; private set;
        }

        /// <summary>
        /// Returns a query block that allows passing queries to. Name service lookup is
        /// implemented using a dictionary cache.  For more scalability this could be using
        /// linq to entities in a persisted name service database.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public IPropagatorBlock<Expression<Func<INameRecord, bool>>, INameRecord> Read(
            ExecutionDataflowBlockOptions options) =>
            new TransformManyBlock<Expression<Func<INameRecord, bool>>, INameRecord>(
                async query => {
                    await _registryLoaded.Task;
                    // Compile query and return ordered results - only connected
                    var result = _cache.Values.Where(r => !r.Disconnected).Where(
                        query.Compile()).OrderByDescending(k => k.LastActivity);
                    if (!result.Any()) {
                        throw new ProxyNotFound("No connected proxies found.");
                    }
                    return result;
                },
            options);

        /// <summary>
        /// Returns the query results async, but retries with exponential backoff when failure.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="continuation"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<Tuple<string, IEnumerable<IoTHubRecord>>> PagedLookupWithRetryAsync(
            string sql, string continuation, CancellationToken ct) {
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
#endif
            var result = await Retry.WithLinearBackoff(null, ct, () =>
                    PagedLookupAsync(sql, continuation, ct),
                _ => !ct.IsCancellationRequested).ConfigureAwait(false);
#if DEBUG
            System.Diagnostics.Trace.TraceInformation($" > {sw.Elapsed} < for Lookup of " +
                $"{sql} (cont: {continuation ?? "<none>"}) returned {result.Item2.Count()} records");
#endif
            return result;
        }

        /// <summary>
        /// Returns the query results async
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="continuation"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<Tuple<string, IEnumerable<IoTHubRecord>>> PagedLookupAsync(
            string sql, string continuation, CancellationToken ct) {
            if (string.IsNullOrEmpty(sql)) {
                throw ProxyEventSource.Log.ArgumentNull("sql");
            }
            try {
                var uri = new UriBuilder {
                    Scheme = "https",
                    Host = _hubConnectionString.HostName,
                    Path = "/devices/query",
                    Query = "api-version=" + _apiVersion
                };
                var stream = await _http.StreamAsync(
                    CreateUri("/devices/query"), Http.Post,
                    async h => {
                        h.Add(HttpRequestHeader.Authorization.ToString(),
                            await GetSasTokenAsync(_hubConnectionString,
                                3600).ConfigureAwait(false));
                        h.Add(HttpRequestHeader.UserAgent.ToString(), _clientId);

                        // Add previous continuation if any provided
                        if (!string.IsNullOrEmpty(continuation)) {
                            h.Add("x-ms-continuation", continuation);
                        }
                    }, (s, h) => {
                        // get continuation returned if any
                        if (h.TryGetValues("x-ms-continuation", out var values)) {
                            continuation = values.FirstOrDefault();
                        }
                        else {
                            continuation = null;
                        }
                    }, ct,
                    @"{""query"":""" + sql + @"""}", "application/json").ConfigureAwait(false);

                using (stream)
                using (var sr = new StreamReader(stream))
                using (JsonReader reader = new JsonTextReader(sr)) {
                    var results = JToken.ReadFrom(reader);
                    return Tuple.Create(continuation, results.Select(j => new IoTHubRecord((JObject)j)));
                }
            }
            catch (Exception e) {
                throw ProxyEventSource.Log.Rethrow(e, this);
            }
        }

        /// <summary>
        /// Creates a device in the IoT Hub device registry
        /// </summary>
        /// <param name="record"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task<IoTHubRecord> AddRecordAsync(INameRecord record,
            CancellationToken ct) {
            try {
                await _http.CallAsync(CreateUri("/devices/" + record.Id), Http.Put,
                    async h => {
                        h.Add(HttpRequestHeader.Authorization.ToString(),
                            await GetSasTokenAsync(_hubConnectionString,
                                3600).ConfigureAwait(false));
                        h.Add(HttpRequestHeader.UserAgent.ToString(), _clientId);
                    }, (sc, h) => {
                        if (sc == HttpStatusCode.Conflict || sc == HttpStatusCode.PreconditionFailed) {
                            throw new TransientException();
                        }
                    }, ct,
                    @"{""deviceId"": """ + record.Id + @"""}", "application/json").ConfigureAwait(false);

                ProxyEventSource.Log.RecordAdded(this, record);
                return new IoTHubRecord(record);
            }
            catch (Exception e) {
                if (e is ITransientException) {
                    // Retrieve the twin object and update it
                    var result = await GetRecordAsync(record, ct).ConfigureAwait(false);
                    if (result == null) {
                        return null;
                    }
                    result.Assign(record);
                    return result;
                }
                throw ProxyEventSource.Log.Rethrow(e, this);
            }
        }

        /// <summary>
        /// Retrieves a single device twin based record from IoT Hub
        /// </summary>
        /// <param name="record"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task<IoTHubRecord> GetRecordAsync(INameRecord record,
            CancellationToken ct) {
            try {
                var stream = await _http.StreamAsync(
                    CreateUri("/twins/" + record.Id), Http.Get,
                    async h => {
                        h.Add(HttpRequestHeader.Authorization.ToString(),
                            await GetSasTokenAsync(_hubConnectionString,
                                3600).ConfigureAwait(false));
                        h.Add(HttpRequestHeader.UserAgent.ToString(), _clientId);
                    }, (sc, h) => {
                        if (sc == HttpStatusCode.NotFound) {
                            throw new TransientException();
                        }
                    }, ct).ConfigureAwait(false);

                using (stream)
                using (var sr = new StreamReader(stream))
                using (JsonReader reader = new JsonTextReader(sr)) {
                    return new IoTHubRecord((JObject)JToken.ReadFrom(reader));
                }
            }
            catch (Exception e) {
                if (e is ITransientException) {
                    ProxyEventSource.Log.RecordRemoved(this, record);
                    return null;
                }
                throw ProxyEventSource.Log.Rethrow(e, this);
            }
        }

        /// <summary>
        /// Deletes an record in the IoT Hub device registry
        /// </summary>
        /// <param name="record"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task RemoveRecordAsync(INameRecord record,
            CancellationToken ct) {
            try {
                await _http.CallAsync(
                    CreateUri("/devices/" + record.Id), Http.Delete,
                    async h => {
                        h.Add(HttpRequestHeader.Authorization.ToString(),
                            await GetSasTokenAsync(_hubConnectionString,
                                3600).ConfigureAwait(false));
                        h.Add(HttpRequestHeader.UserAgent.ToString(), _clientId);
                        h.IfMatch.Add(new EntityTagHeaderValue(@"""*"""));
                    },
                    (sc, h) => { }, ct).ConfigureAwait(false);
            }
            catch (Exception e) {
                ProxyEventSource.Log.HandledExceptionAsInformation(this, e);
            }
        }

        /// <summary>
        /// Updates a record in the device registry
        /// </summary>
        /// <param name="record"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<IoTHubRecord> UpdateRecordAsync(
            INameRecord record, CancellationToken ct) {

            var hubRecord = record as IoTHubRecord;

            //
            // If the record is a generic record, add it first and retrieve the resulting
            // twin record from the registry.  Then assign the generic record to it and
            // see if anything needs patching...
            //
            if (hubRecord == null) {
                // Create and convert generic record into hub record
                hubRecord = await AddRecordAsync(record, ct).ConfigureAwait(false);
                if (hubRecord == null) {
                    return null;
                }
            }

            var json = hubRecord.Patch;
            if (string.IsNullOrEmpty(json)) {
                // Nothing to patch...
                return hubRecord;
            }

            //
            // If we logged changes to the record use the resulting patch json to patch
            // up the twin record.  If we do not find the record anymore as part of
            // patching it, then it was deleted, in which case return null.  Otherwise
            // return the returned patched up twin record
            //
            ProxyEventSource.Log.PatchingRecord(this, record, json);
            try {
                var stream = await _http.StreamAsync(
                    CreateUri("/twins/" + hubRecord.Id), Http.Patch,
                    async h => {
                        h.Add(HttpRequestHeader.Authorization.ToString(),
                            await GetSasTokenAsync(_hubConnectionString,
                                3600).ConfigureAwait(false));
                        h.Add(HttpRequestHeader.UserAgent.ToString(), _clientId);
                        h.IfMatch.Add(new EntityTagHeaderValue(@"""*"""));
                    },
                    (sc, h) => {
                        if (sc == HttpStatusCode.NotFound) {
                            throw new TransientException();
                        }
                    }, ct, json, "application/json").ConfigureAwait(false);
                using (stream)
                using (var sr = new StreamReader(stream))
                using (JsonReader reader = new JsonTextReader(sr)) {
                    hubRecord = new IoTHubRecord(new IoTHubRecord((JObject)JToken.ReadFrom(reader)));
                }
            }
            catch (Exception e) {
                if (e is ITransientException) {
                    ProxyEventSource.Log.RecordRemoved(this, record);
                    hubRecord = null;
                }
                else {
                    throw ProxyEventSource.Log.Rethrow(e, this);
                }
            }
            ProxyEventSource.Log.RecordPatched(this, record, json);
            return hubRecord;
        }

        /// <summary>
        /// Check whether the record is connected
        /// </summary>
        /// <param name="record"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task<bool> IsConnectedAsync(INameRecord record,
            CancellationToken ct) {
            try {
                var stream = await _http.StreamAsync(
                    CreateUri("/devices/" + record.Id), Http.Get,
                    async h => {
                        h.Add(HttpRequestHeader.Authorization.ToString(),
                            await GetSasTokenAsync(_hubConnectionString,
                                3600).ConfigureAwait(false));
                        h.Add(HttpRequestHeader.UserAgent.ToString(), _clientId);
                    }, (sc, h) => {
                        if (sc == HttpStatusCode.NotFound) {
                            throw new TransientException();
                        }
                    }, ct).ConfigureAwait(false);

                using (stream)
                using (var sr = new StreamReader(stream))
                using (JsonReader reader = new JsonTextReader(sr)) {
                    var response = (JObject)JToken.ReadFrom(reader);

                    return response["connectionState"].ToString().Equals(
                        "Connected", StringComparison.CurrentCultureIgnoreCase);
                }
            }
            catch (Exception e) {
                if (e is ITransientException) {
                    return false;
                }
                throw ProxyEventSource.Log.Rethrow(e, this);
            }
        }

        /// <summary>
        /// Returns all results from a query
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task ForeachRecordAsync(string sql, Action<IoTHubRecord> target,
            CancellationToken ct) {
            string continuation = null;
            do {
                var response = await PagedLookupWithRetryAsync(sql, continuation, ct).ConfigureAwait(false);
                foreach (var result in response.Item2) {
                    target(result);
                }
                continuation = response.Item1;
            }
            while (!string.IsNullOrEmpty(continuation) && !ct.IsCancellationRequested);
        }

        /// <summary>
        /// Create record type expression from type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string CreateQueryString(NameRecordType type, bool filterAlive = false) {
            var sql = new StringBuilder("SELECT * FROM devices WHERE ");
            var concat = false;

            // No support for bit queries ...
            if (0 != (type & NameRecordType.Proxy)) {
                sql.Append("(");
                sql.Append("tags.proxy=1");
                concat = true;
            }

            if (0 != (type & NameRecordType.Host)) {
                sql.Append(concat ? " OR " : "(");
                sql.Append("tags.host=1");
                concat = true;
            }

            if (0 != (type & NameRecordType.Link)) {
                sql.Append(concat ? " OR " : "(");
                sql.Append("tags.link=1");
                concat = true;
            }

            if (concat) {
                sql.Append(")");
                if (filterAlive) {
                    sql.Append(" AND ");
                }
            }
            if (filterAlive) {
                sql.Append("properties.reported.alive = 1");
            }
            return sql.ToString();
        }

        /// <summary>
        /// Reload cache every other minute.
        /// </summary>
        private static readonly TimeSpan kReloadTimeSpan = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Load a local in memory copy of all proxies and all hosts. The assumption
        /// is that the number of proxies that are "active" will be small. The number
        /// of hosts will be larger, however, we can prune the list later.
        /// </summary>
        private async Task CacheWorker() {
            try {
                // Load all proxies from iot hub
                await ForeachRecordAsync(CreateQueryString(NameRecordType.Proxy, true),
                    r => _cache.TryAdd(r.Address, r), _open.Token);
                // Load all hosts from iothub
                await ForeachRecordAsync(CreateQueryString(NameRecordType.Host),
                    r => _cache.TryAdd(r.Address, r), _open.Token);

                foreach (var item in _cache.Values) {
                    if (item.Type.HasFlag(NameRecordType.Proxy) &&
                        !await IsConnectedAsync(item, _open.Token)) {
                        item.Disconnected = true;
                    }
                    else {
                        item.Disconnected = false;
                    }
                }
            }
            catch (OperationCanceledException) {
                // Cancelled before fully loaded...
                return;
            }

            // Done loading all proxies and hosts
            _registryLoaded.SetResult(true);
            var reloadTimeout = kReloadTimeSpan;
            while (!_open.IsCancellationRequested) {

                // Update all items in the device registry that need updating
                try {
                    while (!_open.IsCancellationRequested) {
                        var now = DateTime.Now;
                        var item = await _updateQueue.ReceiveAsync(reloadTimeout, _open.Token);
                        if (item.Item2 != NameServiceOperation.Remove) {
                            var record = await UpdateRecordAsync(item.Item1, _open.Token).ConfigureAwait(false);
                            if (record == null) {
                                // TODO: Throw error and catch to log.
                            }
                            else {
                                // Update cache
                                var op = NameServiceEvent.Updated;
                                await NotifyChanges(_cache.AddOrUpdate(record.Address, v => {
                                    record.Disconnected = false;
                                    op = NameServiceEvent.Added;
                                    return record;
                                }, (k, v) => {
                                    record.Disconnected = v.Disconnected;
                                    return record;
                                }), op);
                            }
                        }
                        else {
                            await RemoveRecordAsync(item.Item1, _open.Token).ConfigureAwait(false);
                            if (_cache.TryRemove(item.Item1.Address, out var removed)) {
                                await NotifyChanges(removed, NameServiceEvent.Removed);
                            }
                        }
                        if (reloadTimeout <= kReloadTimeSpan) {
                            var passed = DateTime.Now - now;
                            if (passed < reloadTimeout) {
                                // Some time has passed, but pump more messages...
                                reloadTimeout -= passed;
                                continue;
                            }
                            // Reload now - timeout expired - but make sure to re-arm timer
                            reloadTimeout = kReloadTimeSpan;
                            break;
                        }
                        // Reset timer to original timespan
                        reloadTimeout = kReloadTimeSpan;
                    }
                }
                catch (TimeoutException) {
                    if (reloadTimeout > TimeSpan.FromHours(6)) {
                        // If we are idle for 6 hours, only refresh every week.
                        reloadTimeout = TimeSpan.FromDays(7);
                    }
                    else {
                        // Linearily increase timeout initially
                        reloadTimeout = reloadTimeout + kReloadTimeSpan;
                    }
                }
                catch (Exception) {
                    if (reloadTimeout < kReloadTimeSpan) {
                        reloadTimeout = kReloadTimeSpan;
                    }
                    continue;
                }

                ProxyEventSource.Log.RegistryRefresh(this, reloadTimeout);

                // Reload all hosts from device registry (which is our single source of truth)
                var cache = new Dictionary<Reference, IoTHubRecord>();
                try {
                    await ForeachRecordAsync(CreateQueryString(NameRecordType.Proxy, true),
                        r => cache.Add(r.Address, r), _open.Token);
                    await ForeachRecordAsync(CreateQueryString(NameRecordType.Host),
                        r => cache.Add(r.Address, r), _open.Token);
                }
                catch (OperationCanceledException) {
                    // Cancelled - will exit
                    continue;
                }

                // First remove all items not retrieved now
                foreach (var item in _cache.Values) {
                    if (!cache.ContainsKey(item.Address) &&
                        _cache.TryRemove(item.Address, out var removed)) {
                        await NotifyChanges(removed, NameServiceEvent.Removed);
                    }
                }

                // Then notify about changes
                foreach (var item in cache.Values) {
                    var op = NameServiceEvent.Updated;
                    await NotifyChanges(_cache.AddOrUpdate(item.Address, v => {
                        item.Disconnected = false;
                        op = NameServiceEvent.Added;
                        return item;
                    }, (k, v) => {
                        item.Disconnected = v.Disconnected;
                        return item;
                    }), op);
                }

                // Update status for each item in cache
                foreach (var item in _cache.Values) {
                    if (item.Type.HasFlag(NameRecordType.Proxy) &&
                        !await IsConnectedAsync(item, _open.Token)) {
                        if (!item.Disconnected) {
                            await NotifyChanges(item, NameServiceEvent.Disconnected);
                        }
                        item.Disconnected = true;
                    }
                    else {
                        if (item.Disconnected) {
                            await NotifyChanges(item, NameServiceEvent.Connected);
                        }
                        item.Disconnected = false;
                    }
                }
            }
        }

        /// <summary>
        /// Make a uri to the service
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private Uri CreateUri(string path) => new UriBuilder {
            Scheme = "https",
            Host = _hubConnectionString.HostName,
            Path = path,
            Query = "api-version=" + _apiVersion
        }.Uri;

        private Task NotifyChanges(INameRecord item, NameServiceEvent change) =>
            _listeners.SendAsync(Tuple.Create(item, change));

        private ConcurrentDictionary<Reference, IoTHubRecord> _cache;
        private Task _cacheWorker;
        private readonly Http _http = new Http();
        private BufferBlock<Tuple<INameRecord, NameServiceOperation>> _updateQueue;
        private BroadcastBlock<Tuple<INameRecord, NameServiceEvent>> _listeners;
        private readonly TaskCompletionSource<bool> _registryLoaded;

        #endregion

        /// <summary>
        /// Create a token for iothub from connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="validityPeriodInSeconds"></param>
        /// <returns></returns>
        internal static Task<string> GetSasTokenAsync(ConnectionString connectionString,
            int validityPeriodInSeconds) {
            // http://msdn.microsoft.com/en-us/library/azure/dn170477.aspx
            // signature is computed from joined encoded request Uri string and expiry string
            var expiryTime = DateTime.UtcNow + TimeSpan.FromSeconds(validityPeriodInSeconds);
            var expiry = ((long)(expiryTime -
                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            var encodedScope = Uri.EscapeDataString(connectionString.HostName);
            string sig;
            // the connection string signature is base64 encoded
            var key = Convert.FromBase64String(connectionString.SharedAccessKey);
            using (var hmac = new HMACSHA256(key)) {
                sig = Convert.ToBase64String(hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(encodedScope + "\n" + expiry)));
            }
            return Task.FromResult($"SharedAccessSignature sr={encodedScope}" +
                $"&sig={Uri.EscapeDataString(sig)}&se={Uri.EscapeDataString(expiry)}" +
                $"&skn={Uri.EscapeDataString(connectionString.SharedAccessKeyName)}");
        }


        private class TransientException : Exception, ITransientException { }
        private readonly ConnectionString _hubConnectionString;
        private CancellationTokenSource _open = new CancellationTokenSource();
    }
}