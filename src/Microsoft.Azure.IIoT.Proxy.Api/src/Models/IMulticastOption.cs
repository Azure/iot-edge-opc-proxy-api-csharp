// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

// Keep in sync with native layer, in particular order of members!

namespace Microsoft.Azure.IIoT.Proxy.Models {
    using System.Net.Proxy;

    /// <summary>
    /// Base interface for multicast options
    /// </summary>
    public interface IMulticastOption {
        AddressFamily Family {
            get;
        }
    }
}