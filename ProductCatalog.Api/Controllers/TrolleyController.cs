using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Api.Application.Abstractions;
using ProductCatalog.Api.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Controllers
{
    [ApiController]
    public sealed class TrolleyController : ControllerBase
    {
        private readonly ITrolleyCalculatorClient _trolleyCalculatorClient;

        public TrolleyController(ITrolleyCalculatorClient trolleyCalculatorClient)
        {
            _trolleyCalculatorClient = trolleyCalculatorClient;
        }

        /// <summary>Calculates the lowest total for a trolley, applying any specials.</summary>
        [HttpPost("trolleyTotal")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<decimal>> TrolleyTotal([FromBody] TrolleyRequest request, CancellationToken cancellationToken) =>
            Ok(await _trolleyCalculatorClient.CalculateTotalAsync(request.ToDomain(), cancellationToken));
    }
}
