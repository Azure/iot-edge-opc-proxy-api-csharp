// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Server {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Net.Scanner;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Proxy;
    using System.Threading.Tasks;

    public class ResolverService : IResolverService {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="logger"></param>
        public ResolverService(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pinger = new ConnectProbe(logger, 5000);
        }

        /// <summary>
        /// Todo move to service
        /// </summary>
        /// <param name="socketAddress"></param>
        /// <returns></returns>
        public async Task<SocketAddress> ResolveAsync(SocketAddress socketAddress) {
            if (socketAddress == null) {
                throw new ArgumentNullException(nameof(socketAddress));
            }
            IEnumerable<System.Net.IPAddress> addresses;
            var address = socketAddress.AsProxySocketAddress();
            if (address != null) {
                addresses = await System.Net.Dns.GetHostAddressesAsync(address.Host);
            }
            else if (socketAddress is Inet4SocketAddress i4) {
                addresses = new System.Net.IPAddress(i4.Address).YieldReturn();
            }
            else if (socketAddress is Inet6SocketAddress i6) {
                addresses = new System.Net.IPAddress(i6.Address).YieldReturn();
            }
            else {
                throw new ArgumentException("Address is not valid address");
            }

            if (address.Port != 0) {
                var connects = addresses
                    .Select(a => new System.Net.IPEndPoint(a, address.Port))
                    .Select(_pinger.ExistsAsync);
                var results = await Task.WhenAll(connects);
                return results.FirstOrDefault(ep => ep != null)?.ToSocketAddress();
            }

            // Ping addresses
            //  new System.Net.Ping.().
            //  var results = await Task.WhenAll(endpoints.Select(pint.ExistsAsync));

            return null;
        }

        private readonly ILogger _logger;
        private readonly ConnectProbe _pinger;
    }
}
