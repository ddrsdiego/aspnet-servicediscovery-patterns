using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace School.API.Infrastructure
{
    public class ConsulHostedService : BackgroundService
    {
        private readonly IConsulClient _consulClient;
        private readonly IOptions<ConsulConfigOptions> _consulConfig;
        private readonly ILogger<ConsulHostedService> _logger;
        private readonly IServer _server;
        private readonly IApplicationLifetime _appLife;
        private readonly List<string> _servicesId = new List<string>();

        public ConsulHostedService(IConsulClient consulClient,
                                   IOptions<ConsulConfigOptions> consulConfig,
                                   ILogger<ConsulHostedService> logger,
                                   IServer server,
                                   IApplicationLifetime appLife)
        {
            _consulClient = consulClient;
            _consulConfig = consulConfig;
            _logger = logger;
            _server = server;
            _appLife = appLife;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var features = _server.Features;
            //var addresses = features
            //    .Get<IServerAddressesFeature>()
            //    .Addresses
            //    .Select(p => new Uri(p));

            //foreach (var address in addresses)
            //{
            //    var serviceId = $"{_consulConfig.Value.ServiceName}_{address.Host}:{address.Port}".ToLowerInvariant();

            //    _servicesId.Add(serviceId);

            //    //var httpCheck = new AgentServiceCheck()
            //    //{
            //    //    DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1),
            //    //    Interval = TimeSpan.FromSeconds(1),
            //    //    HTTP = new Uri(address, "HealthCheck").OriginalString
            //    //};

            //    var registration = new AgentServiceRegistration()
            //    {
            //        Address = address.Host,
            //        ID = serviceId,
            //        Name = _consulConfig.Value.ServiceName,
            //        Port = address.Port,
            //        Tags = new[] { "Students", "Courses", "School" }
            //    };

            //    await _consulClient.Agent.ServiceRegister(registration, stoppingToken).ConfigureAwait(false);

            //    _appLife.ApplicationStopping.Register(async () => await _consulClient.Agent.ServiceDeregister(serviceId).ConfigureAwait(false));
            //}
        }

        public async override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deregistering from Consul");

            try
            {
                foreach (var serviceId in _servicesId)
                {
                    await _consulClient.Agent.ServiceDeregister(serviceId, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Deregisteration failed");
            }
        }
    }
}
