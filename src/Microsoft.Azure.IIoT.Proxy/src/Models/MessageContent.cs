// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

// Keep in sync with native layer, in particular order of members!

namespace Microsoft.Azure.IIoT.Proxy.Models {
    using System;

    /// <summary>
    /// Type of message
    /// </summary>
    public static class MessageContent {

        public static readonly uint Ping            = 10;
        public static readonly uint Link            = 12;
        public static readonly uint SetOpt          = 13;
        public static readonly uint GetOpt          = 14;
        public static readonly uint Open            = 20;
        public static readonly uint Close           = 21;
        public static readonly uint Data            = 30;
        public static readonly uint Poll            = 31;
        public static readonly uint Custom          =  0;

        /// <summary>
        /// Returns id for type
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static uint GetId(IMessageContent content) {
            if (content is DataMessage) {
                return Data;
            }
            if (content is PingRequest) {
                return Ping;
            }
            if (content is PollRequest) {
                return Poll;
            }
            if (content is PollResponse) {
                return Poll;
            }
            if (content is PingResponse) {
                return Ping;
            }
            if (content is LinkRequest) {
                return Link;
            }
            if (content is LinkResponse) {
                return Link;
            }
            if (content is SetOptRequest) {
                return SetOpt;
            }
            if (content is SetOptResponse) {
                return SetOpt;
            }
            if (content is GetOptRequest) {
                return GetOpt;
            }
            if (content is GetOptResponse) {
                return GetOpt;
            }
            if (content is OpenRequest) {
                return Open;
            }
            if (content is OpenResponse) {
                return Open;
            }
            if (content is CloseRequest) {
                return Close;
            }
            if (content is CloseResponse) {
                return Close;
            }
            return Custom;
        }

        /// <summary>
        /// Returns whether the object is a response
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static bool IsResponse(IMessageContent content) {
            return content is IResponse;
        }

        /// <summary>
        /// Returns id for type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isResponse"></param>
        /// <returns></returns>
        public static Type TypeOf(uint type, bool isResponse) {
            if (type == Ping) {
                return isResponse ? typeof(PingResponse) : typeof(PingRequest);
            }
            if (type == Link) {
                return isResponse ? typeof(LinkResponse) : typeof(LinkRequest);
            }
            if (type == SetOpt) {
                return isResponse ? typeof(SetOptResponse) : typeof(SetOptRequest);
            }
            if (type == GetOpt) {
                return isResponse ? typeof(GetOptResponse) : typeof(GetOptRequest);
            }
            if (type == Open) {
                return isResponse ? typeof(OpenResponse) : typeof(OpenRequest);
            }
            if (type == Close) {
                return isResponse ? typeof(CloseResponse) : typeof(CloseRequest);
            }
            if (type == Poll) {
                return isResponse ? typeof(PollResponse) : typeof(PollRequest);
            }
            if (type == Data) {
                return typeof(DataMessage);
            }
            return null;
        }
    }
}