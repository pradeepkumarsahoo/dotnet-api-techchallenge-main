using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Api.Application.Products;
using ProductCatalog.Api.Contracts;
using ProductCatalog.Api.Domain.Products;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Controllers
{
    [ApiController]
    [Route("products")]
    public sealed class ProductsController : ControllerBase
    {
        private readonly GetSortedProductsQueryHandler _getSortedProductsQueryHandler;

        public ProductsController(GetSortedProductsQueryHandler getSortedProductsQueryHandler)
        {
            _getSortedProductsQueryHandler = getSortedProductsQueryHandler;
        }

        /// <summary>Returns the catalog sorted by <paramref name="sortOption"/> (Low, High, Ascending, Descending, Recommended).</summary>
        [HttpGet("sort")]
        [HttpGet("/sort")] // Original route, kept so existing consumers do not break.
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<ProductResponse>>> Sort([FromQuery] string? sortOption, CancellationToken cancellationToken)
        {
            if (!SortOptions.TryParse(sortOption, out var option))
            {
                ModelState.AddModelError(nameof(sortOption), $"Must be one of: {string.Join(", ", SortOptions.Names)}.");
                return ValidationProblem(ModelState);
            }

            var products = await _getSortedProductsQueryHandler.Handle(new GetSortedProductsQuery(option), cancellationToken);
            return Ok(products.Select(ProductResponse.FromDomain).ToList());
        }
    }
}
