using ProductCatalog.Api.Application.Abstractions;
using ProductCatalog.Api.Domain.Trolleys;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Infrastructure.WooliesX
{
    public sealed class TrolleyCalculatorClient : ITrolleyCalculatorClient
    {
        private const string Resource = "api/resource/trolleyCalculator";
        private readonly HttpClient _httpClient;

        public TrolleyCalculatorClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<decimal> CalculateTotalAsync(Trolley trolley, CancellationToken cancellationToken) =>
            _httpClient.PostToWooliesXAsync<Trolley, decimal>(Resource, trolley, cancellationToken);
    }
}
