﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Provider {
    using System;
    using System.Net.WebSockets;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Utils;

    /// <summary>
    /// Buffered Websocket stream. Wraps a Websocket and provides read and write
    /// buffering through fragments and end messages.  Only Binary format is
    /// used.
    /// </summary>
    public class WebSocketStream : Stream {

        public static int DefaultBufferSize { get; } = 0x1000;

        /// <summary>
        /// Returns whether the stream is closed
        /// </summary>
        public bool IsClosed => _websocket == null || _websocket.CloseStatus != null;

        public override bool CanRead => !IsClosed;

        public override bool CanWrite => !IsClosed;

        /// <summary>
        /// Reading length is not supported
        /// </summary>
        public override long Length =>
            throw new NotSupportedException();

        /// <summary>
        /// And setting is also not supported
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value) =>
            throw new NotSupportedException();

        /// <summary>
        /// Setting position is not supported
        /// </summary>
        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Cannot seak
        /// </summary>
        public override bool CanSeek { get; } = false;

        /// <summary>
        /// Thus throw...
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="websocket"></param>
        public WebSocketStream(WebSocket websocket) :
            this(websocket, DefaultBufferSize) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="websocket"></param>
        public WebSocketStream(WebSocket websocket, int bufferSize) {
            if (bufferSize <= 0) {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            _websocket = websocket ?? throw new ArgumentNullException(nameof(websocket));
            _bufferSize = bufferSize;

            _readbuffer = new byte[_bufferSize];
            _writebuffer = new byte[_bufferSize];
        }

        /// <summary>
        /// Close sync
        /// </summary>
#if !NETSTANDARD1_3
        public override void Close() {
#else
        public void Close() {
#endif
            Try.Op(Flush);
            _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed",
                CancellationToken.None).Wait();
            _websocket = null;
        }

        /// <summary>
        /// Close async
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task CloseAsync(CancellationToken cancellationToken) {
            await Try.Async(FlushAsync, cancellationToken).ConfigureAwait(false);
            await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed",
                cancellationToken).ConfigureAwait(false);
            _websocket = null;
        }

        /// <summary>
        /// flush final message and reposition read pointer to start of message
        /// </summary>
        public override void Flush() =>
            FlushAsync(CancellationToken.None).Wait();

        /// <summary>
        /// Send final message and reposition read pointer to start of message
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task FlushAsync(CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            if (IsClosed) {
                throw new IOException("Stream closed");
            }

            if (_needsFlush) {
                await _asyncWrite.WaitAsync().ConfigureAwait(false);
                try {
                    if (_needsFlush) {
                        await _websocket.SendAsync(new ArraySegment<byte>(_writebuffer, 0, _writePos),
                            WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                        _writePos = 0;
                        _needsFlush = false;
                    }
                }
                finally {
                    _asyncWrite.Release();
                }
            }
        }

        /// <summary>
        /// Reposition read pointer to start of message
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SkipAsync(CancellationToken cancellationToken) {
            await _asyncRead.WaitAsync().ConfigureAwait(false);
            try {
                while (!_readEnd) {
                    // Read until we hit the end
                    var result = await _websocket.ReceiveAsync(
                        new ArraySegment<byte>(_readbuffer, 0, _bufferSize),
                            cancellationToken).ConfigureAwait(false);
                    _readEnd = result.EndOfMessage;
                    _readPos = _readLen = 0;
                }
            }
            finally {
                _asyncRead.Release();
            }
        }

        /// <summary>
        /// Read from buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (buffer.Length - offset < count) {
                throw new ArgumentException("invalid offset and length");
            }

            if (IsClosed) {
                throw new IOException("Stream closed");
            }

            var readByNow = ReadBuffer(buffer, offset, count);
            if (readByNow == count) {
                return readByNow;
            }

            if (_readEnd) {
                // Indicate end of stream and reset end message marker
                _readEnd = false;
                return readByNow;
            }

            if (readByNow > 0) {
                count -= readByNow;
                offset += readByNow;
            }

            // the assumption is that we at least read 1 full fragment or up to _bufferSize
            var result = _websocket.ReceiveAsync(new ArraySegment<byte>(
                _readbuffer, 0, _bufferSize), CancellationToken.None).Result;
            _readLen = result.Count;
            _readEnd = result.EndOfMessage;
            _readPos = 0;
            return ReadBuffer(buffer, offset, count) + readByNow;
        }

        /// <summary>
        /// Read one byte
        /// </summary>
        /// <returns></returns>
        public override int ReadByte() {
            if (IsClosed) {
                throw new IOException("Stream closed");
            }

            if (_readPos == _readLen) {
                var result = _websocket.ReceiveAsync(new ArraySegment<byte>(
                    _readbuffer, 0, _bufferSize), CancellationToken.None).Result;
                _readLen = result.Count;
                _readEnd = result.EndOfMessage;
                _readPos = 0;
            }
            if (_readPos == _readLen) {
                return -1;
            }
            return _readbuffer[_readPos++];
        }

        /// <summary>
        /// Async read operation
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<int> ReadAsync(byte[] buffer, int offset,
            int count, CancellationToken cancellationToken) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (buffer.Length - offset < count) {
                throw new ArgumentException("invalid offset and length");
            }

            if (cancellationToken.IsCancellationRequested) {
                return Task.FromCanceled<int>(cancellationToken);
            }

            if (IsClosed) {
                throw new IOException("Stream closed");
            }

            var readByNow = 0;
            // lock to avoid race with other async tasks
            var semaphoreLockTask = _asyncRead.WaitAsync();
            if (semaphoreLockTask.Status == TaskStatus.RanToCompletion) {
                var completeSynchronously = true;
                try {
                    readByNow = TryReadBuffer(buffer, offset, count, out var ex);
                    completeSynchronously = (readByNow == count || ex != null);
                    if (completeSynchronously) {
                        if (ex == null) {
                            //
                            // Speed up sync reads by caching last read task
                            // if it has the same value as previous read task
                            //
                            var t = _lastRead;
                            if (t != null && t.Result == readByNow) {
                                return t;
                            }

                            t = Task.FromResult(readByNow);
                            _lastRead = t;
                            return t;
                        }
                        return Task.FromException<int>(ex);
                    }
                }
                finally {
                    if (completeSynchronously) {
                        _asyncRead.Release();
                    }
                }
            }
            // Delegate to the async implementation.
            return ReadAsync(buffer, offset + readByNow, count - readByNow, cancellationToken,
                readByNow, semaphoreLockTask);
        }

        /// <summary>
        /// Do async read using async state machine
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="ct"></param>
        /// <param name="readByNow"></param>
        /// <param name="lockTask"></param>
        /// <returns></returns>
        private async Task<int> ReadAsync(byte[] array, int offset, int count,
            CancellationToken ct, int readByNow, Task lockTask) {
            // Must have a locked sem
            await lockTask.ConfigureAwait(false);
            try {
                // Check buffer - might have been filled while parked
                var read = ReadBuffer(array, offset, count);
                if (read == count) {
                    return readByNow + read;
                }

                if (read > 0) {
                    count -= read;
                    offset += read;
                    readByNow += read;
                }

                // Ok. We can fill the buffer:
                var result = await _websocket.ReceiveAsync(
                    new ArraySegment<byte>(_readbuffer, 0, _bufferSize),
                        ct).ConfigureAwait(false);
                _readLen = result.Count;
                _readEnd = result.EndOfMessage;
                _readPos = 0;

                // and read from it
                read = ReadBuffer(array, offset, count);
                return readByNow + read;
            }
            finally {
                // Now release
                _asyncRead.Release();
            }
        }

        /// <summary>
        /// Internal read from buffer
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private int ReadBuffer(byte[] array, int offset, int count) {
            var readbytes = _readLen - _readPos;
            if (readbytes == 0) {
                return 0;
            }

            if (readbytes > count) {
                readbytes = count;
            }

            Buffer.BlockCopy(_readbuffer, _readPos, array, offset, readbytes);
            _readPos += readbytes;
            return readbytes;
        }

        /// <summary>
        /// Safe read from buffer
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private int TryReadBuffer(byte[] array, int offset, int count, out Exception ex) {
            try {
                ex = null;
                return ReadBuffer(array, offset, count);
            }
            catch (Exception e) {
                ex = e;
                return 0;
            }
        }

        /// <summary>
        /// Write array to stream
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count) {

            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (buffer.Length - offset < count) {
                throw new ArgumentException("Invalid offset and length");
            }

            if (IsClosed) {
                throw new IOException("Stream closed");
            }
            // Fill buffer, if full, write fragment...
            while (true) {
                WriteBuffer(buffer, ref offset, ref count);
                if (_writePos < _bufferSize) {
                    return;
                }
                // Write out fragment
                _websocket.SendAsync(new ArraySegment<byte>(_writebuffer, 0, _writePos),
                    WebSocketMessageType.Binary, false,  CancellationToken.None).Wait();
                _writePos = 0;
            }
        }

        /// <summary>
        /// Write one byte
        /// </summary>
        /// <param name="value"></param>
        public override void WriteByte(byte value) {
            if (IsClosed) {
                throw new IOException("Stream closed");
            }

            if (_writePos >= _bufferSize - 1) {
                // Write out fragment
                _websocket.SendAsync(new ArraySegment<byte>(_writebuffer, 0, _writePos),
                    WebSocketMessageType.Binary, false, CancellationToken.None).Wait();
                _writePos = 0;
            }
            _writebuffer[_writePos++] = value;
        }

        /// <summary>
        /// Write array to stream async
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task WriteAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (buffer.Length - offset < count) {
                throw new ArgumentException("Invalid offset and length");
            }

            if (cancellationToken.IsCancellationRequested) {
                return Task.FromCanceled<int>(cancellationToken);
            }

            if (IsClosed) {
                throw new IOException("Stream closed");
            }
            // Do double checked locking and speed synchronized path...
            var semaphoreLockTask = _asyncWrite.WaitAsync();
            if (semaphoreLockTask.Status == TaskStatus.RanToCompletion) {
                var synchronous = true;
                try {
                    // If the write completely fits into the buffer complete here
                    synchronous = (count < (_bufferSize - _writePos));
                    if (synchronous) {
                        TryWriteBuffer(buffer, ref offset, ref count, out var ex);
                        if (ex == null) {
                            return Task.CompletedTask;
                        }
                        return Task.FromException(ex);
                    }
                }
                finally {
                    if (synchronous) {
                        _asyncWrite.Release();
                    }
                }
            }
            return WriteAsync(buffer, offset, count, cancellationToken, semaphoreLockTask);
        }

        /// <summary>
        /// Real async write using state machine.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="ct"></param>
        /// <param name="lockTask"></param>
        /// <returns></returns>
        private async Task WriteAsync(byte[] array, int offset, int count,
            CancellationToken ct, Task lockTask) {
            await lockTask.ConfigureAwait(false);
            try {
                // Fill buffer, if full, write fragment...
                while (true) {
                    WriteBuffer(array, ref offset, ref count);
                    if (_writePos < _bufferSize) {
                        return;
                    }
                    // Write out fragment
                    await _websocket.SendAsync(new ArraySegment<byte>(_writebuffer, 0, _writePos),
                        WebSocketMessageType.Binary, false, ct).ConfigureAwait(false);
                    _writePos = 0;
                }
            }
            finally {
                _asyncWrite.Release();
            }
        }

        /// <summary>
        /// Internal write to buffer
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        private void WriteBuffer(byte[] array, ref int offset, ref int count) {
            var bytesToWrite = Math.Min(_bufferSize - _writePos, count);
            if (bytesToWrite <= 0) {
                return;
            }

            Buffer.BlockCopy(array, offset, _writebuffer, _writePos, bytesToWrite);
            _writePos += bytesToWrite;
            count -= bytesToWrite;
            offset += bytesToWrite;
            _needsFlush = true;
        }

        /// <summary>
        /// Safe write to buffer
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="error"></param>
        private void TryWriteBuffer(byte[] array, ref int offset, ref int count, out Exception error) {
            try {
                error = null;
                WriteBuffer(array, ref offset, ref count);
            }
            catch (Exception ex) {
                error = ex;
            }
        }

        /// <summary>
        /// Override dispose on stream
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing) {
            try {
                if (disposing && _websocket != null) {
                    _websocket.Dispose();
                }
            }
            finally {
                _websocket = null;
                _readbuffer = null;
                _writebuffer = null;
                _lastRead = null;
                base.Dispose(disposing);
            }
        }

        private WebSocket _websocket;
        private readonly int _bufferSize;

        private SemaphoreSlim _asyncRead = new SemaphoreSlim(1, 1);
        private byte[] _readbuffer;
        private int _readPos;
        private bool _readEnd;
        private int _readLen;

        private SemaphoreSlim _asyncWrite = new SemaphoreSlim(1, 1);
        private byte[] _writebuffer;
        private int _writePos;
        private bool _needsFlush;
        private Task<int> _lastRead;
    }
}

