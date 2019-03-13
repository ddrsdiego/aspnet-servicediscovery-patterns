using System.Net;

namespace School.API.Infrastructure
{
    public class ConsulConfigOptions
    {
        public string AddressDefault { get; set; }
        public string HealthCheckTemplate { get; set; }
        public ServiceDiscovery ServiceDiscovery { get; set; }
    }

    public class ServiceDiscovery
    {
        public string HttpEndpoint { get; set; }
        public DnsEndpoint DnsEndpoint { get; set; }
    }

    public class DnsEndpoint
    {
        public int Port { get; set; }
        public string Address { get; set; }

        public IPEndPoint ToIpEndPoint() 
            => new IPEndPoint(IPAddress.Parse(Address), Port);
    }
}