//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Proxy.Server {
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Proxy.Models;
    using System;
    using System.Net;
    using System.Net.Proxy;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using ProxyException = System.Net.Proxy.SocketException;
    using ProxyError = System.Net.Proxy.SocketError;
    using ProxyFlags = System.Net.Proxy.SocketFlags;
    using ProxyAddress = System.Net.Proxy.SocketAddress;

    /// <summary>
    /// Proxy socket / link at edge
    /// </summary>
    public class ProxyLink : IProxyLink, IDisposable {

        /// <summary>
        /// Local link id
        /// </summary>
        public Reference LocalId { get; } = Reference.Get();

        /// <summary>
        /// Remote link id
        /// </summary>
        public Reference RemoteId { get; set; }

        /// <summary>
        /// Local address on remote side
        /// </summary>
        public ProxyAddress LocalAddress => _socket.LocalEndPoint.ToSocketAddress();

        /// <summary>
        /// Peer address on remote side
        /// </summary>
        public ProxyAddress PeerAddress => _socket.RemoteEndPoint.ToSocketAddress();

        /// <summary>
        /// Stats
        /// </summary>
        public ProxyLinkStats Stats { get; } = new ProxyLinkStats();

        /// <summary>
        /// Information for this socket, exchanged with proxy server
        /// </summary>
        public SocketInfo Info { get; }

        /// <summary>
        /// Time last active
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Create socket
        /// </summary>
        /// <param name="props"></param>
        /// <param name="itf"></param>
        public ProxyLink(SocketInfo props, NetInterface itf) {

            // Check if we need to block link requests for connection to restricted ports
            if (0 == (props.Flags & (uint)ProxyFlags.Internal) &&
                0 == (props.Flags & (uint)ProxyFlags.Passive)) {
                CheckRestrictedPort(props.Address.GetPort());
            }

            Info = props;

            _timeout = Math.Max(props.Timeout, 30000);
            _passive = 0 != (props.Flags & (uint)ProxyFlags.Passive);
            _socket = new Socket(props.Family.ToSocketsAddressFamily(),
                props.Type.ToSocketsSocketType(), props.Protocol.ToSocketsProtocolType());

            // Bind socket to network interface
            if (_passive) {
                _socket.Bind(new IPEndPoint(itf?.UnicastAddress ??
                    IPAddress.Any, props.Address.GetPort()));
            }
            else if (itf != null) {
                _socket.Bind(new IPEndPoint(itf.UnicastAddress,
                    0));
            }

            foreach (var option in props.Options) {
                SetOption(option);
            }

            LastActivity = DateTime.Now;
        }

        /// <summary>
        /// Set options
        /// </summary>
        /// <param name="option"></param>
        public void SetOption(IProperty option) {
            if (option == null) {
                throw new ArgumentException(nameof(option));
            }

            if (!_socket.Connected && !_socket.IsBound) {
                throw new ProxyException(ProxyError.Closed);
            }
            /*
            if (so_val->type == prx_so_ip_multicast_join) {
                result = pal_socket_join_multicast_group(server_sock->sock,
                    &so_val->property.mcast);
                if (result != er_ok)
                    break;
                log_trace(server_sock->log, "Joined multicast group...");
            }
            else if (so_val->type == prx_so_ip_multicast_leave) {
                result = pal_socket_leave_multicast_group(server_sock->sock,
                    &so_val->property.mcast);
                if (result != er_ok)
                    break;
                log_trace(server_sock->log, "Left multicast group...");
            }
            else if (so_val->type == prx_so_props_timeout) {
                server_sock->client_itf.props.timeout =
                    so_val->property.value;
                result = er_ok;
                log_trace(server_sock->log, "Wrote socket gc timeout as %ull...",
                    so_val->property.value);
            }
            else if (so_val->type < __prx_so_max) {
                result = pal_socket_setsockopt(server_sock->sock,
                    (prx_socket_option_t)so_val->type,
                    so_val->property.value);
                if (result != er_ok)
                    break;
                log_trace(server_sock->log, "Wrote socket option %d as %ull...",
                    so_val->type,
                    so_val->property.value);
            }
            else {
                result = er_not_supported;
                break;
            }
            */
            throw new ProxyException(ProxyError.NotSupported);
        }

        public IProperty GetOption(SocketOption option) {
            if (!_socket.Connected && !_socket.IsBound) {
                throw new ProxyException(ProxyError.Closed);
            }
            /*
            do {

                if (so_opt == prx_so_ip_multicast_join ||
               so_opt == prx_so_ip_multicast_leave) {
                    result = er_not_supported;
                    break;
                }
                else if (so_opt == prx_so_props_timeout) {
                    so_val->property.value = server_sock->client_itf.props.timeout;
                    result = er_ok;
                }
                else if (so_opt < __prx_so_max) {
                    result = pal_socket_getsockopt(server_sock->sock, so_opt,
                        &so_val->property.value);
                    if (result != er_ok)
                        break;

                    log_trace(server_sock->log, "Read socket option %d as %ull...",
                        so_opt, so_val->property.value);
                }
                else {
                    log_error(server_sock->log, "Unsupported option type %d...", so_opt);
                    result = er_not_supported;
                    break;
                }

                so_val->type = so_opt;
            } while (0);
            return result;
            */
            throw new ProxyException(ProxyError.NotSupported);
        }

        /// <summary>
        /// Try to open/connect socket
        /// </summary>
        /// <returns></returns>
        public async Task OpenAsync() {
            if (_socket.Connected) {
                throw new ProxyException(ProxyError.BadState);
            }
            if (_passive) {
                if (_socket.ProtocolType == System.Net.Sockets.ProtocolType.Tcp) {
                    // start listen
                    _socket.Listen(50); // TODO
                }
                return;
            }
            var remoteEndpoints = await Info.Address.ToIPEndPointsAsync();
            foreach (var ep in remoteEndpoints) {
                try {
                    await _socket.ConnectAsync(ep);
                    return;
                }
                catch {
                    continue;
                }
            }
            throw new ProxyException(ProxyError.Connecting);
        }

        /// <summary>
        /// Close
        /// </summary>
        public void Dispose() {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(_socket));
            }
            lock (_socket) {
                if (!_disposed) {
                    _socket.SafeDispose();
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Helper to restrict port
        /// </summary>
        /// <param name="port"></param>
        private void CheckRestrictedPort(ushort port) {
            if (port != 0) {
            }
        }

        private readonly Socket _socket;
        private readonly ulong _timeout;
        private readonly bool _passive;
        private bool _disposed;
    }
}

