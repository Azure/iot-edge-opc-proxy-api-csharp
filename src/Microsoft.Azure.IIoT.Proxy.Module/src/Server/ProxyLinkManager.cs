//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Server {
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Proxy.Models;
    using System.Collections.Concurrent;
    using System.Net.Proxy;
    using System.Threading.Tasks;

    public class ProxyLinkManager : IProxyLinkManager {

        /// <summary>
        /// Local proxy id
        /// </summary>
        Reference LocalId { get; } = Reference.Get();

        /// <summary>
        /// Create link manager
        /// </summary>
        /// <param name="restrictions"></param>
        public ProxyLinkManager(PortRange restrictions) {
            _restrictions = restrictions;
            _links = new ConcurrentDictionary<Reference, ProxyLink>();
        }

        /// <summary>
        /// Create link
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public async Task<IProxyLink> CreateAsync(SocketInfo info) {
            var socket = new ProxyLink(info, null); // TODO
            await socket.OpenAsync();
            if (!_links.TryAdd(socket.LocalId, socket)) {
                throw new SocketException(SocketError.Fault);
            }
            return socket;
        }

        /// <summary>
        /// Open and attach stream
        /// </summary>
        /// <param name="localId"></param>
        /// <param name="remoteId"></param>
        /// <param name="connectionString"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public Task OpenAsync(Reference localId, Reference remoteId,
            string connectionString, int encoding) {
            if (!_links.TryGetValue(localId, out var socket)) {
                throw new SocketException(SocketError.Closed);
            }

            // TODO Now attach stream to "remoteId"

            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get option
        /// </summary>
        /// <param name="localId"></param>
        /// <param name="socketOption"></param>
        /// <returns></returns>
        public Task<IProperty> GetOptionAsync(Reference localId,
            SocketOption socketOption) {
            if (!_links.TryGetValue(localId, out var socket)) {
                throw new SocketException(SocketError.Closed);
            }
            var result = socket.GetOption(socketOption);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Set option
        /// </summary>
        /// <param name="localId"></param>
        /// <param name="optionValue"></param>
        /// <returns></returns>
        public Task SetOptionAsync(Reference localId, IProperty optionValue) {
            if (!_links.TryGetValue(localId, out var socket)) {
                throw new SocketException(SocketError.Closed);
            }
            socket.SetOption(optionValue);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Close socket
        /// </summary>
        /// <param name="localId"></param>
        /// <returns></returns>
        public Task<ProxyLinkStats> CloseAsync(Reference localId) {
            if (!_links.TryRemove(localId, out var socket)) {
                throw new SocketException(SocketError.Closed);
            }
            var stats = socket.Stats;
            socket.Dispose();
            return Task.FromResult(stats);
        }

        private readonly PortRange _restrictions;
        private readonly ConcurrentDictionary<Reference, ProxyLink> _links;

    }
}
