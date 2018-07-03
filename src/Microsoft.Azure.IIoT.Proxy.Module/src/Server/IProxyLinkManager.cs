// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Server {
    using Microsoft.Azure.IIoT.Proxy.Models;
    using System.Net.Proxy;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages links
    /// </summary>
    public interface IProxyLinkManager {

        /// <summary>
        /// Create link
        /// </summary>
        /// <param name="info"></param>
        /// <returns>information about the link</returns>
        Task<IProxyLink> CreateAsync(SocketInfo info);

        /// <summary>
        /// Open link attaching to referenced stream
        /// </summary>
        /// <param name="link"></param>
        /// <param name="stream"></param>
        /// <param name="connectionString"></param>
        /// <param name="encoding"></param>
        Task OpenAsync(Reference link, Reference stream,
            string connectionString, int encoding);

        /// <summary>
        /// Set option on socket with handle
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="optionValue"></param>
        Task SetOptionAsync(Reference stream,
            IProperty optionValue);

        /// <summary>
        /// Get option on socket with handle
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="socketOption"></param>
        Task<IProperty> GetOptionAsync(Reference stream,
            SocketOption socketOption);

        /// <summary>
        /// Close socket with handle
        /// </summary>
        /// <param name="stream"></param>
        Task<ProxyLinkStats> CloseAsync(Reference stream);
    }
}