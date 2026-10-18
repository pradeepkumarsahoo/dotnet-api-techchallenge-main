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

//Misleading routes. [Route("/products")] plus [HttpGet("/sort")]: 
//Misleading routes. [Route("/products")] plus [HttpGet("/sort")]: the leading slash makes the action ignore the controller route,
//  so the real URL is /sort, not /products/sort. 
// The leading slash on the class route is also redundant.

// sortOption binding is case-insensitive: low, LOW and Low all work.
// string sortOption with magic values ("Low", "High", …) compared case-sensitively deeper in the handler.
// Typos and different casing are not caught by the compiler. ?sortOption=low silently returns unsorted data.

//No input validation. A missing or unknown option still returns HTTP 200 with unsorted products.

//Domain model returned directly (Product is both domain type and HTTP response). /Instead of domain model , return DTO object mapping model 
