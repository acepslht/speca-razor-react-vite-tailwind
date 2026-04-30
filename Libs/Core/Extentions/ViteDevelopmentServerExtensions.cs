using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Speca.Core.Helpers;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Speca.Core.Extentions
{
    public static class ViteDevelopmentServerExtensions
    {
        /// <summary>
        /// Handles requests by passing them through to an instance of the create-react-app server.
        /// This means you can always serve up-to-date CLI-built resources without having
        /// to run the create-react-app server manually.
        ///
        /// This feature should only be used in development. For production deployments, be
        /// sure not to enable the create-react-app server.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="npmScript">The name of the script in your package.json file that launches the create-react-app server.</param>
        public static void UseViteDevelopmentServer(
            this ISpaBuilder spaBuilder,
            string scriptName,
            string schema = "http")
        {
            ArgumentNullException.ThrowIfNull(spaBuilder);

            var spaOptions = spaBuilder.Options;
            if (string.IsNullOrEmpty(spaOptions.SourcePath))
            {
                throw new InvalidOperationException($"To use {nameof(UseViteDevelopmentServer)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            ViteDevelopmentServerMiddleware.Attach(spaBuilder, scriptName, schema);
        }
    }

    internal static class ViteDevelopmentServerMiddleware
    {
        private const string LogCategoryName = "ViteDevServer";
        private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5); // This is a development-time only feature, so a very long timeout is fine

        public static void Attach(
            ISpaBuilder spaBuilder,
            string scriptName,
            string schema = "http")
        {
            var pkgManagerCommand = spaBuilder.Options.PackageManagerCommand;
            var sourcePath = spaBuilder.Options.SourcePath;
            int devServerPort = spaBuilder.Options.DevServerPort;
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("Property 'SourcePath' cannot be null or empty", nameof(spaBuilder));
            }

            if (string.IsNullOrEmpty(scriptName))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(scriptName));
            }

            // Start create-react-app and attach to middleware pipeline
            var appBuilder = spaBuilder.ApplicationBuilder;
            var applicationStoppingToken = appBuilder.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
            var loggerFactory = appBuilder.ApplicationServices.GetService<ILoggerFactory>();
            var logger = loggerFactory != null
                ? loggerFactory.CreateLogger(LogCategoryName)
                : NullLogger.Instance;
            var diagnosticSource = appBuilder.ApplicationServices.GetRequiredService<DiagnosticSource>();
            var portTask = StartCreateReactAppServerAsync(sourcePath, scriptName, pkgManagerCommand, devServerPort, logger, diagnosticSource, applicationStoppingToken);

            spaBuilder.UseProxyToSpaDevelopmentServer(async () =>
            {
                // On each request, we create a separate startup task with its own timeout. That way, even if
                // the first request times out, subsequent requests could still work.
                var timeout = spaBuilder.Options.StartupTimeout;

                Task waitTimeout = Task.Delay(timeout, applicationStoppingToken);
                await Task.WhenAny(portTask, waitTimeout);
                if (waitTimeout.IsCompletedSuccessfully)
                {
                    Debug.WriteLine("The create-react-app server did not start listening for requests " +
                    $"within the timeout period of {timeout.TotalSeconds} seconds. " +
                    "Check the log output for error information.");
                }

                if (portTask.IsCompletedSuccessfully)
                {
                    return new UriBuilder(schema, "localhost", portTask.Result).Uri;
                }

                // Everything we proxy is hardcoded to target http://localhost because:
                // - the requests are always from the local machine (we're not accepting remote
                //   requests that go directly to the create-react-app server)
                // - given that, there's no reason to use https, and we couldn't even if we
                //   wanted to, because in general the create-react-app server has no certificate
                return new UriBuilder(schema, "localhost", devServerPort).Uri;
            });
        }

        private static async Task<int> StartCreateReactAppServerAsync(
            string sourcePath, string scriptName, string pkgManagerCommand, int portNumber, ILogger logger, DiagnosticSource diagnosticSource, CancellationToken applicationStoppingToken)
        {
            if (portNumber == default)
            {
                portNumber = FindAvailablePort();
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation($"Starting create-react-app server on port {portNumber}...");
            }

            var envVars = new Dictionary<string, string>
            {
                { "PORT", portNumber.ToString(CultureInfo.InvariantCulture) },
                { "BROWSER", "none" }, // We don't want create-react-app to open its own extra browser window pointing to the internal dev server port
            };
            var scriptRunner = new NodeScriptRunner(
                sourcePath, scriptName, null, envVars, pkgManagerCommand, diagnosticSource, applicationStoppingToken);
            scriptRunner.AttachToLogger(logger);

            using (var stdErrReader = new EventedStreamStringReader(scriptRunner.StdErr))
            {
                try
                {
                    // Although the React dev server may eventually tell us the URL it's listening on,
                    // it doesn't do so until it's finished compiling, and even then only if there were
                    // no compiler warnings. So instead of waiting for that, consider it ready as soon
                    // as it starts listening for requests.
                    await scriptRunner.StdOut.WaitForMatch(
                        new Regex("VITE.*ready in.*", RegexOptions.None, RegexMatchTimeout));
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        $"The {pkgManagerCommand} script '{scriptName}' exited without indicating that the " +
                        "create-react-app server was listening for requests. The error output was: " +
                        $"{stdErrReader.ReadAsString()}", ex);
                }
            }

            return portNumber;
        }

        internal static int FindAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
