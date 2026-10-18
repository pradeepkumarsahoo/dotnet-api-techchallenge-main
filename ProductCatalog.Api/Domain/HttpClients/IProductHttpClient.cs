using Flurl;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Domain.HttpClients
{
    public interface IProductHttpClient
    {
        Task<IEnumerable<Product.Product>> GetProducts();
    }

    public class ProductHttpClient : IProductHttpClient
    {
        private readonly string _productUrl = "https://dev-wooliesx-recruitment.azurewebsites.net/api/resource/products";

        public async Task<IEnumerable<Product.Product>> GetProducts()
        {
            try
            {
                var products = await _productUrl
                    .SetQueryParam("token", "25a4f06f-8fd5-49b3-a711-c013c156f8c8")
                    .WithHeader("Accept", "application/json")
                    .GetJsonAsync<Product.Product[]>();

                return products;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
// Every API Hit , No cache layer 

//	Token hard-coded in each client ,Secret lives in source control; cannot rotate or vary per environment.
//Dev, test and prod need different hosts; a URL change means a rebuild.
//Unit tests need Flurl's test hooks or the real network. No per-client timeout, base address or handlers
// . No IHttpClientFactory pooling.
//Unstructured console output, catches every exception type, and logs the full exception, whose message contains the request URL including the token.
//Any fix (timeout, error handling, headers) must be repeated in each client.

//Interface and implementation in one file, file named after the interface only;
//  infrastructure code sits in the Domain namespace.

//Not implemented, recommended next: retry and circuit-breaker policies 
// on the typed clients (for example Microsoft.Extensions.Http.Resilience on .NET 8), and a short-lived cache for the product list.