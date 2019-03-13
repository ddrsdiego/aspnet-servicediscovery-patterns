using Consul;
using DnsClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using School.API.Infrastructure;
using System;
using System.Net;

namespace School.API.Helpers
{
    public static class ServiceDiscoveryContainer
    {
        public static IServiceCollection AddServiceDisvovery(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .ConfigureConsulAddress(configuration)
                .ConfigureConsulDns(configuration);
            return services;
        }

        private static IServiceCollection ConfigureConsulDns(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IDnsQuery>(p =>
            {
                var serviceConfiguration = p.GetRequiredService<IOptions<ConsulConfigOptions>>().Value;
                var client = new LookupClient(IPAddress.Parse("127.0.0.1"), 8500);

                if (serviceConfiguration.ServiceDiscovery.DnsEndpoint != null)
                    client = new LookupClient(serviceConfiguration.ServiceDiscovery.DnsEndpoint.ToIpEndPoint());

                client.EnableAuditTrail = false;
                client.UseCache = true;
                client.MinimumCacheTimeout = TimeSpan.FromSeconds(1);
                return client;
            });

            return services;
        }

        private static IServiceCollection ConfigureConsulAddress(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();

            services.Configure<ConsulConfigOptions>(configuration.GetSection("ConsulConfig"));

            services.AddSingleton<IConsulClient>(s => new ConsulClient(config =>
            {
                var serviceConfiguration = s.GetRequiredService<IOptions<ConsulConfigOptions>>().Value;

                if (!string.IsNullOrEmpty(serviceConfiguration.AddressDefault))
                    config.Address = new Uri(serviceConfiguration.AddressDefault);
                else
                    config.Address = new Uri(serviceConfiguration.ServiceDiscovery.HttpEndpoint);
            }));

            return services;
        }
    }
}