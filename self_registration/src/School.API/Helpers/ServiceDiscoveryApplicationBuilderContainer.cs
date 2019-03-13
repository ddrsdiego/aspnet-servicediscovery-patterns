using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using School.API.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace School.API.Helpers
{
    public static class ServiceDiscoveryApplicationBuilderContainer
    {
        public static IApplicationBuilder UseConsulRegisterService(this IApplicationBuilder app)
        {
            var appLife = app.ApplicationServices.GetRequiredService<IApplicationLifetime>() ?? throw new ArgumentException("Missing dependency", nameof(IApplicationLifetime));
            var serviceOptions = app.ApplicationServices.GetRequiredService<IOptions<ConsulConfigOptions>>() ?? throw new ArgumentException("Missing dependency", nameof(IOptions<ConsulConfigOptions>));
            var consul = app.ApplicationServices.GetRequiredService<IConsulClient>() ?? throw new ArgumentException("Missing dependency", nameof(IConsulClient));
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();

            var logger = loggerFactory.CreateLogger("ServiceDiscoveryBuilder");
            var serviceName = Assembly.GetEntryAssembly().GetName().Name.ToLowerInvariant();

            if (string.IsNullOrEmpty(serviceName))
                throw new ArgumentException("Service Name must be configured", nameof(serviceName));

            var addresses = GetEndPoints(app);

            logger.LogInformation($"Found {addresses.Length} endpoints: {string.Join(",", addresses.Select(p => p.OriginalString))}.");

            foreach (var address in addresses)
            {
                var serviceId = GetServiceId(serviceName, address);
                var serviceChecks = GetHealthCheckingConfiguration(serviceOptions, logger, address, serviceId);
                ConfigureServiceRegister(appLife, consul, serviceName, address, serviceId, serviceChecks);

                logger.LogInformation($"Registering service {serviceId} for address {address}.");
            }

            return app;
        }

        private static Uri[] GetEndPoints(IApplicationBuilder app)
        {
            var features = app.Properties["server.Features"] as FeatureCollection;
            return features
                .Get<IServerAddressesFeature>()
                .Addresses
                .Select(p => new Uri(p)).ToArray();
        }

        private static void ConfigureServiceRegister(IApplicationLifetime appLife,
                                                     IConsulClient consul,
                                                     string serviceName,
                                                     Uri address,
                                                     string serviceId,
                                                     IEnumerable<AgentServiceCheck> serviceChecks)
        {
            var registration = new AgentServiceRegistration()
            {
                Checks = serviceChecks.ToArray(),
                Address = address.Host,
                ID = serviceId,
                Name = serviceName,
                Port = address.Port
            };

            //consul.Agent.ServiceRegister(registration).GetAwaiter().GetResult();
            //appLife.ApplicationStopping.Register(() => consul.Agent.ServiceDeregister(serviceId).GetAwaiter().GetResult());
        }

        private static IEnumerable<AgentServiceCheck> GetHealthCheckingConfiguration(IOptions<ConsulConfigOptions> serviceOptions, ILogger logger, Uri address, string serviceId)
        {
            var serviceChecks = new List<AgentServiceCheck>();

            if (!string.IsNullOrEmpty(serviceOptions.Value.HealthCheckTemplate))
            {
                var healthCheckUri = new Uri(address, serviceOptions.Value.HealthCheckTemplate).OriginalString;
                serviceChecks.Add(new AgentServiceCheck()
                {
                    Status = HealthStatus.Passing,
                    DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1),
                    Interval = TimeSpan.FromSeconds(5),
                    HTTP = healthCheckUri
                });

                logger.LogInformation($"Adding healthcheck for service {serviceId}, checking {healthCheckUri}.");
            }

            return serviceChecks;
        }

        private static string GetServiceId(string serviceName, Uri address)
            => $"{serviceName}_{address.Host}:{address.Port}".ToLowerInvariant();
    }
}