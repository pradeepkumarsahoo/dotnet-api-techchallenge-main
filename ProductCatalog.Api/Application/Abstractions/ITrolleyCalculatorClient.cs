using ProductCatalog.Api.Domain.Trolleys;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Application.Abstractions
{
    public interface ITrolleyCalculatorClient
    {
        Task<decimal> CalculateTotalAsync(Trolley trolley, CancellationToken cancellationToken);
    }
}
