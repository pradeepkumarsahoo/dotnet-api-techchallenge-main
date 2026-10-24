using Microsoft.Extensions.Options;
using ProductCatalog.Api.Application.Abstractions;
using ProductCatalog.Api.Domain.Products;
using ProductCatalog.Api.Domain.Trolleys;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Tests.Stubs
{
    public sealed class StubProductCatalogClient : IProductCatalogClient
    {
        public IReadOnlyList<Product> Products { get; set; } = [];

        public Exception? Failure { get; set; }

        public int Calls { get; private set; }

        public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Failure is null ? Task.FromResult(Products) : Task.FromException<IReadOnlyList<Product>>(Failure);
        }
    }

    public sealed class StubShopperHistoryClient : IShopperHistoryClient
    {
        public IReadOnlyList<ShopperHistory> History { get; set; } = [];

        public int Calls { get; private set; }

        public Task<IReadOnlyList<ShopperHistory>> GetShopperHistoryAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(History);
        }
    }

    public sealed class StubTrolleyCalculatorClient : ITrolleyCalculatorClient
    {
        public decimal Total { get; set; }

        public Trolley? ReceivedTrolley { get; private set; }

        public Task<decimal> CalculateTotalAsync(Trolley trolley, CancellationToken cancellationToken)
        {
            ReceivedTrolley = trolley;
            return Task.FromResult(Total);
        }
    }

    public sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T value)
        {
            CurrentValue = value;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
