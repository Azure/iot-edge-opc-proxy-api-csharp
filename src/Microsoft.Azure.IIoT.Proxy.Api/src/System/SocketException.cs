// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Proxy {
    using System;

    public class SocketException : System.Net.Sockets.SocketException {

        public SocketError ProxyErrorCode { get; private set; }

        internal string _message;
        public override string Message => _message;

        internal Exception _innerException;
        public new Exception InnerException => _innerException;

        public SocketException(string message, Exception e, 
            SocketError errorCode) : base((int)errorCode.ToSystem()) {
            ProxyErrorCode = errorCode;
            _message = message;
            _innerException = e;
        }

        public SocketException(string message, Exception e)
            : this(message, e, SocketError.Fatal) {
        }

        public SocketException(string message)
            : this(message, SocketError.Fatal) {
        }

        public SocketException(SocketError errorCode)
            : this(errorCode.ToString(), errorCode) {
        }

        public SocketException(string message, SocketError errorCode)
            : this(message, (Exception)null, errorCode) {
        }

        public SocketException(Exception e)
            : this(e.ToString(), e) {
        }

        public SocketException(AggregateException e)
            : this(e.GetCombinedExceptionMessage(), e) {
        }

        public SocketException(string message, AggregateException e)
            : this(message, e, SocketError.Fatal) {
        }

        public SocketException(string message, AggregateException e, SocketError errorCode )
            : this(message, (Exception)e?.Flatten(), errorCode) {
        }

        public override string ToString() => $"{_message} : {ProxyErrorCode}";

        internal static SocketException Create(string message, Exception e) {
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
    }
}
