using BusinessMapNET.Core.Models;
using Xunit;

namespace BusinessMapNET.Core.Tests.Models;

public sealed class CardsQueryTests
{
    [Fact]
    public void ToQueryString_WithNoFilters_ReturnsEmptyString()
    {
        var query = new CardsQuery();

        Assert.Equal(string.Empty, query.ToQueryString());
    }

    [Fact]
    public void ToQueryString_WithScalarFilters_BuildsExpectedParameters()
    {
        var query = new CardsQuery
        {
            Page = 2,
            PerPage = 50,
            IsBlocked = true,
        };

        var result = query.ToQueryString();

        Assert.Contains("is_blocked=1", result, StringComparison.Ordinal);
        Assert.Contains("page=2", result, StringComparison.Ordinal);
        Assert.Contains("per_page=50", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ToQueryString_WithCollectionFilters_SerializesAsCommaSeparatedValues()
    {
        var query = new CardsQuery
        {
            BoardIds = new[] { 10, 20 },
            OwnerUserIds = new[] { 7 },
        };

        var result = query.ToQueryString();

        // Commas are URL-encoded as %2C.
        Assert.Contains("board_ids=10%2C20", result, StringComparison.Ordinal);
        Assert.Contains("owner_user_ids=7", result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(CardState.Active, "state=active")]
    [InlineData(CardState.Archived, "state=archived")]
    [InlineData(CardState.Discarded, "state=discarded")]
    public void ToQueryString_State_IsSerializedInLowercase(CardState state, string expected)
    {
        var query = new CardsQuery { State = state };

        Assert.Contains(expected, query.ToQueryString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ToQueryString_IsBlockedFalse_SerializesAsZero()
    {
        var query = new CardsQuery { IsBlocked = false };

        Assert.Contains("is_blocked=0", query.ToQueryString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ToQueryString_StartsWithQuestionMark_WhenAnyFilterPresent()
    {
        var query = new CardsQuery { Page = 1 };

        Assert.StartsWith("?", query.ToQueryString(), StringComparison.Ordinal);
    }
}
