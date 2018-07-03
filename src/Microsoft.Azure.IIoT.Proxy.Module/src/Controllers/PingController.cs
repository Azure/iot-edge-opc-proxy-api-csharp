// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Module.Controllers {
    using Microsoft.Azure.IIoT.Proxy.Module.Filters;
    using Microsoft.Azure.IIoT.Proxy.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Edge;
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Proxy.Server;
    using System.Net.Proxy;

    /// <summary>
    /// Proxy controller handling ping protocol
    /// </summary>
    [Version(1)]
    [ExceptionsFilter]
    public class PingController : IMethodController {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="pinger"></param>
        /// <param name="logger"></param>
        public PingController(IResolverService pinger, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pinger = pinger ?? throw new ArgumentNullException(nameof(pinger));
        }

        /// <summary>
        /// Handle ping request
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<Message> PingAsync(Message message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            if (!(message.Content is PingRequest request)) {
                throw new ArgumentException("Must have ping request content.");
            }
            if (Reference.Null == message.Target) {
                throw new ArgumentException("Must have empty target.");
            }
            if (request.SocketAddress == null) {
                throw new ArgumentException(nameof(request.SocketAddress));
            }
            var found = await _pinger.ResolveAsync(request.SocketAddress);
            if (found != null) {
                return Message.Create(message, PingResponse.Create(found));
            }
            // Do not send back message, but raw exception...
            throw new SocketException(SocketError.NoHost);
        }

        private readonly ILogger _logger;
        private readonly IResolverService _pinger;
    }
}
