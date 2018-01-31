﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Devices.Proxy {
    /// <summary>
    /// Concrete implementation for Lookup and Sending
    /// </summary>
    public interface IProvider {

        /// <summary>
        /// Name service
        /// </summary>
        /// <returns></returns>
        INameService NameService { get; }

        /// <summary>
        /// Control channel
        /// </summary>
        /// <returns></returns>
        IRemotingService ControlChannel { get; }

        /// <summary>
        /// Stream service to create streams
        /// </summary>
        /// <returns></returns>
        IStreamService StreamService { get; }
    }
}
