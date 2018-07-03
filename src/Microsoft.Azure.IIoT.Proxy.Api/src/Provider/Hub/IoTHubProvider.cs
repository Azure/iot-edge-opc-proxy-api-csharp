// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Provider.Legacy {
    using Microsoft.Azure.IIoT.Utils;

    /// <summary>
    /// Default service provider uses IoTHub service.
    /// </summary>
    public class IoTHubProvider : IProvider {

        public virtual IRemotingService ControlChannel  => _iothub;
        public virtual INameService NameService => _iothub;
        public virtual IStreamService StreamService => _iothub;

        /// <summary>
        /// Initialize default provider
        /// </summary>
        /// <param name="iothub"></param>
        public IoTHubProvider(ConnectionString iothub) {
            _iothub = new IoTHubService(iothub);
        }

        /// <summary>
        /// Initialize default provider
        /// </summary>
        /// <param name="iothub"></param>
        public IoTHubProvider(string iothub) {
            _iothub = new IoTHubService(iothub);
        }

        /// <summary>
        /// Default constructor, initializes from environment
        /// </summary>
        public IoTHubProvider() : this((string)null) {}

        /// <summary>
        /// Same as constructor
        /// </summary>
        /// <param name="iothub"></param>
        /// <returns></returns>
        public static IProvider Create(string iothub) => new IoTHubProvider(iothub);

        /// <summary>
        /// Same as constructor
        /// </summary>
        /// <param name="iothub"></param>
        /// <returns></returns>
        public static IProvider Create(ConnectionString iothub) => new IoTHubProvider(iothub);

        private readonly IoTHubService _iothub;
    }
}
