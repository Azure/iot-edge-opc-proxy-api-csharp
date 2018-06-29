// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Proxy {
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides the underlying stream for TCP Client network access
    /// </summary>
    public class NetworkStream : Stream {

        private volatile bool _cleanedUp;
        private readonly bool _ownsSocket;

        /// <summary>
        /// Create stream with socket, and declare socket ownership
        /// </summary>
        public NetworkStream(Socket socket, bool ownsSocket) {
            if (socket == null) {
                throw new ArgumentNullException(nameof(socket));
            }
            if (!socket.Connected) {
                throw new IOException("Socket not connected");
            }
            if (socket.SocketType != SocketType.Stream) {
                throw new IOException("Not a stream socket");
            }

            Socket = socket;
            _ownsSocket = ownsSocket;

            Readable = true;
            Writeable = true;
        }

        /// <summary>
        /// Creates a new NetworkStream class
        /// </summary>
        public NetworkStream(Socket socket) : this(socket, false) {}

        /// <summary>
        /// Returns the underlying socket
        /// </summary>
        public Socket Socket {
            get; private set;
        }

        /// <summary>
        /// Used by the class to indicate that the stream is readable.
        /// </summary>
        protected bool Readable {
            get; set;
        }

        /// <summary>
        /// Used by the class to indicate that the stream is writable.
        /// </summary>
        protected bool Writeable {
            get; set;
        }

        /// <summary>
        /// Indicates that data can be read from the stream.
        /// </summary>
        public override bool CanRead => Readable;

        /// <summary>
        /// Cannot seek
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Indicates that data can be written to the stream.
        /// </summary>
        public override bool CanWrite => Writeable;

        /// <summary>
        /// Can always timeout
        /// </summary>
        public override bool CanTimeout => true;

        /// <summary>
        /// Returns the read timeout
        /// </summary>
        public override int ReadTimeout {
            get => Socket.ReceiveTimeout;
            set => Socket.ReceiveTimeout = value;
        }

        /// <summary>
        /// Returns the write timeout
        /// </summary>
        public override int WriteTimeout {
            get => Socket.SendTimeout;
            set => Socket.SendTimeout = value;
        }

        /// <summary>
        /// Indicates data is available on the stream to be read.
        /// </summary>
        public virtual bool DataAvailable {
            get {
                if (_cleanedUp) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                var chkStreamSocket = Socket;
                if (chkStreamSocket == null) {
                    throw new IOException("Connection closed");
                }
                return chkStreamSocket.Available != 0;
            }
        }

        /// <summary>
        /// Indicates whether the stream is still connected
        /// </summary>
        internal bool Connected {
            get {
                var socket = Socket;
                if (!_cleanedUp && socket != null && socket.Connected) {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Read - provide core Read functionality.
        /// </summary>
        public override int Read([In, Out] byte[] buffer, int offset, int count) {
            var canRead = CanRead;  // Prevent race with Dispose.
            if (_cleanedUp) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!canRead) {
                throw new InvalidOperationException("Cannot read on writeonly stream");
            }
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset >= buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || count > buffer.Length - offset) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var chkStreamSocket = Socket;
            if (chkStreamSocket == null) {
                throw new IOException("connection closed");
            }
            try {
                var bytesTransferred = chkStreamSocket.Receive(buffer, offset, count);
                return bytesTransferred;
            }
            catch (Exception exception) {
                if (exception is OutOfMemoryException) {
                    throw;
                }
                throw new IOException("Failed to receive", exception);
            }
        }

        /// <summary>
        /// Write - provide core Write functionality.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count) {
            var canWrite = CanWrite; // Prevent race with Dispose.
            if (_cleanedUp) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!canWrite) {
                throw new InvalidOperationException("Cannot write on readonly stream");
            }
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset >= buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || count > buffer.Length - offset) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var chkStreamSocket = Socket;
            if (chkStreamSocket == null) {
                throw new IOException("connection closed");
            }
            try {
                chkStreamSocket.Send(buffer, offset, count);
            }
            catch (Exception exception) {
                if (exception is OutOfMemoryException) {
                    throw;
                }
                throw new IOException("Failed to send", exception);
            }
        }

#if NET45 || NET46
        /// <summary>
        /// BeginRead - provide async read functionality.
        /// </summary>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
            var canRead = CanRead; // Prevent race with Dispose.
            if (_cleanedUp) {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (!canRead) {
                throw new InvalidOperationException("Cannot read on writeonly stream");
            }
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset >= buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || count > buffer.Length - offset) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var chkStreamSocket = Socket;
            if (chkStreamSocket == null) {
                throw new IOException("connection closed");
            }
            try {
                var asyncResult =
                    chkStreamSocket.BeginReceive(buffer, offset, count, callback, state);
                return asyncResult;
            }
            catch (Exception exception) {
                if (exception is OutOfMemoryException) {
                    throw;
                }
                throw new IOException("Failed to receive", exception);
            }
        }

        /// <summary>
        /// EndRead - handle the end of an async read.
        /// </summary>
        public override int EndRead(IAsyncResult asyncResult) {
            if (_cleanedUp) {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (asyncResult == null) {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            var chkStreamSocket = Socket;
            if (chkStreamSocket == null) {
                throw new IOException("connection closed");
            }
            try {
                var bytesTransferred = chkStreamSocket.EndReceive(asyncResult);
                return bytesTransferred;
            }
            catch (Exception exception) {
                if (exception is OutOfMemoryException) {
                    throw;
                }
                throw new IOException("Failed to receive", exception);
            }
        }
#endif
        /// <summary>
        /// ReadAsync - provide async read functionality.
        /// </summary>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            var canRead = CanRead;  // Prevent race with Dispose.
            if (_cleanedUp) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!canRead) {
                throw new InvalidOperationException("Cannot read on writeonly stream");
            }
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset >= buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || count > buffer.Length - offset) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var chkStreamSocket = Socket;
            if (chkStreamSocket == null) {
                throw new IOException("connection closed");
            }

            try {
                return await chkStreamSocket.ReceiveAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) {
                if (exception is OutOfMemoryException) {
                    throw;
                }
                throw new IOException("Failed to receive", exception);
            }
        }

        /// <summary>
        /// ReadAsync - provide async read functionality.
        /// </summary>
        public Task<int> ReadAsync(byte[] buffer) =>
            ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

#if NET45 || NET46
        /// <summary>
        /// BeginWrite - provide async write functionality.
        /// </summary>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
            var canWrite = CanWrite; // Prevent race with Dispose.
            if (_cleanedUp) {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (!canWrite) {
                throw new InvalidOperationException("Cannot write on readonly stream");
            }
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset >= buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || count > buffer.Length - offset) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var chkStreamSocket = Socket;
            if (chkStreamSocket == null) {
                throw new IOException("connection closed");
            }
            try {
                /// Call BeginSend on the Socket.
                var asyncResult =
                    chkStreamSocket.BeginSend(buffer, offset, count, callback, state);

                return asyncResult;
            }
            catch (Exception exception) {
                if (exception is OutOfMemoryException) {
                    throw;
                }
                throw new IOException("Failed to send", exception);
            }
        }

        /// <summary>
        /// Complete write
        /// </summary>
        public override void EndWrite(IAsyncResult asyncResult) {
            if (_cleanedUp) {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (asyncResult == null) {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            var chkStreamSocket = Socket;
            if (chkStreamSocket == null) {
                throw new IOException("connection closed");
            }
            try {
                chkStreamSocket.EndSend(asyncResult);
            }
            catch (Exception exception) {
                if (exception is OutOfMemoryException) {
                    throw;
                }
                throw new IOException("Failed to send", exception);
            }
        }
#endif

        /// <summary>
        /// WriteAsync - provide async write functionality.
        /// </summary>
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            var canWrite = CanWrite; // Prevent race with Dispose.
            if (_cleanedUp) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!canWrite) {
                throw new InvalidOperationException("Cannot write on readonly stream");
            }
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset >= buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || count > buffer.Length - offset) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var chkStreamSocket = Socket;
            if (chkStreamSocket == null) {
                throw new IOException("connection closed");
            }

            try {
                await chkStreamSocket.SendAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) {
                if (exception is OutOfMemoryException) {
                    throw;
                }
                throw new IOException("Failed to send", exception);
            }
        }

        /// <summary>
        /// WriteAsync with just buffer
        /// </summary>
        public Task WriteAsync(byte[] buffer) =>
            WriteAsync(buffer, 0, buffer.Length, CancellationToken.None);

        /// <summary>
        /// Flushes data from the stream.
        /// </summary>
        public override void Flush() {}

        /// <summary>
        /// Same no op but async
        /// </summary>
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        /// <summary>
        /// Always throws NotSupportedException.
        /// </summary>
        public override long Length => throw new NotSupportedException("seek not supported on network streams");

        /// <summary>
        /// Always throws NotSupportedException
        /// </summary>
        public override void SetLength(long value) =>
            throw new NotSupportedException("seek not supported on network streams");

        /// <summary>
        /// Always throws NotSupportedException.
        /// </summary>
        public override long Position {
            get => Length;
            set => SetLength(value);
        }

        /// <summary>
        /// Always throws NotSupportedException.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin) => Length;

        #region internal
        /// <summary>
        /// Disposes the Network stream.
        /// </summary>
        protected override void Dispose(bool disposing) {
            // Mark this as disposed before changing anything else.
            var cleanedUp = _cleanedUp;
            _cleanedUp = true;
            if (!cleanedUp && disposing) {
                var chkStreamSocket = Socket;
                Socket = null;
                Readable = false;
                Writeable = false;

                if (chkStreamSocket != null) {
                    if (_ownsSocket) {
                        chkStreamSocket.Dispose();
                        chkStreamSocket = null;
                    }
                }
            }
            base.Dispose(disposing);
        }

        ~NetworkStream() {
            Dispose(false);
        }
        #endregion
    }
}
