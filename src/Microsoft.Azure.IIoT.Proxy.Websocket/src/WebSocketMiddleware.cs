﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Provider {
    using Microsoft.Azure.IIoT.Proxy.Models;
    using Microsoft.Azure.IIoT.Proxy.Provider.Legacy;
    using Microsoft.Azure.IIoT.Proxy.Serializer;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Options;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    /// <summary>
    /// asp.net core middleware that provides streamprovider that uses websockets as stream service
    /// implementation.
    /// </summary>
    public class WebSocketMiddleware : IoTHubProvider, IStreamService {

        /// <summary>
        /// Exposes the new asp.net stream service on the default provider
        /// </summary>
        public override IStreamService StreamService => this;

        internal ConcurrentDictionary<Reference, WebSocketConnection> _connectionMap =
            new ConcurrentDictionary<Reference, WebSocketConnection>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        public WebSocketMiddleware(RequestDelegate next, IOptions<WebSocketOptions> options) :
            base(options.Value.IoTHubOwnerConnectionString) {
            _uri = options.Value.PublicEndpoint;
            _secure = _uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
            _next = next;
            DefaultProvider.Set(this);
        }

        /// <summary>
        /// Handle all websocket requests
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context) {
            try {
                if (context.WebSockets.IsWebSocketRequest && (!_secure || context.Request.IsHttps)) {
                    // Correlate the accepted socket to an open stream in our map
                    if (Reference.TryParse(context.Request.Path.Value.Trim('/'), out var streamId)) {
                        if (_connectionMap.TryGetValue(streamId, out var connection)) {
                            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            var server = connection.OpenAsync(new WebSocketStream(webSocket));
                            if (server != null) {
                                ProxyEventSource.Log.ConnectionAccepted(context);
                                await server.ConfigureAwait(false);
                                return; // Connected
                            }
                        }
                        ProxyEventSource.Log.ConnectionRejected(context, null);
                    }
                }
            }
            catch (Exception e) {
                // Some error occurred
                ProxyEventSource.Log.ConnectionRejected(context, e);
            }
            await _next(context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates new connection
        /// </summary>
        /// <param name="streamId">Local reference id of the stream</param>
        /// <param name="remoteId">Remote reference of link</param>
        /// <param name="proxy">The proxy server</param>
        /// <param name="encoding">The encoding to use</param>
        /// <returns></returns>
        public Task<IConnection> CreateConnectionAsync(Reference streamId,
            Reference remoteId, INameRecord proxy, CodecId encoding) {
            var uri = new UriBuilder(_uri) {
                Scheme = _secure ? "wss" : "ws",
                Path = streamId.ToString()
            };
            var connection = new WebSocketConnection(this, streamId, remoteId, encoding,
                ConnectionString.CreateWithEndpointAndToken(uri.Uri, "proxy", "secret"));
            _connectionMap.AddOrUpdate(streamId, connection, (r, s) => connection);
            return Task.FromResult((IConnection)connection);
        }

        private Uri _uri;
        private bool _secure;
        private RequestDelegate _next;
    }
}
