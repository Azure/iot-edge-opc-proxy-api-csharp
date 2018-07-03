// Copyright (c) 1996-2016, OPC Foundation. All rights reserved.
// The source code in this file is covered under a dual-license scenario:
//   - RCL: for OPC Foundation members in good-standing
//   - GPL V2: everybody else
// RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
// GNU General Public License as published by the Free Software Foundation;
// version 2 of the License are accompanied with this source code. See http://opcfoundation.org/License/GPLv2
// This source code is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//

using System;
using System.Threading.Tasks;
using System.Net.Proxy;

namespace Opc.Ua.Bindings.Proxy {

    /// <summary>
    /// Creates a new ProxyTransportChannel with ITransportChannel interface.
    /// </summary>
    public class ProxyTransportChannelFactory : ITransportChannelFactory {
        /// <summary>
        /// The method creates a new instance of a Proxy transport channel
        /// </summary>
        /// <returns> the transport channel</returns>
        public ITransportChannel Create() => new ProxyTransportChannel();
    }

    /// <summary>
    /// Creates a transport channel with proxy transport, UA-SC security and UA Binary encoding
    /// </summary>
    public class ProxyTransportChannel : UaSCUaBinaryTransportChannel {
        public ProxyTransportChannel() : base(new ProxyMessageSocketFactory()) {}
    }

    /// <summary>
    /// Handles async event callbacks from a socket
    /// </summary>
    public class ProxyMessageSocketAsyncEventArgs : IMessageSocketAsyncEventArgs {
        public ProxyMessageSocketAsyncEventArgs() {
            _args = new SocketAsyncEventArgs {
                UserToken = this
            };
        }

        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose() => _args.Dispose();

        public object UserToken { get; set; }

        public void SetBuffer(byte[] buffer, int offset, int count) {
            _args.SetBuffer(buffer, offset, count);
        }

        public bool IsSocketError => _args.SocketError != SocketError.Success;

        public string SocketErrorString => _args.SocketError.ToString();

        public event EventHandler<IMessageSocketAsyncEventArgs> Completed {
            add {
                _internalComplete += value;
                _args.Completed += OnComplete;
            }
            remove {
                _internalComplete -= value;
                _args.Completed -= OnComplete;
            }
        }

        protected void OnComplete(object sender, SocketAsyncEventArgs e) {
            if (e.UserToken == null) {
                return;
            }

            _internalComplete(this, e.UserToken as IMessageSocketAsyncEventArgs);
        }

        public int BytesTransferred => _args.BytesTransferred;

        public byte[] Buffer => _args.Buffer;

        public BufferCollection BufferList {
            get => _args.BufferList as BufferCollection;
            set => _args.BufferList = value;
        }

        public SocketAsyncEventArgs _args;

        private event EventHandler<IMessageSocketAsyncEventArgs> _internalComplete;
    }

    /// <summary>
    /// Creates a new ProxyMessageSocket with IMessageSocket interface.
    /// </summary>
    public class ProxyMessageSocketFactory : IMessageSocketFactory {
        /// <summary>
        /// The method creates a new instance of a proxy message socket
        /// </summary>
        /// <returns> the message socket</returns>
        public IMessageSocket Create(IMessageSink sink, BufferManager bufferManager, 
            int receiveBufferSize) =>
            new ProxyMessageSocket(sink, bufferManager, receiveBufferSize);

        /// <summary>
        /// Gets the implementation description.
        /// </summary>
        /// <value>The implementation string.</value>
        public string Implementation => "UA-Proxy";
    }

    /// <summary>
    /// Handles reading and writing of message chunks over a socket.
    /// </summary>
    public class ProxyMessageSocket : IMessageSocket {
        /// <summary>
        /// The proxy socket
        /// </summary>
        public Socket ProxySocket { get; set; }

        /// <summary>
        /// Creates an unconnected socket.
        /// </summary>
        public ProxyMessageSocket(
            IMessageSink sink,
            BufferManager bufferManager,
            int receiveBufferSize) {
            _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
            ProxySocket = null;
            _sink = sink;
            _receiveBufferSize = receiveBufferSize;
            _incomingMessageSize = -1;
            _readComplete = new EventHandler<SocketAsyncEventArgs>(OnReadComplete);
        }

        /// <summary>
        /// Attaches the object to an existing socket.
        /// </summary>
        public ProxyMessageSocket(
            IMessageSink sink,
            Socket socket,
            BufferManager bufferManager,
            int receiveBufferSize) {
            _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
            ProxySocket = socket ?? throw new ArgumentNullException(nameof(socket));
            _sink = sink;
            _receiveBufferSize = receiveBufferSize;
            _incomingMessageSize = -1;
            _readComplete = new EventHandler<SocketAsyncEventArgs>(OnReadComplete);
        }

        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                // get the socket.
                Socket socket = null;

                lock (_socketLock) {
                    socket = ProxySocket;
                    ProxySocket = null;
                }

                // shutdown the socket.
                if (socket != null) {
                    try {
                        socket.Dispose();
                    }
                    catch (Exception e) {
                        Utils.Trace(e, "Unexpected error closing socket.");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the socket handle.
        /// </summary>
        /// <value>The socket handle.</value>
        public int Handle {
            get {
                if (ProxySocket != null) {
                    return ProxySocket.GetHashCode();
                }
                return -1;
            }
        }

        /// <summary>
        /// Connects to an endpoint.
        /// </summary>
        public async Task<bool> BeginConnect(Uri endpointUrl, EventHandler<IMessageSocketAsyncEventArgs> callback, object state) {
            var result = false;

            if (endpointUrl == null) {
                throw new ArgumentNullException(nameof(endpointUrl));
            }

            if (ProxySocket != null) {
                throw new InvalidOperationException("The socket is already connected.");
            }

            var args = new ProxyMessageSocketAsyncEventArgs {
                UserToken = state
            };
            args._args.SocketError = SocketError.HostUnknown;

            ProxySocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            try {
                await ProxySocket.ConnectAsync(endpointUrl.DnsSafeHost, endpointUrl.Port);
                args._args.SocketError = SocketError.Success;
                result = true;
            }
            catch (Exception e) {
                args._args.SocketError = e.GetProxySocketError();
            }
            finally {
                var t = Task.Run(() => callback(this, args));
            }
            return result;
        }

        /// <summary>
        /// Forcefully closes the socket.
        /// </summary>
        public void Close() {
            // get the socket.
            if (ProxySocket != null) {
                ProxySocket.Close();

                Dispose(true);
            }
        }

        /// <summary>
        /// Starts reading messages from the socket.
        /// </summary>
        public void ReadNextMessage() {
            lock (_readLock) {
                // allocate a buffer large enough to a message chunk.
                if (_receiveBuffer == null) {
                    _receiveBuffer = _bufferManager.TakeBuffer(_receiveBufferSize, "ReadNextMessage");
                }

                // read the first 8 bytes of the message which contains the message size.
                _bytesReceived = 0;
                _bytesToReceive = TcpMessageLimits.MessageTypeAndSize;
                _incomingMessageSize = -1;

                ReadNextBlock();
            }
        }

        /// <summary>
        /// Changes the sink used to report reads.
        /// </summary>
        public void ChangeSink(IMessageSink sink) {
            lock (_readLock) {
                _sink = sink;
            }
        }

        /// <summary>
        /// Handles a read complete event.
        /// </summary>
        private void OnReadComplete(object sender, SocketAsyncEventArgs e) {
            lock (_readLock) {
                ServiceResult error = null;

                try {
                    error = DoReadComplete(e);
                }
                catch (Exception ex) {
                    Utils.Trace(ex, "Unexpected error during OnReadComplete,");
                    error = ServiceResult.Create(ex, StatusCodes.BadTcpInternalError, ex.Message);
                }
                finally {
                    e.Dispose();
                }

                if (ServiceResult.IsBad(error)) {
                    if (_receiveBuffer != null) {
                        _bufferManager.ReturnBuffer(_receiveBuffer, "OnReadComplete");
                        _receiveBuffer = null;
                    }

                    if (_sink != null) {
                        _sink.OnReceiveError(this, error);
                    }
                }
            }
        }

        /// <summary>
        /// Handles a read complete event.
        /// </summary>
        private ServiceResult DoReadComplete(SocketAsyncEventArgs e) {
            // complete operation.
            var bytesRead = e.BytesTransferred;

            lock (_socketLock) {
                BufferManager.UnlockBuffer(_receiveBuffer);
            }

            Utils.Trace("Bytes read: {0}", bytesRead);

            if (bytesRead == 0 || e.SocketError != (int)SocketError.Success) {
                // free the empty receive buffer.
                if (_receiveBuffer != null) {
                    _bufferManager.ReturnBuffer(_receiveBuffer, "DoReadComplete");
                    _receiveBuffer = null;
                }

                if (bytesRead == 0) {
                    return ServiceResult.Create(StatusCodes.BadConnectionClosed, "Remote side closed connection");
                }

                return ServiceResult.Create(StatusCodes.BadTcpInternalError, "Error {0} on connection during receive", e.SocketError.ToString());
            }

            _bytesReceived += bytesRead;

            // check if more data left to read.
            if (_bytesReceived < _bytesToReceive) {
                ReadNextBlock();

                return ServiceResult.Good;
            }

            // start reading the message body.
            if (_incomingMessageSize < 0) {
                _incomingMessageSize = BitConverter.ToInt32(_receiveBuffer, 4);

                if (_incomingMessageSize <= 0 || _incomingMessageSize > _receiveBufferSize) {
                    Utils.Trace(
                        "BadTcpMessageTooLarge: BufferSize={0}; MessageSize={1}",
                        _receiveBufferSize,
                        _incomingMessageSize);

                    return ServiceResult.Create(
                        StatusCodes.BadTcpMessageTooLarge,
                        "Messages size {1} bytes is too large for buffer of size {0}.",
                        _receiveBufferSize,
                        _incomingMessageSize);
                }

                // set up buffer for reading the message body.
                _bytesToReceive = _incomingMessageSize;

                ReadNextBlock();

                return ServiceResult.Good;
            }

            // notify the sink.
            if (_sink != null) {
                try {
                    // send notification (implementor responsible for freeing buffer) on success.
                    var messageChunk = new ArraySegment<byte>(_receiveBuffer, 0, _incomingMessageSize);

                    // must allocate a new buffer for the next message.
                    _receiveBuffer = null;

                    _sink.OnMessageReceived(this, messageChunk);
                }
                catch (Exception ex) {
                    Utils.Trace(ex, "Unexpected error invoking OnMessageReceived callback.");
                }
            }

            // free the receive buffer.
            if (_receiveBuffer != null) {
                _bufferManager.ReturnBuffer(_receiveBuffer, "DoReadComplete");
                _receiveBuffer = null;
            }

            // start receiving next message.
            ReadNextMessage();

            return ServiceResult.Good;
        }

        /// <summary>
        /// Reads the next block of data from the socket.
        /// </summary>
        private void ReadNextBlock() {
            Socket socket = null;

            // check if already closed.
            lock (_socketLock) {
                if (ProxySocket == null) {
                    if (_receiveBuffer != null) {
                        _bufferManager.ReturnBuffer(_receiveBuffer, "ReadNextBlock");
                        _receiveBuffer = null;
                    }

                    return;
                }

                socket = ProxySocket;

                // avoid stale ServiceException when socket is disconnected
                if (!socket.Connected) {
                    return;
                }

            }

            BufferManager.LockBuffer(_receiveBuffer);

            var error = ServiceResult.Good;
            var args = new SocketAsyncEventArgs();
            try {
                args.SetBuffer(_receiveBuffer, _bytesReceived, _bytesToReceive - _bytesReceived);
                args.Completed += _readComplete;
                if (!socket.ReceiveAsync(args)) {
                    // I/O completed synchronously
                    if ((args.SocketError != SocketError.Success) || (args.BytesTransferred < (_bytesToReceive - _bytesReceived))) {
                        throw ServiceResultException.Create(StatusCodes.BadTcpInternalError, args.SocketError.ToString());
                    }

                    args.Dispose();
                }
            }
            catch (ServiceResultException sre) {
                args.Dispose();
                BufferManager.UnlockBuffer(_receiveBuffer);
                throw sre;
            }
            catch (Exception ex) {
                args.Dispose();
                BufferManager.UnlockBuffer(_receiveBuffer);
                throw ServiceResultException.Create(StatusCodes.BadTcpInternalError, ex, "BeginReceive failed.");
            }
        }

        /// <summary>
        /// Sends a buffer.
        /// </summary>
        public bool SendAsync(IMessageSocketAsyncEventArgs args) {
            if (!(args is ProxyMessageSocketAsyncEventArgs eventArgs)) {
                throw new ArgumentNullException(nameof(args));
            }
            if (ProxySocket == null) {
                throw new InvalidOperationException("The socket is not connected.");
            }
            eventArgs._args.SocketError = SocketError.Unknown;
            return ProxySocket.SendAsync(eventArgs._args);
        }

        public IMessageSocketAsyncEventArgs MessageSocketEventArgs() {
            return new ProxyMessageSocketAsyncEventArgs();
        }

        private IMessageSink _sink;
        private BufferManager _bufferManager;
        private readonly int _receiveBufferSize;
        private readonly EventHandler<SocketAsyncEventArgs> _readComplete;
        private readonly object _socketLock = new object();
        private readonly object _readLock = new object();
        private byte[] _receiveBuffer;
        private int _bytesReceived;
        private int _bytesToReceive;
        private int _incomingMessageSize;
    }
}