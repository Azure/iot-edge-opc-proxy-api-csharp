// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Client {
    using Microsoft.Azure.IIoT.Proxy.Provider;
    using Microsoft.Azure.IIoT.Proxy.Exceptions;
    using Microsoft.Azure.IIoT.Proxy.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using System.Net.Proxy;

    internal class UDPSocket : BroadcastSocket {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="provider"></param>
        internal UDPSocket(SocketInfo info, IProvider provider) :
            base(info, provider) {

            if (Info.Type != SocketType.Dgram) {
                throw new ArgumentException("Udp only supports datagrams");
            }

            if (Info.Address == null) {
                Info.Address = new AnySocketAddress();
            }
        }

        /// <summary>
        /// Select the proxy to bind to
        /// </summary>
        /// <param name="address"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public override Task BindAsync(SocketAddress address, CancellationToken ct) =>
            LinkAsync(address, ct);

        /// <summary>
        /// Send buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="endpoint"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async override Task<int> SendAsync(ArraySegment<byte> buffer,
            SocketAddress endpoint, CancellationToken ct) {
            await SendBlock.SendAsync(Message.Create(null, null, null,
                DataMessage.Create(buffer, endpoint)), ct).ConfigureAwait(false);
            return buffer.Count;
        }

        /// <summary>
        /// Receive a data packet
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async override Task<ProxyAsyncResult> ReceiveAsync(
            ArraySegment<byte> buffer, CancellationToken ct) {
            Message message = null;
            try {
                message = await ReceiveBlock.ReceiveAsync(ct).ConfigureAwait(false);
                var data = message.Content as DataMessage;
                var copy = Math.Min(data.Payload.Length, buffer.Count);
                Buffer.BlockCopy(data.Payload, 0, buffer.Array, buffer.Offset, copy);
                return new ProxyAsyncResult {
                    Address = data.Source,
                    Count = copy
                };
            }
            catch (Exception e) {
                if (ReceiveBlock.Completion.IsFaulted) {
                    e = ReceiveBlock.Completion.Exception;
                }
                throw SocketException.Create("Failed to receive", e);
            }
            finally {
                message?.Dispose();
            }
        }

        public override Task ConnectAsync(SocketAddress address, CancellationToken ct) {
            throw new NotSupportedException("Cannot call connect on this socket");
        }

        public override Task ListenAsync(int backlog, CancellationToken ct) {
            throw new NotSupportedException("Cannot call listen on this socket");
        }
    }
}
