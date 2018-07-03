// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Serializer {
    using Microsoft.Azure.IIoT.Proxy.Models;
    using MsgPack;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Net.Proxy;

    /// <summary>
    /// Custom Message serializer to use type id as discriminator
    /// </summary>
    internal class MessageSerializer : Serializer<Message> {

        public override async Task<Message> ReadAsync(Reader reader,
            SerializerContext context, CancellationToken ct) {

            var message = Message.Get();
            var members = await reader.ReadObjectHeaderAsync(ct).ConfigureAwait(false);
            if (members != 9) {
                throw new FormatException($"Unexpected number of properties {members}");
            }

            message.Version = await reader.ReadUInt32Async(ct).ConfigureAwait(false);
            if ((message.Version >> 16) != (VersionEx.Assembly.ToUInt() >> 16)) {
                throw new FormatException($"Bad message version {message.Version}");
            }

            message.Source = await context.Get<Reference>().ReadAsync(
                reader, context, ct).ConfigureAwait(false);
            message.Proxy = await context.Get<Reference>().ReadAsync(
                reader, context, ct).ConfigureAwait(false);
            message.Target = await context.Get<Reference>().ReadAsync(
                reader, context, ct).ConfigureAwait(false);

            message.SequenceId = await reader.ReadUInt32Async(ct).ConfigureAwait(false);
            message.Error = await reader.ReadInt32Async(ct).ConfigureAwait(false);
            message.IsResponse = await reader.ReadBoolAsync(ct).ConfigureAwait(false);
            message.TypeId = await reader.ReadUInt32Async(ct).ConfigureAwait(false);

            if (message.IsResponse) {
                /**/ if (message.TypeId == MessageContent.Data) {
                    message.Content = await context.Get<DataMessage>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else if (message.TypeId == MessageContent.Ping) {
                    message.Content = await context.Get<PingResponse>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else if (message.TypeId == MessageContent.Link) {
                    message.Content = await context.Get<LinkResponse>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else if (message.TypeId == MessageContent.GetOpt) {
                    message.Content = await context.Get<GetOptResponse>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else if (message.TypeId == MessageContent.Close) {
                    message.Content = await context.Get<CloseResponse>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else {
                    await reader.ReadAsync(ct).ConfigureAwait(false);
                    /**/ if (message.TypeId == MessageContent.Open) {
                        message.Content = OpenResponse.Create();
                    }
                    else if (message.TypeId == MessageContent.SetOpt) {
                        message.Content = SetOptResponse.Create();
                    }
                    else if (message.TypeId == MessageContent.Poll) {
                        message.Content = PollResponse.Create();
                    }
                    else {
                        message.Content = null;
                    }
                }
            }
            else {
                /**/ if (message.TypeId == MessageContent.Data) {
                    message.Content = await context.Get<DataMessage>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else if (message.TypeId == MessageContent.Poll) {
                    message.Content = await context.Get<PollRequest>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else if (message.TypeId == MessageContent.Ping) {
                    message.Content = await context.Get<PingRequest>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else if (message.TypeId == MessageContent.Link) {
                    message.Content = await context.Get<LinkRequest>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else if (message.TypeId == MessageContent.SetOpt) {
                    message.Content = await context.Get<SetOptRequest>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else if (message.TypeId == MessageContent.GetOpt) {
                    message.Content = await context.Get<GetOptRequest>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else if (message.TypeId == MessageContent.Open) {
                    message.Content = await context.Get<OpenRequest>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false);
                }
                else {
                    await reader.ReadAsync(ct).ConfigureAwait(false);
                    /**/ if (message.TypeId == MessageContent.Close) {
                        message.Content = CloseRequest.Create();
                    }
                    else {
                        message.Content = null;
                    }
                }
            }
            return message;
        }

        public override async Task WriteAsync(Writer writer, Message obj,
            SerializerContext context, CancellationToken ct) {

            if (obj == null) {
                await writer.WriteNilAsync(ct).ConfigureAwait(false);
                return;
            }

            await writer.WriteObjectHeaderAsync(9, ct).ConfigureAwait(false);

            await writer.WriteAsync(obj.Version, ct).ConfigureAwait(false);

            await context.Get<Reference>().WriteAsync(
                writer, obj.Source, context, ct).ConfigureAwait(false);
            await context.Get<Reference>().WriteAsync(
                writer, obj.Proxy, context, ct).ConfigureAwait(false);
            await context.Get<Reference>().WriteAsync(
                writer, obj.Target, context, ct).ConfigureAwait(false);

            await writer.WriteAsync(obj.SequenceId, ct).ConfigureAwait(false);

            await writer.WriteAsync(obj.Error, ct).ConfigureAwait(false);

            await writer.WriteAsync(obj.IsResponse, ct).ConfigureAwait(false);

            await writer.WriteAsync(obj.TypeId, ct).ConfigureAwait(false);

            /**/ if (obj.Content is DataMessage) {
                await context.Get<DataMessage>().WriteAsync(writer,
                    (DataMessage)obj.Content, context, ct).ConfigureAwait(false);
            }
            else if (obj.Content is PollRequest) {
                await context.Get<PollRequest>().WriteAsync(writer,
                    (PollRequest)obj.Content, context, ct).ConfigureAwait(false);
            }
            else if (obj.Content is PingRequest) {
                await context.Get<PingRequest>().WriteAsync(writer,
                    (PingRequest)obj.Content, context, ct).ConfigureAwait(false);
            }
            else if (obj.Content is PingResponse) {
                await context.Get<PingResponse>().WriteAsync(writer,
                    (PingResponse)obj.Content, context, ct).ConfigureAwait(false);
            }
            else if (obj.Content is LinkRequest) {
                await context.Get<LinkRequest>().WriteAsync(writer,
                    (LinkRequest)obj.Content, context, ct).ConfigureAwait(false);
            }
            else if (obj.Content is LinkResponse) {
                await context.Get<LinkResponse>().WriteAsync(writer,
                    (LinkResponse)obj.Content, context, ct).ConfigureAwait(false);
            }
            else if (obj.Content is SetOptRequest) {
                await context.Get<SetOptRequest>().WriteAsync(writer,
                    (SetOptRequest)obj.Content, context, ct).ConfigureAwait(false);
            }
            else if (obj.Content is GetOptRequest) {
                await context.Get<GetOptRequest>().WriteAsync(writer,
                    (GetOptRequest)obj.Content, context, ct).ConfigureAwait(false);
            }
            else if (obj.Content is GetOptResponse) {
                await context.Get<GetOptResponse>().WriteAsync(writer,
                    (GetOptResponse)obj.Content, context, ct).ConfigureAwait(false);
            }
            else if (obj.Content is OpenRequest) {
                await context.Get<OpenRequest>().WriteAsync(writer,
                    (OpenRequest)obj.Content, context, ct).ConfigureAwait(false);
            }
            else if (obj.Content is CloseResponse) {
                await context.Get<CloseResponse>().WriteAsync(writer,
                    (CloseResponse)obj.Content, context, ct).ConfigureAwait(false);
            }
            else if (obj.Content is IVoidMessage) {
                await writer.WriteNilAsync(ct).ConfigureAwait(false);
            }
            else {
                throw new FormatException("Bad type in content");
            }
        }
    }

    /// <summary>
    /// Custom socket address serializer to ensure format expected in native code
    /// </summary>
    internal class SocketAddressSerializer : Serializer<SocketAddress> {

        public async override Task<SocketAddress> ReadAsync(Reader reader,
            SerializerContext context, CancellationToken ct) {
            SocketAddress result = null;
            var members = await reader.ReadObjectHeaderAsync(ct).ConfigureAwait(false);
            if (members < 1) {
                throw new FormatException(
                    $"Unexpected number of properties {members}");
            }
            var family = (AddressFamily)
                await reader.ReadInt32Async(ct).ConfigureAwait(false);
            if (family == AddressFamily.Unspecified) {
                result = new AnySocketAddress();
            }
            else if (family == AddressFamily.Unix) {
                if (members < 2) {
                    throw new FormatException(
                        $"Unexpected number of properties {members}");
                }
                var path = await reader.ReadStringAsync(ct).ConfigureAwait(false);
                result = new UnixSocketAddress(path);
            }
            else if (
                family != AddressFamily.Proxy &&
                family != AddressFamily.InterNetwork &&
                family != AddressFamily.InterNetworkV6) {
                throw new FormatException($"Bad address family {family}");
            }
            else {
                if (members < 3) {
                    throw new FormatException(
                        $"Unexpected number of properties {members}");
                }
                var port = await reader.ReadUInt16Async(ct).ConfigureAwait(false);
                byte[] address;
                switch (family) {
                    case AddressFamily.InterNetwork:
                        address = await reader.ReadBinAsync(ct).ConfigureAwait(false);
                        if (address.Length != 4) {
                            throw new FormatException(
                                $"Bad v4 address size {address.Length}");
                        }
                        result = new Inet4SocketAddress(address, port);
                        break;
                    case AddressFamily.InterNetworkV6:
                        if (members < 5) {
                            throw new FormatException(
                                $"Unexpected number of properties {members}");
                        }
                        var flow = await reader.ReadUInt32Async(ct).ConfigureAwait(false);
                        address = await reader.ReadBinAsync(ct).ConfigureAwait(false);
                        if (address.Length != 16) {
                            throw new FormatException(
                                $"Bad v6 address size {address.Length}");
                        }
                        var scopeId = await reader.ReadUInt32Async(ct).ConfigureAwait(false);
                        result = new Inet6SocketAddress(address, port, flow, scopeId);
                        break;
                    case AddressFamily.Proxy:
                    default:
                        if (members < 5) {
                            throw new FormatException(
                                $"Unexpected number of properties {members}");
                        }
                        var flags = await reader.ReadUInt16Async(ct).ConfigureAwait(false);
                        var interfaceIndex = await reader.ReadInt32Async(ct).ConfigureAwait(false);
                        var host = await reader.ReadStringAsync(ct).ConfigureAwait(false);
                        result = new ProxySocketAddress(host, port, flags, interfaceIndex);
                        break;
                }
            }
            return result;
        }

        public override async Task WriteAsync(Writer writer,
            SocketAddress obj, SerializerContext context, CancellationToken ct) {
            if (obj == null) {
                await writer.WriteNilAsync(ct).ConfigureAwait(false);
                return;
            }

            switch (obj.Family) {
                case AddressFamily.InterNetwork:
                    await writer.WriteObjectHeaderAsync(3, ct).ConfigureAwait(false);
                    break;
                case AddressFamily.InterNetworkV6:
                    await writer.WriteObjectHeaderAsync(5, ct).ConfigureAwait(false);
                    break;
                case AddressFamily.Proxy:
                    await writer.WriteObjectHeaderAsync(5, ct).ConfigureAwait(false);
                    break;
                case AddressFamily.Unix:
                    await writer.WriteObjectHeaderAsync(2, ct).ConfigureAwait(false);
                    break;
                case AddressFamily.Unspecified:
                    await writer.WriteObjectHeaderAsync(1, ct).ConfigureAwait(false);
                    break;
                default:
                    throw new NotSupportedException();
            }

            await writer.WriteAsync((int)obj.Family,
                ct).ConfigureAwait(false);
            if (obj.Family == AddressFamily.Unix) {
                await writer.WriteAsync(((UnixSocketAddress)obj).Path,
                    ct).ConfigureAwait(false);
            }
            else if (obj.Family != AddressFamily.Unspecified) {
                await writer.WriteAsync(((InetSocketAddress)obj).Port,
                    ct).ConfigureAwait(false);
                switch (obj.Family) {
                    case AddressFamily.InterNetwork:
                        await writer.WriteAsync(((Inet4SocketAddress)obj).Address,
                            ct).ConfigureAwait(false);
                        break;
                    case AddressFamily.InterNetworkV6:
                        await writer.WriteAsync(((Inet6SocketAddress)obj).Flow,
                            ct).ConfigureAwait(false);
                        await writer.WriteAsync(((Inet6SocketAddress)obj).Address,
                            ct).ConfigureAwait(false);
                        await writer.WriteAsync(((Inet6SocketAddress)obj).ScopeId,
                            ct).ConfigureAwait(false);
                        break;
                    case AddressFamily.Proxy:
                        await writer.WriteAsync(((ProxySocketAddress)obj).Flags,
                            ct).ConfigureAwait(false);
                        await writer.WriteAsync(((ProxySocketAddress)obj).InterfaceIndex,
                            ct).ConfigureAwait(false);
                        await writer.WriteAsync(((ProxySocketAddress)obj).Host,
                            ct).ConfigureAwait(false);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Custom multicast option serializer to ensure format expected in native code
    /// </summary>
    internal class MulticastOptionSerializer : Serializer<IMulticastOption> {

        public async override Task<IMulticastOption> ReadAsync(Reader reader,
            SerializerContext context, CancellationToken ct) {
            IMulticastOption result = null;
            var members = await reader.ReadObjectHeaderAsync(ct).ConfigureAwait(false);
            if (members < 3) {
                throw new FormatException(
                    $"Unexpected number of properties {members}");
            }
            var family = (AddressFamily)
                await reader.ReadInt32Async(ct).ConfigureAwait(false);
            if (family != AddressFamily.InterNetwork &&
                family != AddressFamily.InterNetworkV6) {
                throw new FormatException($"Bad address family {family}");
            }
            var interfaceIndex = await reader.ReadInt32Async(ct).ConfigureAwait(false);
            var address = await reader.ReadBinAsync(ct).ConfigureAwait(false);
            if (family == AddressFamily.InterNetwork) {
                if (address.Length != 4) {
                    throw new FormatException(
                        $"Bad v4 address size {address.Length}");
                }
                result = Inet4MulticastOption.Create(interfaceIndex, address);
            }
            else {
                if (address.Length != 16) {
                    throw new FormatException(
                        $"Bad v6 address size {address.Length}");
                }
                result = Inet6MulticastOption.Create(interfaceIndex, address);
            }
            return result;
        }

        public override async Task WriteAsync(Writer writer,
            IMulticastOption obj, SerializerContext context, CancellationToken ct) {
            if (obj == null) {
                await writer.WriteNilAsync(ct).ConfigureAwait(false);
                return;
            }
            await writer.WriteObjectHeaderAsync(3, ct).ConfigureAwait(false);
            await writer.WriteAsync((int)obj.Family, ct).ConfigureAwait(false);
            switch (obj.Family) {
                case AddressFamily.InterNetwork:
                    await writer.WriteAsync(((Inet4MulticastOption)obj).InterfaceIndex,
                        ct).ConfigureAwait(false);
                    await writer.WriteAsync(((Inet4MulticastOption)obj).Address,
                        ct).ConfigureAwait(false);
                    break;
                case AddressFamily.InterNetworkV6:
                    await writer.WriteAsync(((Inet6MulticastOption)obj).InterfaceIndex,
                       ct).ConfigureAwait(false);
                    await writer.WriteAsync(((Inet6MulticastOption)obj).Address,
                        ct).ConfigureAwait(false);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    /// <summary>
    /// Custom property serializer to ensure format expected in native code
    /// </summary>
    internal class PropertySerializer : Serializer<IProperty> {

        public async override Task<IProperty> ReadAsync(Reader reader,
            SerializerContext context, CancellationToken ct) {
            IProperty result = null;
            var members = await reader.ReadObjectHeaderAsync(ct).ConfigureAwait(false);
            if (members != 2) {
                throw new FormatException(
                    $"Unexpected number of properties {members}");
            }

            var type = await reader.ReadUInt32Async(ct).ConfigureAwait(false);
            if (type == (uint)SocketOption.IpMulticastJoin ||
                type == (uint)SocketOption.IpMulticastLeave) {
                result = Property<IMulticastOption>.Create(type,
                    await context.Get<IMulticastOption>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false));
            }
            else if (type == (uint)PropertyType.FileInfo) {
                result = Property<FileInfo>.Create(type,
                    await context.Get<FileInfo>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false));
            }
            else if (type == (uint)PropertyType.AddressInfo) {
                result = Property<AddressInfo>.Create(type,
                    await context.Get<AddressInfo>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false));
            }
            else if (type == (uint)PropertyType.InterfaceInfo) {
                result = Property<InterfaceInfo>.Create(type,
                    await context.Get<InterfaceInfo>().ReadAsync(
                        reader, context, ct).ConfigureAwait(false));
            }
            else if (type < (uint)SocketOption.__prx_so_max) {
                result = Property<ulong>.Create(type,
                    await reader.ReadUInt64Async(ct).ConfigureAwait(false));
            }
            else {
                throw new FormatException($"Bad type encountered {type}");
            }
            return result;
        }

        public override async Task WriteAsync(Writer writer,
            IProperty obj, SerializerContext context, CancellationToken ct) {
            if (obj == null) {
                await writer.WriteNilAsync(ct).ConfigureAwait(false);
                return;
            }

            await writer.WriteObjectHeaderAsync(2, ct).ConfigureAwait(false);
            await writer.WriteAsync(obj.Type, ct).ConfigureAwait(false);

            if (obj.Type == (uint)SocketOption.IpMulticastJoin ||
                obj.Type == (uint)SocketOption.IpMulticastLeave) {
                await context.Get<IMulticastOption>().WriteAsync(writer,
                    ((Property<IMulticastOption>)obj).Value, context,
                        ct).ConfigureAwait(false);
            }
            else if (obj.Type == (uint)PropertyType.FileInfo) {
                await context.Get<FileInfo>().WriteAsync(writer,
                    ((Property<FileInfo>)obj).Value, context,
                        ct).ConfigureAwait(false);
            }
            else if (obj.Type == (uint)PropertyType.AddressInfo) {
                await context.Get<AddressInfo>().WriteAsync(writer,
                    ((Property<AddressInfo>)obj).Value, context,
                        ct).ConfigureAwait(false);
            }
            else if (obj.Type == (uint)PropertyType.InterfaceInfo) {
                await context.Get<InterfaceInfo>().WriteAsync(writer,
                    ((Property<InterfaceInfo>)obj).Value, context,
                        ct).ConfigureAwait(false);
            }
            else if (obj.Type < (uint)SocketOption.__prx_so_max) {
                await writer.WriteAsync(((Property<ulong>)obj).Value,
                    ct).ConfigureAwait(false);
            }
            else {
                throw new FormatException( $"Bad type encountered {obj.Type}");
            }
        }
    }
}
