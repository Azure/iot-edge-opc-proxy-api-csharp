// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Devices.Proxy {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public static class UtilsExtensions {

        public static void AddRange<T>(this ISet<T> set, IEnumerable<T> enumerable) {
            foreach (var item in enumerable) {
                set.Add(item);
            }
        }

        public static void Append(this StringBuilder stringBuilder, byte[] bytes, int size) {
            var truncate = bytes.Length > size;
            var length = truncate ? size : bytes.Length;
            var ascii = true;
            for (var i = 0; i < length; i++) {
                if (bytes[i] <= 32 || bytes[i] > 127) {
                    ascii = false;
                    break;
                }
            }
            var content = ascii ? Encoding.ASCII.GetString(bytes, 0, length) :
                BitConverter.ToString(bytes, 0, length);
            length = content.IndexOf('\n');
            if (length > 0) {
                stringBuilder.Append(content, 0, length - 1);
            }
            else {
                stringBuilder.Append(content);
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this T obj) {
            yield return obj;
        }

        public static bool SameAs<T>(this IEnumerable<T> enumerable1, IEnumerable<T> enumerable2) {
            return new HashSet<T>(enumerable1).SetEquals(enumerable2);
        }

        public static string GetCombinedExceptionMessage(this AggregateException ae) {
            var sb = new StringBuilder();
            foreach (var e in ae.InnerExceptions) {
                sb.AppendLine(string.Concat("E: ", e.Message));
            }

            return sb.ToString();
        }

        public static SocketError GetSocketError(this Exception ex) {
            var s = GetFirstOf<SocketException>(ex);
            return s != null ? s.Error : SocketError.Fatal;
        }

        public static T GetFirstOf<T>(this Exception ex) where T : Exception {
            if (ex is T) {
                return (T)ex;
            }
            if (ex is AggregateException) {
                var ae = ((AggregateException)ex).Flatten();
                foreach (var e in ae.InnerExceptions) {
                    var found = GetFirstOf<T>(e);
                    if (found != null) {
                        return found;
                    }
                }
            }
            return null;
        }
    }
}