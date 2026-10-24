using ProductCatalog.Api.Application.Abstractions;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Infrastructure.WooliesX
{
    /// <summary>Shared request/response handling so each client stays a one-liner.</summary>
    internal static class WooliesXHttpClientExtensions
    {
        public static async Task<T> GetFromWooliesXAsync<T>(this HttpClient httpClient, string resource, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await httpClient.GetAsync(resource, cancellationToken);
                return await ReadAsync<T>(response, resource, cancellationToken);
            }
            catch (HttpRequestException e)
            {
                throw new UpstreamServiceException(resource, "request could not be completed", e);
            }
        }

        public static async Task<TResponse> PostToWooliesXAsync<TRequest, TResponse>(
            this HttpClient httpClient, string resource, TRequest body, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await httpClient.PostAsJsonAsync(resource, body, cancellationToken);
                return await ReadAsync<TResponse>(response, resource, cancellationToken);
            }
            catch (HttpRequestException e)
            {
                throw new UpstreamServiceException(resource, "request could not be completed", e);
            }
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response, string resource, CancellationToken cancellationToken)
        {
            if (!response.IsSuccessStatusCode)
                throw new UpstreamServiceException(resource, response.StatusCode);

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                       ?? throw new UpstreamServiceException(resource, "response body was empty");
            }
            catch (JsonException e)
            {
                throw new UpstreamServiceException(resource, "response body was not valid JSON", e);
            }
        }
    }
}
