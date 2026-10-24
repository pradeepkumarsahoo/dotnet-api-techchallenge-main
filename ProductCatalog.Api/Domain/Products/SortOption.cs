using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ProductCatalog.Api.Domain.Products
{
    public enum SortOption
    {
        Low = 1,
        High,
        Ascending,
        Descending,
        Recommended
    }

    public static class SortOptions
    {
        private static readonly Dictionary<string, SortOption> ByName =
            Enum.GetValues<SortOption>().ToDictionary(option => option.ToString(), StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<string> Names { get; } = ByName.Keys.ToArray();

        /// <summary>
        /// Parses a sort option by name, ignoring case. Unlike <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>,
        /// numeric strings ("1") and flag combinations ("Low,High") are rejected.
        /// </summary>
        public static bool TryParse([NotNullWhen(true)] string? value, out SortOption option)
        {
            option = default;
            return value is not null && ByName.TryGetValue(value.Trim(), out option);
        }
    }
}
