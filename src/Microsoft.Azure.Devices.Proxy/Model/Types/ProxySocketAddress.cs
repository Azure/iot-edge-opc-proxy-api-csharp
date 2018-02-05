// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

// Keep in sync with native layer, in particular order of members!

namespace Microsoft.Azure.Devices.Proxy {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Proxy socket address (prx_address_family_proxy)
    /// </summary>
    [DataContract]
    public class ProxySocketAddress : InetSocketAddress, IEquatable<ProxySocketAddress> {

        [DataMember(Name = "family", Order = 1)]
        public override AddressFamily Family => AddressFamily.Proxy;

        /// <summary>
        /// Interface Index field
        /// </summary>
        [DataMember(Name = "itf_index", Order = 3)]
        public int InterfaceIndex {
            get; set;
        } = -1;

        /// <summary>
        /// Interface Index field
        /// </summary>
        [DataMember(Name = "flags", Order = 4)]
        public ushort Flags {
            get; set;
        }

        /// <summary>
        /// Host name to use
        /// </summary>
        [DataMember(Name = "host", Order = 5)]
        public string Host {
            get; set;
        } = "";

        /// <summary>
        /// Domain name to use
        /// </summary>
        [DataMember(Name = "domain", Order = 5)]
        public string Domain {
            get; set;
        } = "";

        /// <summary>
        /// Default constructor
        /// </summary>
        public ProxySocketAddress() {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="flags"></param>
        /// <param name="interfaceIndex"></param>
        public ProxySocketAddress(string host, ushort port = 0, 
            ushort flags = 0, int interfaceIndex = -1) : this() {
            if (host == null) {
                throw new ArgumentNullException(nameof(host));
            }
            // Parse a domain from host if any provided.
            Host = ParseDomain(host, out var domain);
            Domain = domain;
            Port = port;
            Flags = flags;
            InterfaceIndex = interfaceIndex;
        }

        /// <summary>
        /// Constructor that checks port is in valid ushort range and throws if not.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public ProxySocketAddress(string host, int port) : this(host, (ushort)port) {
            if (port < 0 || port > ushort.MaxValue) {
                throw new ArgumentNullException(nameof(port));
            }
        }

        /// <summary>
        /// Creates an address from a proxy socket address string representation
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static ProxySocketAddress Parse(string address) {
            string host;
            ushort port;
            var index = address.IndexOf(':');
            if (index <= 0) {
                host = address;
                port = 0;
            }
            else {
                host = address.Substring(0, index);
                port = ushort.Parse(address.Substring(index + 1));
            }
            return new ProxySocketAddress(host, port);
        }

        /// <summary>
        /// Parse a string into a proxy address - does not throw.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="parsed"></param>
        /// <returns></returns>
        public static bool TryParse(string address, out ProxySocketAddress parsed) {
            try {
                parsed = Parse(address);
            }
            catch {
                parsed = null;
            }
            return parsed != null;
        }

        /// <summary>
        /// Comparison - equality does not include flags and interface index
        /// </summary>
        /// <param name="that"></param>
        /// <returns></returns>
        public bool Equals(ProxySocketAddress that) {
            if (that == null) {
                return false;
            }
            return
                IsEqual(that as InetSocketAddress) &&
                IsEqual(Host, that.Host) &&
                IsEqual(Domain, that.Domain);
        }

        public override bool IsEqual(object that) => Equals(that as ProxySocketAddress);

        protected override void SetHashCode() {
            base.SetHashCode();
            MixToHash(Host);
            MixToHash(Domain);
        }

        public override ProxySocketAddress AsProxySocketAddress() => this;

        /// <summary>
        /// Stringify address
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return $"{Host}{(!string.IsNullOrEmpty(Domain) ? (kDomainDelimiter + Domain) : "")}:{Port}";
        }

        /// <summary>
        /// Parse domain from host name
        /// </summary>
        /// <param name="host"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        private string ParseDomain(string host, out string domain) {
            host = host.Trim().ToLowerInvariant();
            if (host.Contains(kDomainDelimiter)) {
                var components = host.Split(new string[] { kDomainDelimiter },
                    StringSplitOptions.RemoveEmptyEntries);
                if (components.Length == 2) {
                    domain = components[1].Trim();
                    return components[0].Trim();
                }
            }
            domain = null;
            return host;
        }

        const string kDomainDelimiter = ".proxy.";
    }
}