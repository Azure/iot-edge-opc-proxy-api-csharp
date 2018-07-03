// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Server {
    public class ProxyLinkStats {

        /// <summary>
        /// How long the link was open
        /// </summary>
        public ulong TimeOpenInMilliseconds {
            get; set;
        }

        /// <summary>
        /// How many bytes were sent
        /// </summary>
        public ulong BytesSent {
            get; set;
        }

        /// <summary>
        /// How many received
        /// </summary>
        public ulong BytesReceived {
            get; set;
        }

        /// <summary>
        /// Last error code on the link, e.g. reason link closed.
        /// </summary>
        public int ErrorCode {
            get; set;
        }
    }
}