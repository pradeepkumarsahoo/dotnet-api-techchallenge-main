using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Api.Domain.HttpClients;
using ProductCatalog.Api.Domain.Product;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Controllers
{
    [Route("/products")]
    public class ProductController : ControllerBase
    {
        [HttpGet("/sort")]
        public async Task<ActionResult<IEnumerable<Product>>> Sort([FromQuery] string sortOption)
        {
            var sortedProductQueryHandler =
                new GetSortedProductQueryHandler(new ProductHttpClient(), new ShopperHistoryHttpClient());

             //Review comment 
             //Dependency Injection problem 
             //Dependencies are created with new inside the action. Startup already registers the handler and both clients, but they are never used.      
             // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
             //Controller cannot be unit-tested without hitting the real WooliesX API. Lifetimes and configuration are not managed by the container. A new handler and two clients are built on every request.
        

            var getSortedProductQuery = new GetSortedProductQuery(sortOption);
            var queryResponse = await sortedProductQueryHandler.Handle(getSortedProductQuery);
            return Ok(queryResponse.Products);
        }
    }
}