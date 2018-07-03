// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Provider {
    using Microsoft.Azure.IIoT.Proxy.Provider.Legacy;
    using System;

    public class DefaultProvider : IProvider {

        public IRemotingService ControlChannel => throw new NotImplementedException();

        public INameService NameService => throw new NotImplementedException();

        public IStreamService StreamService => throw new NotImplementedException();

        /// <summary>
        /// Get a provider instance
        /// </summary>
        /// <returns></returns>
        public static IProvider Get() {
            if (_provider != null) {
                return _provider;
            }
            lock (_lock) {
                if (_provider == null) {
                    var cs = Environment.GetEnvironmentVariable("_HUB_CS");
                    if (cs != null) {
                        _provider = new IoTHubProvider(cs);
                    }
                    else {
                        _provider = new DefaultProvider();
                    }
                }
                return _provider;
            }
        }

        /// <summary>
        /// Set default instance
        /// </summary>
        /// <param name="provider"></param>
        public static void Set(IProvider provider) {
            lock(_lock) {
                _provider = provider ?? new DefaultProvider();
            }
        }

        /// <summary>
        /// Default constructor, initializes from environment
        /// </summary>
        private DefaultProvider() {}

        private static IProvider _provider;
        private static object _lock = new object();
    }
}
