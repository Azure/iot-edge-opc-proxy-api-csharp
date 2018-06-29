// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Models {
    using System.Net.Proxy;

    public static class ProxyModelEx {

        /// <summary>
        /// Convert to system enum
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Reference AsReference(this Inet6SocketAddress address) => 
            new Reference(address.Address);
    }
}