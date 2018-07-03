// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Client {
    using System.Net.Proxy;

    /// <summary>
    /// Factory of proxy sockets
    /// </summary>
    public interface IProxySocketFactory {

        /// <summary>
        /// Create proxy socket
        /// </summary>
        /// <param name="addressFamily"></param>
        /// <param name="protocolType"></param>
        /// <param name="socketType"></param>
        /// <param name="keepAlive"></param>
        /// <returns></returns>
        IProxySocket Create(AddressFamily addressFamily,
            ProtocolType protocolType, SocketType socketType, uint keepAlive);
    }
}
