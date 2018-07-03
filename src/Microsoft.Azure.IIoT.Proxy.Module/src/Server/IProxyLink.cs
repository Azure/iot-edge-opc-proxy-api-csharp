// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Server {
    using Microsoft.Azure.IIoT.Proxy.Models;
    using System.Net.Proxy;

    public interface IProxyLink {

        /// <summary>
        /// Local link id
        /// </summary>
        Reference LocalId { get; }

        /// <summary>
        /// Remote link id
        /// </summary>
        Reference RemoteId { get; }

        /// <summary>
        /// Local address on remote side
        /// </summary>
        SocketAddress LocalAddress { get; }

        /// <summary>
        /// Peer address on remote side
        /// </summary>
        SocketAddress PeerAddress { get; }
    }
}