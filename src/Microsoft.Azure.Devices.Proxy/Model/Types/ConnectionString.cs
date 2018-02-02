﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

// Keep in sync with native layer, in particular order of members!

namespace Microsoft.Azure.Devices.Proxy {
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text;

    /// <summary>
    /// Connection string
    /// </summary>
    [DataContract]
    public class ConnectionString : Poco<ConnectionString> {

        public enum Id {
            HostName,
            DeviceId,
            SharedAccessKeyName,
            SharedAccessKey,
            Endpoint,
            ConsumerGroup,
            PartitionCount,
            Entity,
            EndpointName,
            SharedAccessToken
        }

        /// <summary>
        /// All items to be serialized
        /// </summary>
        [DataMember (Order = 1)]
        public Dictionary<Id, string> items;

        /// <summary>
        /// Get hub name from connection string
        /// </summary>
        public string HubName {
            get {
                var idx = HostName.IndexOf('.');
                if (idx == -1) {
                    throw new InvalidDataContractException("No hub name");
                }

                return HostName.Substring(idx);
            }
        }

        /// <summary>
        /// Get host name from connection string
        /// </summary>
        public string HostName => this[Id.HostName];

        /// <summary>
        /// Get device id
        /// </summary>
        public string DeviceId => this[Id.DeviceId];

        /// <summary>
        /// Get shared access key name
        /// </summary>
        public string SharedAccessKeyName => this[Id.SharedAccessKeyName];

        /// <summary>
        /// Get shared access key
        /// </summary>
        public string SharedAccessKey => this[Id.SharedAccessKey];

        /// <summary>
        /// Get shared access key
        /// </summary>
        public string SharedAccessToken => this[Id.SharedAccessToken];

        /// <summary>
        /// Get Endpoint address
        /// </summary>
        public Uri Endpoint => new Uri(this[Id.Endpoint]);

        /// <summary>
        /// Get Endpoint address
        /// </summary>
        public string Entity => this[Id.Entity];

        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string this [Id id] {
            get {
                if (!items.TryGetValue(id, out var value)) {
                    return null;
                }

                return value;
            }
            set => items.Add(id, value);
        }

        /// <summary>
        /// Create connection string
        /// </summary>
        public static ConnectionString Create(string host, string endpoint,
            string keyName, string key, bool device = true) {
            var connectionString = Create();
            connectionString.items[Id.HostName] = host;
            if (device) {
                connectionString.items[Id.DeviceId] = endpoint;
                connectionString.items[Id.SharedAccessKey] = key;
            }
            else {
                connectionString.items[Id.EndpointName] = endpoint;
                connectionString.items[Id.SharedAccessToken] = key;
            }
            connectionString.items[Id.SharedAccessKeyName] = keyName;
            return connectionString;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="keyName"></param>
        /// <param name="token"></param>
        public static ConnectionString Create(Uri endpoint, string keyName, string token) {
            var connectionString = Create();
            connectionString.items[Id.Endpoint] = endpoint.ToString();
            connectionString.items[Id.SharedAccessKeyName] = keyName;
            connectionString.items[Id.SharedAccessToken] = token;
            return connectionString;
        }

        /// <summary>
        /// Create connection string
        /// </summary>
        public static ConnectionString Create() => Create(new Dictionary<Id, string>());

        /// <summary>
        /// Create connection string
        /// </summary>
        protected static ConnectionString Create(Dictionary<Id, string> items) {
            var connectionString = Get();
            connectionString.items = items;
            return connectionString;
        }

        public ConnectionString Clone() => Create(items);

        /// <summary>
        /// Parse connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static ConnectionString Parse(string connectionString) {
            if (connectionString == null) {
                throw new ArgumentException("Connection string must be non null");
            }

            var cs = ConnectionString.Create();
            foreach (var elem in connectionString.Split(';')) {
                var i = elem.IndexOf("=", StringComparison.Ordinal);
                if (i < 0) {
                    throw new InvalidDataContractException("Bad key value pair.");
                }
                // Throws argument if already exists or parse fails...
                cs.items.Add((Id)Enum.Parse(typeof(Id), elem.Substring(0, i), true), elem.Substring(i + 1));
            }
            return cs;
        }

        /// <summary>
        /// Converts to string
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var b = new StringBuilder();
            foreach (var kv in items) {
                b.Append(kv.Key.ToString());
                b.Append("=");
                b.Append(kv.Value.ToString());
                b.Append(";");
            }
            return b.ToString().TrimEnd(';');
        }

        /// <summary>
        /// Type safe compare
        /// </summary>
        /// <param name="that"></param>
        /// <returns></returns>
        public override bool IsEqual(ConnectionString that) =>
            IsEqual(items, that.items);

        protected override void SetHashCode() =>
            MixToHash(items);
    }
}
