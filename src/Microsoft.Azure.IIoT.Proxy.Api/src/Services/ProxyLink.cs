﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Services {
    using Microsoft.Azure.IIoT.Proxy.Provider;
    using Microsoft.Azure.IIoT.Proxy.Exceptions;
    using Microsoft.Azure.IIoT.Proxy.Serializer;
    using Microsoft.Azure.IIoT.Proxy.Models;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using System.Net.Proxy;

    /// <summary>
    /// Proxy link represents a 1:1 link with a remote socket. Proxy
    /// Link is created via LinkRequest, and OpenRequest Handshake.
    /// </summary>
    internal class ProxyLink : IProxyLink {

        /// <summary>
        /// Remote link id
        /// </summary>
        public Reference RemoteId {
            get; private set;
        }

        /// <summary>
        /// Local address on remote side
        /// </summary>
        public SocketAddress LocalAddress {
            get; private set;
        }

        /// <summary>
        /// Peer address on remote side
        /// </summary>
        public SocketAddress PeerAddress {
            get; private set;
        }

        /// <summary>
        /// Bound proxy for this stream
        /// </summary>
        public INameRecord Proxy {
            get; private set;
        }

        /// <summary>
        /// Proxy the socket is bound on.
        /// </summary>
        public SocketAddress ProxyAddress => Proxy.Address.ToSocketAddress();

        /// <summary>
        /// Target buffer block
        /// </summary>
        public ITargetBlock<Message> SendBlock => _send;

        /// <summary>
        /// Source buffer block
        /// </summary>
        public ISourceBlock<Message> ReceiveBlock => _receive;

        /// <summary>
        /// Constructor for proxy link object
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="proxy"></param>
        /// <param name="remoteId"></param>
        /// <param name="localAddress"></param>
        /// <param name="peerAddress"></param>
        internal ProxyLink(ProxySocket socket, INameRecord proxy, Reference remoteId,
            SocketAddress localAddress, SocketAddress peerAddress) {

            _socket = socket ?? throw new ArgumentNullException(nameof(socket));

            Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
            RemoteId = remoteId ?? throw new ArgumentNullException(nameof(remoteId));
            LocalAddress = localAddress ?? throw new ArgumentNullException(nameof(localAddress));
            PeerAddress = peerAddress ?? throw new ArgumentNullException(nameof(peerAddress));
        }


        /// <summary>
        /// Begin open of stream, this provides the connection string for the
        /// remote side, that is passed as part of the open request.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<OpenRequest> BeginOpenAsync(CancellationToken ct) {
            try {
#if DEBUG_MESSAGE_CONTENT
                var encoding = CodecId.Json;
#else
                var encoding = CodecId.Mpack;
#endif
                _connection = await _socket.Provider.StreamService.CreateConnectionAsync(
                    _streamId, RemoteId, Proxy, encoding).ConfigureAwait(false);

                var maxSize = (int)_connection.MaxBufferSize;

                CreateSendBlock(maxSize);
                CreateReceiveBlock();

                return OpenRequest.Create(_streamId, (int)encoding,
                    _connection.ConnectionString != null ?
                        _connection.ConnectionString.ToString() : "", 0, _connection.IsPolled,
                    _connection.MaxBufferSize);
            }
            catch (OperationCanceledException) {
                return null;
            }
        }

        /// <summary>
        /// Create send block
        /// </summary>
        /// <param name="maxSize">max buffer size for sending</param>
        protected virtual void CreateSendBlock(int maxSize) {
            var options = new ExecutionDataflowBlockOptions {
                NameFormat = "Send (in Link) Id={1}",
                EnsureOrdered = true,
                MaxMessagesPerTask = DataflowBlockOptions.Unbounded,
                SingleProducerConstrained = false,
                BoundedCapacity = 3
            };
            if (maxSize == 0) {
                _send = new BufferBlock<Message>(options);
                return;
            }

            _send = new TransformManyBlock<Message, Message>((message) => {
                if (message.TypeId != MessageContent.Data ||
                    ((DataMessage)message.Content).Payload.Length <= maxSize) {
                    return message.AsEnumerable();
                }
                // Split messages if needed using max size
                var data = message.Content as DataMessage;
                var segmentCount = data.Payload.Length / maxSize;
                var segmentSize = maxSize;

                if (data.Payload.Length % maxSize != 0) {
                    // Distribute payload equally across all messages
                    segmentCount++;
                    segmentSize = (data.Payload.Length + segmentCount) / segmentCount;
                }

                // Create segment messages
                var segmentMessages = new Message[segmentCount];
                for (int i = 0, offset = 0; i < segmentMessages.Length; i++, offset += segmentSize) {
                    var segmentLength = Math.Min(data.Payload.Length - offset, segmentSize);
                    var segment = new ArraySegment<byte>(data.Payload, offset, segmentLength);
                    segmentMessages[i] = Message.Create(message.Source, message.Target, message.Proxy,
                        DataMessage.Create(segment, null));
                }
                return segmentMessages;
            },
            options);
        }

        /// <summary>
        /// Create receive block
        /// </summary>
        protected virtual void CreateReceiveBlock() {
            _receive = new TransformManyBlock<Message, Message>((message) => {
                if (message.Error == (int)SocketError.Closed ||
                    message.TypeId == MessageContent.Close) {
                    // Remote side closed
                    throw new SocketException("Remote side closed", null, SocketError.Closed);
                }
                if (message.Error == (int)SocketError.Duplicate) {
                    return Enumerable.Empty<Message>();
                }
                if (message.Error != (int)SocketError.Success) {
                    ProxySocket.ThrowIfFailed(message);
                }
                else if (message.TypeId == MessageContent.Data) {
                    return message.AsEnumerable();
                }
                // Todo: log error?
                return Enumerable.Empty<Message>();
            },
            new ExecutionDataflowBlockOptions {
                NameFormat = "Receive (in Link) Id={1}",
                EnsureOrdered = true,
                MaxMessagesPerTask = DataflowBlockOptions.Unbounded,
                SingleProducerConstrained = true,
                BoundedCapacity = 3
            });
        }

        /// <summary>
        /// Complete connection by waiting for remote side to connect.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<bool> TryCompleteOpenAsync(CancellationToken ct) {
            if (_connection == null) {
                return false;
            }

            try {
                var stream = await _connection.OpenAsync(ct).ConfigureAwait(false);

                _streamReceive = stream.ReceiveBlock.ConnectTo(_receive);
                _streamSend = _send.ConnectTo(stream.SendBlock);

                return true;
            }
            catch (OperationCanceledException) {
                return false;
            }
        }

        /// <summary>
        /// Send socket option message
        /// </summary>
        /// <param name="option"></param>
        /// <param name="value"></param>
        /// <param name="ct"></param>
        public async Task SetSocketOptionAsync(
            SocketOption option, ulong value, CancellationToken ct) {
            using (var request = Message.Create(_socket.Id, RemoteId, SetOptRequest.Create(
                Property<ulong>.Create((uint)option, value)))) {

                var response = await _socket.Provider.ControlChannel.CallAsync(
                    Proxy, request, TimeSpan.MaxValue, ct).ConfigureAwait(false);
                ProxySocket.ThrowIfFailed(response);
            }
        }

        /// <summary>
        /// Get socket option
        /// </summary>
        /// <param name="option"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<ulong> GetSocketOptionAsync(
            SocketOption option, CancellationToken ct) {
            using (var request = Message.Create(_socket.Id, RemoteId, GetOptRequest.Create(
                option))) {

                var response = await _socket.Provider.ControlChannel.CallAsync(
                    Proxy, request, TimeSpan.MaxValue, ct).ConfigureAwait(false);
                ProxySocket.ThrowIfFailed(response);

                var optionValue = ((GetOptResponse)response.Content).OptionValue as Property<ulong>;
                if (optionValue == null) {
                    throw new ProxyException("Bad option value returned");
                }
                return optionValue.Value;
            }
        }

        /// <summary>
        /// Close link
        /// </summary>
        /// <param name="ct"></param>
        public async Task CloseAsync(CancellationToken ct) {
            var tasks = new Task[] { UnlinkAsync(ct), TerminateConnectionAsync(ct) };
            try {
                // Close both ends
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (AggregateException ae) {
                if (ae.InnerExceptions.Count == tasks.Length) {
                    // Only throw if all tasks failed.
                    throw SocketException.Create("Exception during close", ae);
                }
                ProxyEventSource.Log.HandledExceptionAsInformation(this, ae.Flatten());
            }
            catch (Exception e) {
                ProxyEventSource.Log.HandledExceptionAsInformation(this, e);
            }
        }

        /// <summary>
        /// Close the stream part
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task TerminateConnectionAsync(CancellationToken ct) {
            var connection = _connection;
            _connection = null;

            ProxyEventSource.Log.StreamClosing(this, null);
            try {
                try {
                    await SendBlock.SendAsync(Message.Create(_socket.Id, RemoteId,
                    CloseRequest.Create()), ct).ConfigureAwait(false);
                }
                catch { }
                try {
                    await connection.CloseAsync().ConfigureAwait(false);
                }
                catch { }

               // try {
               //     SendBlock.Complete();
               //     await SendBlock.Completion.ConfigureAwait(false);
               // }
               // catch { }
               // try {
               //     ReceiveBlock.Complete();
               //     await ReceiveBlock.Completion.ConfigureAwait(false);
               // }
               // catch { }
            }
            finally {
                ProxyEventSource.Log.StreamClosed(this, null);
            }
        }

        /// <summary>
        /// Remove link on remote proxy through rpc
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task UnlinkAsync(CancellationToken ct) {
            var request = Message.Create(_socket.Id, RemoteId, CloseRequest.Create());
            try {
                var response = await _socket.Provider.ControlChannel.CallAsync(Proxy,
                    request, TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
                ProxyEventSource.Log.LinkRemoved(this);
                if (response != null) {
                    var errorCode = (SocketError)response.Error;
                    if (errorCode != SocketError.Success &&
                        errorCode != SocketError.Timeout &&
                        errorCode != SocketError.Closed) {
                        throw new SocketException(errorCode);
                    }
                }
            }
            catch (Exception e) when (!(e is SocketException)) {
                throw SocketException.Create("Failed to close", e);
            }
            finally {
                request.Dispose();
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() {
            return
                $"Link {PeerAddress} through {LocalAddress} on {Proxy} "
              + $"with stream {_streamId} (Socket {_socket})";
        }

        protected IPropagatorBlock<Message, Message> _send;
        protected IPropagatorBlock<Message, Message> _receive;
        protected readonly ProxySocket _socket;
        private IConnection _connection;
        private IDisposable _streamSend;
        private IDisposable _streamReceive;
        private readonly Reference _streamId = new Reference();
    }
}