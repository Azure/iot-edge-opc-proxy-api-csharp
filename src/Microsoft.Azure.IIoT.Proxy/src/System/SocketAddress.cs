// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

// Keep in sync with native layer, in particular order of members!

namespace System.Net.Proxy {
    using System.Runtime.Serialization;
    using Microsoft.Azure.IIoT.Proxy.Utils;

    /// <summary>
    /// Socket address base class
    /// </summary>
    [DataContract]
    public class SocketAddress : Poco<SocketAddress> {

        /// <summary>
        /// Address family
        /// </summary>
        [DataMember(Name = "family", Order = 1)]
        public virtual AddressFamily Family => AddressFamily.Unspecified;

        /// <summary>
        /// Comparison
        /// </summary>
        /// <param name="that"></param>
        /// <returns></returns>
        public override bool IsEqual(SocketAddress that) =>
            IsEqual(Family, that.Family);

        public virtual ProxySocketAddress AsProxySocketAddress() => null;

        /// <summary>
        /// Parse a string into a socket address
        /// </summary>
        /// <param name="address"></param>
        /// <param name="parsed"></param>
        /// <returns></returns>
        public static bool TryParse(string address, out SocketAddress parsed) {
            /**/ if (string.IsNullOrEmpty(address)) {
                parsed = new AnySocketAddress();
            }
            else if (InetSocketAddress.TryParse(address, out var inet)) {
                parsed = inet;
            }
            else if (ProxySocketAddress.TryParse(address, out var proxy)) {
                parsed = proxy;
            }
            else {
                parsed = new AnySocketAddress();
                return false;
            }
            return true;
        }

        protected override void SetHashCode() => MixToHash(Family);
    }
}