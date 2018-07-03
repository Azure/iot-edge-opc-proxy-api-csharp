// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Module.Controllers {
    using Microsoft.Azure.IIoT.Proxy.Module.Filters;
    using Microsoft.Azure.IIoT.Proxy.Models;
    using Microsoft.Azure.IIoT.Proxy.Server;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Edge;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Proxy controller handling link protocol
    /// </summary>
    [Version(1)]
    [ExceptionsFilter]
    public class LinkController : IMethodController {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="sockets"></param>
        /// <param name="logger"></param>
        public LinkController(IProxyLinkManager sockets, ILogger logger) {
            _sockets = sockets ?? throw new ArgumentNullException(nameof(sockets));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handle link request
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<Message> LinkAsync(Message message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            if (!(message.Content is LinkRequest request)) {
                throw new ArgumentException("Must have link request content.");
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (Reference.Null == message.Target) {
                throw new ArgumentException("Must have empty target.");
            }
            try {
                var link = await _sockets.CreateAsync(request.Properties);
                return Message.Create(message, LinkResponse.Create(
                    link.RemoteId, link.LocalAddress, link.PeerAddress, 0));
            }
            catch (Exception ex) {
                return Message.Create(message, LinkResponse.Get(), ex);
            }
        }

        /// <summary>
        /// Handle open request
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<Message> OpenAsync(Message message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            if (!(message.Content is OpenRequest request)) {
                throw new ArgumentException("Must have open request content.");
            }
            try {
                await _sockets.OpenAsync(message.Target, request.StreamId,
                    request.ConnectionString, request.Encoding);
                return Message.Create(message, OpenResponse.Create());
            }
            catch (Exception ex) {
                return Message.Create(message, OpenResponse.Get(), ex);
            }
        }

        /// <summary>
        /// Handle getting options
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<Message> GetOptAsync(Message message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            if (!(message.Content is GetOptRequest request)) {
                throw new ArgumentException("Must have getopt request content.");
            }
            try {
                var property = await _sockets.GetOptionAsync(message.Target,
                    request.Option);
                return Message.Create(message, GetOptResponse.Create(property));
            }
            catch (Exception ex) {
                return Message.Create(message, GetOptResponse.Get(), ex);
            }
        }

        /// <summary>
        /// Handle setting options
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<Message> SetOptAsync(Message message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            if (!(message.Content is SetOptRequest request)) {
                throw new ArgumentException("Must have setopt request content.");
            }
            try {
                await _sockets.SetOptionAsync(message.Target,
                    request.OptionValue);
                return Message.Create(message, SetOptResponse.Create());
            }
            catch (Exception ex) {
                return Message.Create(message, SetOptResponse.Get(), ex);
            }
        }

        /// <summary>
        /// Handle polling
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task<Message> PollAsync(Message message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            // Legacy - not supported
            var msg = Message.Create(message,
                PollResponse.Get(), (int)System.Net.Proxy.SocketError.NotSupported);
            return Task.FromResult(msg);
        }

        /// <summary>
        /// Handle send message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task<Message> DataAsync(Message message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            // Legacy - not supported
            var msg = Message.Create(message,
                DataMessage.Get(), (int)System.Net.Proxy.SocketError.NotSupported);
            return Task.FromResult(msg);
        }

        /// <summary>
        /// Handle close request
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<Message> CloseAsync(Message message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            if (!(message.Content is CloseRequest request)) {
                throw new ArgumentException("Must have close request content.");
            }
            try {
                var stats = await _sockets.CloseAsync(message.Target);
                return Message.Create(message, CloseResponse.Create(
                    stats.TimeOpenInMilliseconds, stats.BytesSent,
                        stats.BytesReceived, stats.ErrorCode));
            }
            catch (Exception ex) {
                return Message.Create(message, CloseResponse.Get(), ex);
            }
        }

        private readonly IProxyLinkManager _sockets;
        private readonly ILogger _logger;
    }
}
