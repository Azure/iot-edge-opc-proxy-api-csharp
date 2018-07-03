// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Proxy {
    using System;

    public class SocketException : Sockets.SocketException {

        /// <summary>
        /// Returns the original proxy socket error code
        /// </summary>
        public SocketError ProxyErrorCode { get; private set; }

        /// <inheritdoc/>
        public override string Message => _message;

        /// <inheritdoc/>
        public new Exception InnerException => _innerException;

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="e"></param>
        /// <param name="errorCode"></param>
        public SocketException(string message, Exception e,
            SocketError errorCode) : base((int)errorCode.ToSocketsSocketError()) {
            ProxyErrorCode = errorCode;
            _message = message;
            _innerException = e;
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="e"></param>
        public SocketException(string message, Exception e)
            : this(message, e, SocketError.Fatal) {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        public SocketException(string message)
            : this(message, SocketError.Fatal) {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="errorCode"></param>
        public SocketException(SocketError errorCode)
            : this(errorCode.ToString(), errorCode) {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="errorCode"></param>
        public SocketException(string message, SocketError errorCode)
            : this(message, (Exception)null, errorCode) {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="e"></param>
        public SocketException(Exception e)
            : this(e.ToString(), e) {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="e"></param>
        public SocketException(AggregateException e)
            : this(e.GetCombinedExceptionMessage(), e) {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="e"></param>
        public SocketException(string message, AggregateException e)
            : this(message, e, SocketError.Fatal) {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="e"></param>
        /// <param name="errorCode"></param>
        public SocketException(string message, AggregateException e, SocketError errorCode )
            : this(message, (Exception)e?.Flatten(), errorCode) {
        }

        /// <inheritdoc/>
        public override string ToString() => $"{_message} : {ProxyErrorCode}";

        /// <summary>
        /// Helper to create exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static SocketException Create(string message, Exception e) {
            if (e is SocketException sex) {
                return sex;
            }
            if (e is AggregateException aex) {
                var s = aex.GetFirstOf<SocketException>();
                if (s != null) {
                    return s;
                }
            }
            if (e.InnerException != null) {
                return Create(message, e.InnerException);
            }
            return new SocketException(message, e);
        }

        protected string _message;
        protected Exception _innerException;
    }
}
