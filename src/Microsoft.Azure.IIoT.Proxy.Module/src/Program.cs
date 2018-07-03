// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy.Module {
    using Microsoft.Azure.IIoT.Proxy.Module.Controllers;
    using Microsoft.Azure.IIoT.Proxy.Module.Runtime;
    using Microsoft.Azure.IIoT.Edge;
    using Microsoft.Azure.IIoT.Edge.Services;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading.Tasks;

    /// <summary>
    /// Main entry point
    /// </summary>
    public static class Program {

        /// <summary>
        /// Main entry point to run the micro service process
        /// </summary>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true)
                .Build();

            // Set up dependency injection for the module host
            var container = ConfigureContainer(config);

            RunAsync(container, config).Wait();
        }

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static async Task RunAsync(IContainer container, IConfigurationRoot config) {
            // Wait until the module unloads or is cancelled
            var tcs = new TaskCompletionSource<bool>();
            AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);

            using (var hostScope = container.BeginLifetimeScope()) {
                // BUGBUG: This creates 2 instances one in container one as scope
                var events = hostScope.Resolve<IEventEmitter>();
                var module = hostScope.Resolve<IEdgeHost>();
                while (true) {
                    try {
                        await module.StartAsync(
                            "proxy", config.GetValue<string>("site", null));

                        if (!Console.IsInputRedirected) {
                            Console.WriteLine("Press any key to exit...");
                            Console.TreatControlCAsInput = true;
                            await Task.WhenAny(tcs.Task, Task.Run(() => Console.ReadKey()));
                        }
                        else {
                            await tcs.Task;
                        }
                        return;
                    }
                    catch (Exception ex) {
                        var logger = hostScope.Resolve<ILogger>();
                        logger.Error("Error during edge run!", () => ex);
                    }
                    finally {
                        await module.StopAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        public static IContainer ConfigureContainer(IConfigurationRoot configuration) {

            var config = new Config(configuration);
            var builder = new ContainerBuilder();

            // Register logger
            builder.RegisterInstance(config.Logger)
                .AsImplementedInterfaces().SingleInstance();
            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            // Register edge framework
            builder.RegisterModule<EdgeHostModule>();

            // Register controllers
            builder.RegisterType<LinkController>()
                .AsImplementedInterfaces();
            builder.RegisterType<PingController>()
                .AsImplementedInterfaces();

            return builder.Build();
        }
    }
}
