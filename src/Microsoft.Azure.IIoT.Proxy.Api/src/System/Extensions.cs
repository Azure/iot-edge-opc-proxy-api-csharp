// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Proxy {
    using System;

    public static class Extensions {

        /// <summary>
        /// Convert to system enum
        /// </summary>
        /// <param name="family"></param>
        /// <returns></returns>
        public static System.Net.Sockets.AddressFamily ToSystem(this AddressFamily family) {
            switch(family) {
                case AddressFamily.InterNetwork:
                    return System.Net.Sockets.AddressFamily.InterNetwork;
                case AddressFamily.InterNetworkV6:
                    return System.Net.Sockets.AddressFamily.InterNetworkV6;
                case AddressFamily.Unix:
                    return System.Net.Sockets.AddressFamily.Unix;
                case AddressFamily.Unspecified:
                    return System.Net.Sockets.AddressFamily.Unspecified;
                case AddressFamily.Bound:
                case AddressFamily.Collection:
                case AddressFamily.Proxy:
                    return (System.Net.Sockets.AddressFamily)family;
                default:
                    return System.Net.Sockets.AddressFamily.Unknown;
            }
        }

        /// <summary>
        /// Convert to model enum
        /// </summary>
        /// <param name="family"></param>
        /// <returns></returns>
        public static AddressFamily ToModel(this System.Net.Sockets.AddressFamily family) {
            switch (family) {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    return AddressFamily.InterNetwork;
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    return AddressFamily.InterNetworkV6;
                case System.Net.Sockets.AddressFamily.Unix:
                    return AddressFamily.Unix;
                case System.Net.Sockets.AddressFamily.Unspecified:
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
        public static System.Net.Sockets.ProtocolType ToSystem(this ProtocolType type) {
            switch (type) {
                case ProtocolType.Tcp:
                    return System.Net.Sockets.ProtocolType.Tcp;
                case ProtocolType.Udp:
                    return System.Net.Sockets.ProtocolType.Udp;
                case ProtocolType.Icmpv6:
                    return System.Net.Sockets.ProtocolType.IcmpV6;
                case ProtocolType.Icmp:
                    return System.Net.Sockets.ProtocolType.Icmp;
                case ProtocolType.Unspecified:
                    return System.Net.Sockets.ProtocolType.Unspecified;
                default:
                    return System.Net.Sockets.ProtocolType.Unknown;
            }
        }

        /// <summary>
        /// Convert to model enum
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ProtocolType ToModel(this System.Net.Sockets.ProtocolType type) {
            switch (type) {
                case System.Net.Sockets.ProtocolType.Tcp:
                    return ProtocolType.Tcp;
                case System.Net.Sockets.ProtocolType.Udp:
                    return ProtocolType.Udp;
                case System.Net.Sockets.ProtocolType.IcmpV6:
                    return ProtocolType.Icmpv6;
                case System.Net.Sockets.ProtocolType.Icmp:
                    return ProtocolType.Icmp;
                case System.Net.Sockets.ProtocolType.Unspecified:
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
        public static System.Net.Sockets.SocketType ToSystem(this SocketType type) {
            switch (type) {
                case SocketType.Dgram:
                    return System.Net.Sockets.SocketType.Dgram;
                case SocketType.Raw:
                    return System.Net.Sockets.SocketType.Raw;
                case SocketType.RDM:
                    return System.Net.Sockets.SocketType.Rdm;
                case SocketType.SeqPacket:
                    return System.Net.Sockets.SocketType.Seqpacket;
                case SocketType.Stream:
                    return System.Net.Sockets.SocketType.Stream;
                default:
                    return System.Net.Sockets.SocketType.Unknown;
            }
        }

        /// <summary>
        /// Convert to model enum
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static SocketType ToModel(this System.Net.Sockets.SocketType type) {
            switch (type) {
                case System.Net.Sockets.SocketType.Dgram:
                    return SocketType.Dgram;
                case System.Net.Sockets.SocketType.Raw:
                    return SocketType.Raw;
                case System.Net.Sockets.SocketType.Rdm:
                    return SocketType.RDM;
                case System.Net.Sockets.SocketType.Seqpacket:
                    return SocketType.SeqPacket;
                case System.Net.Sockets.SocketType.Stream:
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
        public static System.Net.Sockets.SocketError GetSystemError(this Exception ex) {
            var s = ex.GetFirstOf<System.Net.Sockets.SocketException>();
            return s != null ? s.SocketErrorCode : System.Net.Sockets.SocketError.SocketError;
        }

        /// <summary>
        /// Returns socket error from the or any of the inner exceptions
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static SocketError GetSocketError(this Exception ex) {
            var s = ex.GetFirstOf<SocketException>();
            return s != null ? s.ProxyErrorCode : SocketError.Fatal;
        }

        /// <summary>
        /// Convert to system error
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public static System.Net.Sockets.SocketError ToSystem(this SocketError error) {
            switch(error) {
                case SocketError.Arg:
                    return System.Net.Sockets.SocketError.InvalidArgument;
                case SocketError.Fault:
                    return System.Net.Sockets.SocketError.Fault;
                case SocketError.OutOfMemory:
                    return System.Net.Sockets.SocketError.NoBufferSpaceAvailable;
                case SocketError.NotFound:
                    return System.Net.Sockets.SocketError.TypeNotFound;
                case SocketError.NotSupported:
                    return System.Net.Sockets.SocketError.OperationNotSupported;
                case SocketError.Permission:
                    return System.Net.Sockets.SocketError.AccessDenied;
                case SocketError.Retry:
                    return System.Net.Sockets.SocketError.TryAgain;
                case SocketError.Network:
                    return System.Net.Sockets.SocketError.NetworkDown;
                case SocketError.Connecting:
                    return System.Net.Sockets.SocketError.ConnectionAborted;
                case SocketError.Waiting:
                    return System.Net.Sockets.SocketError.IOPending;
                case SocketError.Timeout:
                    return System.Net.Sockets.SocketError.TimedOut;
                case SocketError.Aborted:
                    return System.Net.Sockets.SocketError.Interrupted;
                case SocketError.Closed:
                    return System.Net.Sockets.SocketError.NotConnected;
                case SocketError.Shutdown:
                    return System.Net.Sockets.SocketError.Shutdown;
                case SocketError.Refused:
                    return System.Net.Sockets.SocketError.ConnectionRefused;
                case SocketError.NoAddress:
                    return System.Net.Sockets.SocketError.AddressNotAvailable;
                case SocketError.NoHost:
                    return System.Net.Sockets.SocketError.HostUnreachable;
                case SocketError.HostUnknown:
                    return System.Net.Sockets.SocketError.HostNotFound;
                case SocketError.AddressFamily:
                    return System.Net.Sockets.SocketError.AddressFamilyNotSupported;
                case SocketError.Reset:
                    return System.Net.Sockets.SocketError.ConnectionReset;
                case SocketError.Success:
                    return System.Net.Sockets.SocketError.Success;
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
                    return (System.Net.Sockets.SocketError)error;
                case SocketError.Fatal:
                default:
                    return System.Net.Sockets.SocketError.SocketError;
            }
        }
    }
}