// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Client {
    using Microsoft.Azure.IIoT.Proxy.Exceptions;
    using Microsoft.Azure.IIoT.Proxy.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Proxy.Provider;
    using System.Net.Proxy;

    /// <summary>
    /// Concrete tcp proxy socket implementation
    /// </summary>
    internal class TCPServerSocket : BroadcastSocket {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="provider"></param>
        internal TCPServerSocket(SocketInfo info, IProvider provider) :
            base(info, provider) {

            if (Info.Type != SocketType.Stream) {
                throw new ArgumentException("Tcp only supports streams");
            }

            if (Info.Address == null) {
                Info.Address = new AnySocketAddress();
            }
            Info.Flags |= (uint)SocketFlags.Passive;
        }

        /// <summary>
        /// Select the proxy to bind to
        /// </summary>
        /// <param name="address"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public override Task BindAsync(SocketAddress address, CancellationToken ct) {
            if (_boundEndpoint != null) {
                throw new SocketException(
                    "Cannot double bind already bound socket. Use collection address.");
            }
            _boundEndpoint = address ?? throw new ArgumentNullException(nameof(address));

            while (_boundEndpoint.Family == AddressFamily.Bound) {
                // Unwrap bound address
                _boundEndpoint = ((BoundSocketAddress)_boundEndpoint).LocalAddress;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Start listening - creates connection
        /// </summary>
        /// <param name="backlog"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public override Task ListenAsync(int backlog, CancellationToken ct) {
            if (_boundEndpoint == null) {
                throw new SocketException("Must call bind before listen");
            }
            return LinkAsync(_boundEndpoint, ct);
        }

        public override Task ConnectAsync(SocketAddress address,
            CancellationToken ct) {
            throw new NotSupportedException("Cannot call connect on server socket");
        }

        public override Task<int> SendAsync(ArraySegment<byte> buffer, SocketAddress endpoint,
            CancellationToken ct) {
            throw new NotSupportedException("Cannot call send on server socket");
        }

        public override Task<ProxyAsyncResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken ct) {
            throw new NotSupportedException("Cannot call receive on server socket");
        }

        private SocketAddress _boundEndpoint;
    }
}