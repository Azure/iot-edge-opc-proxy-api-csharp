// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Server {
    using System.Threading.Tasks;
    using System.Net.Proxy;

    public interface IResolverService {

        /// <summary>
        /// Find the specific socket address and return
        /// false if not found.
        /// </summary>
        /// <param name="socketAddress"></param>
        /// <returns></returns>
        Task<SocketAddress> ResolveAsync(SocketAddress socketAddress);
    }
}