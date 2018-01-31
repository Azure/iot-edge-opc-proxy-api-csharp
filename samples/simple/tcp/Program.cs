﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Devices.Proxy.Samples {
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Proxy;
    using System.Threading;
    using System.Diagnostics;
    using System.IO;

    class Program {

        static readonly string hostName = System.Net.Dns.GetHostName();
        static Random rand = new Random();

        enum Op {
            None, Perf, Sync, Async, All
        }

        ///
        /// <summary>
        ///
        /// Simple TCP/IP Services
        ///
        /// Port 7:  Echo http://tools.ietf.org/html/rfc862
        /// Port 19: Chargen http://tools.ietf.org/html/rfc864
        /// Port 13: daytime http://tools.ietf.org/html/rfc867
        /// Port 17: quotd http://tools.ietf.org/html/rfc865
        ///
        /// </summary>
        static void Main(string[] args) {
            var op = Op.None;
            var bypass = false;
            var index = 0;
            var timeout = 10;
            var bufferSize = 60000;

            // Parse command line
            try {
                for (var i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "--all":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutual exclusive");
                            }
                            op = Op.All;
                            break;
                        case "-s":
                        case "--sync":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutual exclusive");
                            }
                            op = Op.Sync;
                            break;
                        case "-a":
                        case "--async":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutual exclusive");
                            }
                            op = Op.Async;
                            break;
                        case "-p":
                        case "--perf":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutual exclusive");
                            }
                            op = Op.Perf;
                            break;
                        case "-t":
                        case "--timeout":
                            i++;
                            if (i >= args.Length || !int.TryParse(args[i], out timeout)) {
                                throw new ArgumentException($"Bad -t arg");
                            }
                            break;
                        case "-b":
                        case "--start-at":
                            i++;
                            if (i >= args.Length || !int.TryParse(args[i], out index)) {
                                throw new ArgumentException($"Bad -b arg");
                            }
                            break;
                        case "-z":
                        case "--buffer-size":
                            i++;
                            if (i >= args.Length || !int.TryParse(args[i], out bufferSize)) {
                                throw new ArgumentException($"Bad -z arg");
                            }
                            break;
                        case "--bypass":
                            bypass = true;
                            break;
                        case "-R":
                        case "--relay":
                            Socket.Provider = Provider.RelayProvider.CreateAsync().Result;
                            break;
                        case "-W":
                        case "--websocket":
                            Provider.WebSocketProvider.Create();
                            break;
                        case "-?":
                        case "-h":
                        case "--help":
                            throw new ArgumentException("Help");
                        default:
                            throw new ArgumentException($"Unknown {args[i]}");
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    @"
Simple - Proxy .net simple tcp/ip sample.  
usage:       Simple [options] operation [args]

Options:
    --relay
     -R      Use relay provider instead of default provider.
    --websocket
     -W      Use websocket kestrel provider.
    --buffer-size
     -z      Specifies the buffer size for performance tests.
             Defaults to 60000 bytes.
    --start-at
     -b      Begins the test loop at this index. Defaults to 0.
    --timeout
     -t      Async tests timeout in minutes. Defaults to 10.

    --help
     -?
     -h      Prints out this help.

Operations (Mutually exclusive):
    --all
             Run async and sync tests (default).
    --sync
     -s      Run sync tests
    --async
     -a      Run async tests
    --perf
     -p      Run performance tests.
"
                    );
                return;
            }

            if (op == Op.None) {
                op = Op.All;
            }

            if (op == Op.Perf) {
                Console.Clear();
                try {
                    if (bypass) {
                        PerfLoopComparedAsync(bufferSize, CancellationToken.None).Wait();
                    }
                    else {
                        PerfLoopAsync(bufferSize, CancellationToken.None).Wait();
                    }
                }
                catch (Exception e) {
                    Console.Out.WriteLine($"{e.Message}");
                    Thread.Sleep(4000);
                }
            }
            else {
                for (var j = index + 1; ; j++) {
                    if (op == Op.Sync || op == Op.All) {
                        Console.Clear();
                        Console.Out.WriteLine($"#{j} Sync tests...");
                        try {
                            SendReceive(7, Encoding.UTF8.GetBytes("Simple test to echo server"));
                            Receive(19);
                            Receive(13);
                            Receive(17);
                            Receive(19);
                            EchoLoop(j + 1);
                        }
                        catch (Exception e) {
                            Console.Out.WriteLine($"{e.Message}");
                            Thread.Sleep(4000);
                        }
                    }
                    if (op == Op.Async || op == Op.All) {
                        Console.Clear();
                        Console.Out.WriteLine($"#{j} Async tests...");
                        var tasks = new List<Task>();
                        try {
                            for (var i = 0; i < j + 1; i++) {
                                tasks.Add(ReceiveAsync(i, 19));
                                tasks.Add(ReceiveAsync(i, 13));
                                tasks.Add(ReceiveAsync(i, 17));
                                tasks.Add(EchoLoopAsync(i, 5));
                            }

                            Task.WaitAll(tasks.ToArray(),
                                new CancellationTokenSource(TimeSpan.FromMinutes(timeout)).Token);
                            Console.Out.WriteLine($"#{j} ... complete!");
                        }
                        catch (OperationCanceledException) {
                            foreach (var pending in tasks) {
                                Console.Out.WriteLine(
                                    $"{pending}  did not complete after {timeout} minutes.");
                            }
                            Thread.Sleep(4000);
                        }
                        catch (Exception e) {
                            Console.Out.WriteLine($"{e.Message}");
                            Thread.Sleep(4000);
                        }
                    }
                }
            }

            Console.WriteLine("Press a key to exit...");
            Console.ReadKey();
        }

        public static void Receive(int port) {
            var buffer = new byte[1024];
            using (var s = new Socket(SocketType.Stream, ProtocolType.Tcp)) {
                s.Connect(hostName, port);
                Console.Out.WriteLine($"Receive: Connected to {s.RemoteEndPoint} on {s.InterfaceEndPoint} via {s.LocalEndPoint}!");
                Console.Out.WriteLine("Receive: Receiving sync...");
                var count =  s.Receive(buffer);
                Console.Out.WriteLine(Encoding.UTF8.GetString(buffer, 0, count));
                s.Close();
            }
        }

        public static void EchoLoop(int loops) {
            using (var s = new Socket(SocketType.Stream, ProtocolType.Tcp)) {
                s.Connect(hostName, 7);
                Console.Out.WriteLine($"EchoLoop: Connected to {s.RemoteEndPoint} on {s.InterfaceEndPoint} via {s.LocalEndPoint}!");

                for (var i = 0; i < loops; i++) {
                    s.Send(Encoding.UTF8.GetBytes($"EchoLoop: {i} sync loop to echo server"));
                    var buffer = new byte[1024];
                    Console.Out.WriteLine($"EchoLoop: {i}:        Receiving sync...");
                    var count = s.Receive(buffer);
                    Console.Out.WriteLine(Encoding.UTF8.GetString(buffer, 0, count));
                }
                s.Close();
            }
        }

        public static void Send(int port, byte[] buffer, int iterations) {
            using (var s = new Socket(SocketType.Stream, ProtocolType.Tcp)) {
                s.Connect(hostName, port);
                Console.Out.WriteLine($"Send: Connected to {s.RemoteEndPoint} on {s.InterfaceEndPoint} via {s.LocalEndPoint}!");
                Console.Out.WriteLine("Send: Sending sync ...");
                for (var i = 0; i < iterations; i++) {
                    s.Send(buffer);
                }

                s.Close();
            }
        }

        public static void SendReceive(int port, byte[] buffer) {
            using (var s = new Socket(SocketType.Stream, ProtocolType.Tcp)) {
                s.Connect(hostName, port);
                Console.Out.WriteLine($"SendReceive: Connected to {s.RemoteEndPoint} on {s.InterfaceEndPoint} via {s.LocalEndPoint}!");
                Console.Out.WriteLine("SendReceive: Sending sync ...");
                s.Send(buffer);
                buffer = new byte[1024];
                Console.Out.WriteLine("SendReceive: Receiving sync...");
                var count = s.Receive(buffer);
                Console.Out.WriteLine(Encoding.UTF8.GetString(buffer, 0, count));
                s.Close();
            }
        }

        public static async Task EchoLoopAsync(int index, int loops) {
            using (var s = new Socket(SocketType.Stream, ProtocolType.Tcp)) {
                await s.ConnectAsync(hostName, 7, CancellationToken.None);
                Console.Out.WriteLine($"EchoLoopAsync #{index}: Connected!");
                for (var i = 0; i < loops; i++) {
                    await EchoLoopAsync1(index, s, i);
                }
                await s.CloseAsync(CancellationToken.None);
            }
            Console.Out.WriteLine($"EchoLoopAsync #{index}.  Done!");
        }

        public static async Task PerfLoopAsync(int bufferSize, CancellationToken ct) {
            var port = 5000;
            var cts = new CancellationTokenSource();
            var server = PerfEchoServer(port, cts.Token);
            await Task.Delay(100);
            try {
                using (var client = new TcpClient()) {
                    await client.ConnectAsync(hostName, port);
                    var buffer = new byte[bufferSize];
                    _rand.NextBytes(buffer);
                    long _received = 0;
                    var _receivedw = Stopwatch.StartNew();
                    for (var i = 0; !ct.IsCancellationRequested; i++) {
                        _received += await EchoLoopAsync2(client.GetStream(), buffer);
                        Console.CursorLeft = 0; Console.CursorTop = 0;
                        Console.Out.WriteLine($"{i} { (_received / _receivedw.ElapsedMilliseconds) } kB/sec");
                    }
                }
            }
            finally {
                cts.Cancel();
                await server;
            }
        }

        public static async Task PerfLoopComparedAsync(int bufferSize, CancellationToken ct) {
            var port = 5000;
            var cts = new CancellationTokenSource();
            var server = PerfEchoServer(port, cts.Token);
            await Task.Delay(100);
            try {
                using (var client = new System.Net.Sockets.TcpClient()) {
                    await client.ConnectAsync(hostName, port);
                    var buffer = new byte[bufferSize];
                    _rand.NextBytes(buffer);
                    long _received = 0;
                    var _receivedw = Stopwatch.StartNew();
                    for (var i = 0; !ct.IsCancellationRequested; i++) {
                        _received += await EchoLoopAsync2(client.GetStream(), buffer);
                        Console.CursorLeft = 0; Console.CursorTop = 0;
                        Console.Out.WriteLine($"{i} { (_received / _receivedw.ElapsedMilliseconds) } kB/sec");
                    }
                }
            }
            finally {
                cts.Cancel();
                await server;
            }
        }

        public static async Task PerfEchoServer(int port, CancellationToken ct) {
            var server = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
            server.Start();
            try {
                await Task.Run(async () => {
                    var client = await server.AcceptSocketAsync();
                    var buf = new byte[0x10000];
                    var rcvbyte = 0;

                    while ((rcvbyte = client.Receive(buf, buf.Length, System.Net.Sockets.SocketFlags.None)) > 0) {
                        client.Send(buf, rcvbyte, System.Net.Sockets.SocketFlags.None);
                    }
                }, ct);
            }
            catch {}
            finally {
                server.Stop();
            }
        }

        private static async Task<int> EchoLoopAsync2(Stream stream, byte[] msg) {
            await stream.WriteAsync(msg, 0, msg.Length, CancellationToken.None);
            var buffer = new byte[msg.Length];
            try {
                var offset = 0;
                while (offset < buffer.Length) {
                    var rcvbyte = await stream.ReadAsync(buffer, offset, buffer.Length - offset);
                    offset += rcvbyte;
                }
#if TEST
                if (!buffer.SameAs(msg)) {
                    throw new Exception("Bad echo returned!");
                }
#endif
                return offset;
            }
            catch {
                Console.Out.WriteLine("Failed to receive echo buffer");
                throw;
            }
        }

        public static async Task ReceiveAsync(int index, int port) {
            var buffer = new byte[1024];
            using (var client = new TcpClient()) {
                await client.ConnectAsync(hostName, port, CancellationToken.None);
                Console.Out.WriteLine($"ReceiveAsync #{index}: Connected to port {port}!.  Read ...");
                using (var str = client.GetStream()) {
                    var read = await str.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
                    Console.Out.WriteLine($"{Encoding.UTF8.GetString(buffer, 0, read)}     #{index}");
                }
            }
            Console.Out.WriteLine($"ReceiveAsync #{index} port {port}.  Done!");
        }

        public static async Task SendReceiveAsync(int index, int port, byte[] buffer) {
            using (var client = new TcpClient()) {
                await client.ConnectAsync(hostName, port, CancellationToken.None);
                Console.Out.WriteLine($"SendReceiveAsync #{index}: Connected to port {port}!.  Write ...");
                var str = client.GetStream();
                await str.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None);
                var read = await str.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
                Console.Out.WriteLine(Encoding.UTF8.GetString(buffer, 0, read));
            }
            Console.Out.WriteLine($"SendReceiveAsync #{index} port {port}.  Done!");
        }

        private static async Task EchoLoopAsync1(int index, Socket s, int i) {
            var id = (ushort)(short.MaxValue * _rand.NextDouble());
            var msg = Encoding.UTF8.GetBytes(
                string.Format("{0,6} async loop #{1} to echo server {2}", i, index, id));
            await s.SendAsync(msg, 0, msg.Length, CancellationToken.None);
            var buffer = new byte[msg.Length];
            try {
                var count = await s.ReceiveAsync(buffer, 0, buffer.Length);
                Console.Out.WriteLine("({1,6}) received '{0}' ... (#{2}, {3})",
                    Encoding.UTF8.GetString(buffer, 0, count), i, index, id);
#if TEST
                if (!buffer.SameAs(msg)) {
                    throw new Exception("Bad echo returned!");
                }
#endif
            }
            catch {
                Console.Out.WriteLine("Failed to receive {1,6} '{0}'... (#{2}, {3})",
                    Encoding.UTF8.GetString(msg, 0, msg.Length), i, index, id);
                throw;
            }
        }
        static Random _rand = new Random();
    }
}
