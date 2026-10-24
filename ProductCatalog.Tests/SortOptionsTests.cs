using FluentAssertions;
using ProductCatalog.Api.Domain.Products;
using Xunit;

namespace ProductCatalog.Tests
{
    public class SortOptionsTests
    {
        [Theory]
        [InlineData("Low", SortOption.Low)]
        [InlineData("low", SortOption.Low)]
        [InlineData("HIGH", SortOption.High)]
        [InlineData(" ascending ", SortOption.Ascending)]
        [InlineData("Descending", SortOption.Descending)]
        [InlineData("recommended", SortOption.Recommended)]
        public void ShouldParseNamesIgnoringCase(string value, SortOption expected)
        {
            SortOptions.TryParse(value, out var option).Should().BeTrue();
            option.Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Lowest")]
        [InlineData("1")]
        [InlineData("Low,High")]
        public void ShouldRejectAnythingElse(string? value)
        {
            SortOptions.TryParse(value, out _).Should().BeFalse();
        }
    }
}
