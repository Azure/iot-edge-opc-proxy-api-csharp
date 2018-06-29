// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Provider {

    /// <summary>
    /// Operations allowed on the name service.
    /// </summary>
    public enum NameServiceEvent {
        Removed,
        Added,
        Updated,
        Disconnected,
        Connected
    }
}
