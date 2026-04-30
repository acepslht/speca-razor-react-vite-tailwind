using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Speca.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Yarp.ReverseProxy.Configuration;

namespace Speca.Core.Extentions
{
    public static class YarpExtensions
    {
        public static void AddSpecaReverseProxy(this IServiceCollection services, IConfigurationSection appConfig)
        {
            services.AddScoped<ViteService>();

            var address = "https://localhost:" + appConfig["vite:server:port"] ?? "5173";

            services.AddReverseProxy()
                    .LoadFromMemory(
                        new[] {
                            new RouteConfig {
                                RouteId = "vite-route",
                                ClusterId = "vite-cluster",
                                Match = new RouteMatch { Path = "{**catch-all}" }
                            }
                        },
                        new[] {
                            new ClusterConfig {
                                ClusterId = "vite-cluster",
                                Destinations = new Dictionary<string, DestinationConfig> {
                                    { "d1", new DestinationConfig { Address = address } }
                                }
                            }
                        }
                    )
                    .ConfigureHttpClient((context, handler) =>
                    {
                        var socketsHandler = (SocketsHttpHandler)handler;
                        socketsHandler.SslOptions.RemoteCertificateValidationCallback =
                            (sender, certificate, chain, sslPolicyErrors) => true; // Terima semua sertifikat dev
                    });
        }

        //public static void MapSpecaReverseProxy(this IEndpointRouteBuilder endpoints, bool isDevelopment = true)
        //{
        //    if (isDevelopment)
        //    {
        //        endpoints.MapReverseProxy();
        //    }
        //}

        public static void MapSpecaReverseProxy(this IEndpointRouteBuilder endpoints, bool isDevelopment = true)
        {
            if (isDevelopment)
            {
                endpoints.MapReverseProxy();
            }
        }
    }
}
