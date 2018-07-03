// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Proxy {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public static class SocketExtensions {

        /// <summary>
        /// Convert to system enum
        /// </summary>
        /// <param name="family"></param>
        /// <returns></returns>
        public static Sockets.AddressFamily ToSocketsAddressFamily(this AddressFamily family) {
            switch(family) {
                case AddressFamily.InterNetwork:
                    return Sockets.AddressFamily.InterNetwork;
                case AddressFamily.InterNetworkV6:
                    return Sockets.AddressFamily.InterNetworkV6;
                case AddressFamily.Unix:
                    return Sockets.AddressFamily.Unix;
                case AddressFamily.Unspecified:
                    return Sockets.AddressFamily.Unspecified;
                case AddressFamily.Bound:
                case AddressFamily.Collection:
                case AddressFamily.Proxy:
                    return (Sockets.AddressFamily)family;
                default:
                    return Sockets.AddressFamily.Unknown;
            }
        }

        /// <summary>
        /// Convert to model enum
        /// </summary>
        /// <param name="family"></param>
        /// <returns></returns>
        public static AddressFamily ToProxyAddressFamily(this Sockets.AddressFamily family) {
            switch (family) {
                case Sockets.AddressFamily.InterNetwork:
                    return AddressFamily.InterNetwork;
                case Sockets.AddressFamily.InterNetworkV6:
                    return AddressFamily.InterNetworkV6;
                case Sockets.AddressFamily.Unix:
                    return AddressFamily.Unix;
                case Sockets.AddressFamily.Unspecified:
                    return AddressFamily.Unspecified;
                default:
                    return (AddressFamily)family;
            }
        }

        /// <summary>
        /// Convert to system enum
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Sockets.ProtocolType ToSocketsProtocolType(this ProtocolType type) {
            switch (type) {
                case ProtocolType.Tcp:
                    return Sockets.ProtocolType.Tcp;
                case ProtocolType.Udp:
                    return Sockets.ProtocolType.Udp;
                case ProtocolType.Icmpv6:
                    return Sockets.ProtocolType.IcmpV6;
                case ProtocolType.Icmp:
                    return Sockets.ProtocolType.Icmp;
                case ProtocolType.Unspecified:
                    return Sockets.ProtocolType.Unspecified;
                default:
                    return Sockets.ProtocolType.Unknown;
            }
        }

        /// <summary>
        /// Convert to model enum
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ProtocolType ToProxyProtocolType(this Sockets.ProtocolType type) {
            switch (type) {
                case Sockets.ProtocolType.Tcp:
                    return ProtocolType.Tcp;
                case Sockets.ProtocolType.Udp:
                    return ProtocolType.Udp;
                case Sockets.ProtocolType.IcmpV6:
                    return ProtocolType.Icmpv6;
                case Sockets.ProtocolType.Icmp:
                    return ProtocolType.Icmp;
                case Sockets.ProtocolType.Unspecified:
                    return ProtocolType.Unspecified;
                default:
                    return (ProtocolType)type;
            }
        }

        /// <summary>
        /// Convert to system enum
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Sockets.SocketType ToSocketsSocketType(this SocketType type) {
            switch (type) {
                case SocketType.Dgram:
                    return Sockets.SocketType.Dgram;
                case SocketType.Raw:
                    return Sockets.SocketType.Raw;
                case SocketType.RDM:
                    return Sockets.SocketType.Rdm;
                case SocketType.SeqPacket:
                    return Sockets.SocketType.Seqpacket;
                case SocketType.Stream:
                    return Sockets.SocketType.Stream;
                default:
                    return Sockets.SocketType.Unknown;
            }
        }

        /// <summary>
        /// Convert to model enum
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static SocketType ToProxySocketType(this Sockets.SocketType type) {
            switch (type) {
                case Sockets.SocketType.Dgram:
                    return SocketType.Dgram;
                case Sockets.SocketType.Raw:
                    return SocketType.Raw;
                case Sockets.SocketType.Rdm:
                    return SocketType.RDM;
                case Sockets.SocketType.Seqpacket:
                    return SocketType.SeqPacket;
                case Sockets.SocketType.Stream:
                    return SocketType.Stream;
                default:
                    return (SocketType)type;
            }
        }

        /// <summary>
        /// Returns socket error from the or any of the inner exceptions
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static Sockets.SocketError GetSocketsSocketError(this Exception ex) {
            var s = ex.GetFirstOf<Sockets.SocketException>();
            return s != null ? s.SocketErrorCode : Sockets.SocketError.SocketError;
        }

        /// <summary>
        /// Returns socket error from the or any of the inner exceptions
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static SocketError GetProxySocketError(this Exception ex) {
            var s = ex.GetFirstOf<SocketException>();
            return s != null ? s.ProxyErrorCode : SocketError.Fatal;
        }

        /// <summary>
        /// Convert to system error
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public static Sockets.SocketError ToSocketsSocketError(this SocketError error) {
            switch(error) {
                case SocketError.Arg:
                    return Sockets.SocketError.InvalidArgument;
                case SocketError.Fault:
                    return Sockets.SocketError.Fault;
                case SocketError.OutOfMemory:
                    return Sockets.SocketError.NoBufferSpaceAvailable;
                case SocketError.NotFound:
                    return Sockets.SocketError.TypeNotFound;
                case SocketError.NotSupported:
                    return Sockets.SocketError.OperationNotSupported;
                case SocketError.Permission:
                    return Sockets.SocketError.AccessDenied;
                case SocketError.Retry:
                    return Sockets.SocketError.TryAgain;
                case SocketError.Network:
                    return Sockets.SocketError.NetworkDown;
                case SocketError.Connecting:
                    return Sockets.SocketError.ConnectionAborted;
                case SocketError.Waiting:
                    return Sockets.SocketError.IOPending;
                case SocketError.Timeout:
                    return Sockets.SocketError.TimedOut;
                case SocketError.Aborted:
                    return Sockets.SocketError.Interrupted;
                case SocketError.Closed:
                    return Sockets.SocketError.NotConnected;
                case SocketError.Shutdown:
                    return Sockets.SocketError.Shutdown;
                case SocketError.Refused:
                    return Sockets.SocketError.ConnectionRefused;
                case SocketError.NoAddress:
                    return Sockets.SocketError.AddressNotAvailable;
                case SocketError.NoHost:
                    return Sockets.SocketError.HostUnreachable;
                case SocketError.HostUnknown:
                    return Sockets.SocketError.HostNotFound;
                case SocketError.AddressFamily:
                    return Sockets.SocketError.AddressFamilyNotSupported;
                case SocketError.Reset:
                    return Sockets.SocketError.ConnectionReset;
                case SocketError.Success:
                    return Sockets.SocketError.Success;
                case SocketError.Duplicate:
                case SocketError.BadFlags:
                case SocketError.InvalidFormat:
                case SocketError.DiskIo:
                case SocketError.Missing:
                case SocketError.PropGet:
                case SocketError.PropSet:
                case SocketError.Undelivered:
                case SocketError.Crypto:
                case SocketError.Comm:
                case SocketError.Nomore:
                case SocketError.NotImpl:
                case SocketError.AlreadyExists:
                case SocketError.BadState:
                case SocketError.Busy:
                case SocketError.Writing:
                case SocketError.Reading:
                    return (Sockets.SocketError)error;
                case SocketError.Fatal:
                default:
                    return Sockets.SocketError.SocketError;
            }
        }

        /// <summary>
        /// Convert to ip endpoint
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<IPEndPoint>> ToIPEndPointsAsync(
            this SocketAddress address) {
            switch (address) {
                case BoundSocketAddress ba:
                    return await ToIPEndPointsAsync(ba.RemoteAddress);
                case SocketAddressCollection ca:
                    var endpoints = Enumerable.Empty<IPEndPoint>();
                    foreach (var a in ca.Addresses()) {
                        try {
                            var eps = await ToIPEndPointsAsync(a);
                            endpoints = endpoints.Concat(eps);
                        }
                        catch {
                            continue;
                        }
                    }
                    break;
                case AnySocketAddress ai:
                    return new IPEndPoint(IPAddress.Any, 0).YieldReturn();
                case Inet6SocketAddress i6:
                    return new IPEndPoint(new IPAddress(i6.Address), i6.Port).YieldReturn();
                case Inet4SocketAddress i4:
                    return new IPEndPoint(new IPAddress(i4.Address), i4.Port).YieldReturn();
                case ProxySocketAddress pa:
                    var addresses = await Dns.GetHostAddressesAsync(pa.Host);
                    return addresses.Select(a => new IPEndPoint(a, pa.Port));
            }
            throw new ArgumentException("Bad address");
        }

        /// <summary>
        /// Convert ip endpoint to socket address
        /// </summary>
        /// <param name="ep"></param>
        /// <returns></returns>
        public static SocketAddress ToSocketAddress(this EndPoint ep) {
            if (ep is IPEndPoint endpoint) {
                if (endpoint.AddressFamily == Sockets.AddressFamily.InterNetwork) {
                    return new Inet4SocketAddress(endpoint.Address.GetAddressBytes(),
                        (ushort)endpoint.Port);
                }
                if (endpoint.AddressFamily == Sockets.AddressFamily.InterNetworkV6) {
                    return new Inet6SocketAddress(endpoint.Address.GetAddressBytes(),
                        (ushort)endpoint.Port, 0, (uint)endpoint.Address.ScopeId);
                }
            }
            throw new ArgumentException("Bad address");
        }

        /// <summary>
        /// Convert to ip endpoint
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static ushort GetPort(this SocketAddress address) {
            switch (address) {
                case BoundSocketAddress ba:
                    return GetPort(ba.RemoteAddress);
                case SocketAddressCollection ca:
                    foreach (var a in ca.Addresses()) {
                        var p = GetPort(a);
                        if (p != 0) {
                            return p;
                        }
                    }
                    break;
                case InetSocketAddress ia:
                    return ia.Port;
            }
            return 0;
        }
    }
}