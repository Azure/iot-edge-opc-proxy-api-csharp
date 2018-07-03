// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Client {
    using Microsoft.Azure.IIoT.Proxy.Models;
    using Microsoft.Azure.IIoT.Proxy.Provider;
    using System.Net.Proxy;

    /// <summary>
    /// Factory of proxy sockets
    /// </summary>
    public class ProxySocketFactory : IProxySocketFactory {

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="provider"></param>
        public ProxySocketFactory(IProvider provider = null) {
            _provider = provider ?? DefaultProvider.Get();
        }

        /// <summary>
        /// Create proxy socket
        /// </summary>
        /// <param name="addressFamily"></param>
        /// <param name="protocolType"></param>
        /// <param name="socketType"></param>
        /// <param name="keepAlive"></param>
        /// <returns></returns>
        public IProxySocket Create(AddressFamily addressFamily,
            ProtocolType protocolType, SocketType socketType, uint keepAlive) {
            return ProxySocket.Create(new SocketInfo {
                Family = addressFamily,
                Protocol = protocolType,
                Type = socketType,
                Timeout = keepAlive
            }, _provider);
        }

        private IProvider _provider;
    }
}
