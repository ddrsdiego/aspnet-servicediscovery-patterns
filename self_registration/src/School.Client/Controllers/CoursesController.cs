using Consul;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using School.Client.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace School.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private const string SERVICE_NAME = "school.api";
        private readonly IConsulClient _consulClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConsultServiceClient _consultServiceClient;

        public CoursesController(IConsulClient consulClient, IHttpClientFactory httpClientFactory, IConsultServiceClient consultServiceClient)
        {
            _consulClient = consulClient;
            _httpClientFactory = httpClientFactory;
            _consultServiceClient = consultServiceClient;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var endPoint = await _consultServiceClient.GetServiceInstance(SERVICE_NAME).ConfigureAwait(false);
            var courses = await _consultServiceClient.Get<IEnumerable<Course>>(SERVICE_NAME, "/api/courses").ConfigureAwait(false);
            //var simulatorRouteConfig = await _consultServiceClient.GetKV<SimulatorRouteConfig>("symbolbase.simulatorRoute").ConfigureAwait(false);

            return Ok(courses);
        }
    }

    public interface IConsultServiceClient
    {
        Task<ServiceInstance> GetServiceInstance(string serviceName);
        Task<T> Get<T>(string serviceName, string resource);
        Task<T> GetKV<T>(string serviceName);
    }

    public class ConsultServiceClient : IConsultServiceClient
    {
        private readonly IConsulClient _consulClient;
        private readonly IHttpClientFactory _httpClientFactory;

        public ConsultServiceClient(IConsulClient consulClient, IHttpClientFactory httpClientFactory)
        {
            _consulClient = consulClient;
            _httpClientFactory = httpClientFactory;
        }

        private static bool EnsureSuccessStatusCode(QueryResult<CatalogService[]> services)
            => (int)services.StatusCode >= (int)HttpStatusCode.OK && (int)services.StatusCode < (int)HttpStatusCode.MultipleChoices;

        public async Task<T> GetKV<T>(string serviceName)
        {
            var queryResult = await _consulClient.KV.Get(serviceName).ConfigureAwait(false);

            using (MemoryStream stream = new MemoryStream(queryResult.Response.Value))
            {
                return Deserialize<T>(stream);
            }
        }

        public async Task<T> Get<T>(string serviceName, string resource)
        {
            var endPoint = await GetServiceInstance(serviceName).ConfigureAwait(false);

            var uri = new Uri($"http://{endPoint.ServiceAddress}:{endPoint.ServicePort}{resource}");
            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await client.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseStatusCode = response.StatusCode;
                var responseStream = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                return JsonConvert.DeserializeObject<T>(responseStream);
            }

            return default(T);
        }

        public async Task<ServiceInstance> GetServiceInstance(string serviceName)
        {
            var services = await _consulClient.Catalog.Service(serviceName).ConfigureAwait(false);

            var endPoint = Array.Find(services.Response, x => x.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
            if (!EnsureSuccessStatusCode(services))
                return null;

            return new ServiceInstance
            {
                Address = endPoint.Address,
                Node = endPoint.Node,
                ServiceAddress = endPoint.ServiceAddress,
                ServiceEndpoint = $"{endPoint.ServiceAddress}:{endPoint.ServicePort}",
                ServiceId = endPoint.ServiceID,
                ServiceName = endPoint.ServiceName,
                ServicePort = endPoint.ServicePort,
                ServiceTags = endPoint.ServiceTags
            };
        }

        protected TOut Deserialize<TOut>(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var jsonSerializer = new JsonSerializer();
                    return jsonSerializer.Deserialize<TOut>(jsonReader);
                }
            }
        }
    }

    public class SimulatorRouteConfig
    {
        public SimulatorRoute SimulatorRoute { get; set; }
    }

    public class SimulatorRoute
    {
        public string BaseUrl { get; set; }
        public string FixedIncomeRentabilityTypeFixed { get; set; }
        public string FixedIncomeRentabilityTypeIndexed { get; set; }
        public string FixedIncomeRentabilityTypeFixedAndIndexed { get; set; }
    }

    public class RiskModeConfig
    {
        public RiskMode RiskMode { get; set; }
    }

    public class RiskMode
    {
        public IEnumerable<int> Block { get; set; }
    }

    public class ServiceInstance
    {
        public string Node { get; set; }
        public string Address { get; set; }
        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceAddress { get; set; }
        public int ServicePort { get; set; }
        public string ServiceEndpoint { get; set; }
        public string[] ServiceTags { get; set; }
    }
}