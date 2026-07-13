using System.Text.Json;
using BusinessMapNET.Core.Http;
using BusinessMapNET.Core.Models;
using Xunit;

namespace BusinessMapNET.Core.Tests.Models;

public sealed class CardSerializationTests
{
    // Mirrors the options used by the client so the tests exercise the real mapping behavior.
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Deserialize_MapsSnakeCaseProperties()
    {
        const string json = """
        {
            "card_id": 101,
            "custom_id": "ABC-1",
            "board_id": 9,
            "workflow_id": 3,
            "title": "Sample",
            "owner_user_id": 55,
            "is_blocked": 1,
            "tag_ids": [1, 2, 3]
        }
        """;

        var card = JsonSerializer.Deserialize<Card>(json, Options);

        Assert.NotNull(card);
        Assert.Equal(101, card!.CardId);
        Assert.Equal("ABC-1", card.CustomId);
        Assert.Equal(9, card.BoardId);
        Assert.Equal(3, card.WorkflowId);
        Assert.Equal(55, card.OwnerUserId);
        Assert.Equal(1, card.IsBlocked);
        Assert.Equal(new[] { 1, 2, 3 }, card.TagIds);
    }

    [Fact]
    public void Deserialize_KeepsUnknownNestedStructuresAsRawJson()
    {
        const string json = """
        { "card_id": 1, "custom_fields": [ { "field_id": 4, "value": "x" } ] }
        """;

        var card = JsonSerializer.Deserialize<Card>(json, Options);

        Assert.NotNull(card!.CustomFields);
        Assert.Equal(JsonValueKind.Array, card.CustomFields!.Value.ValueKind);
    }

    [Fact]
    public void Deserialize_IntoImmutableInitProperties_Succeeds()
    {
        // Regression guard: the response DTOs use 'init' accessors; deserialization must still work.
        const string json = """{ "card_id": 7, "title": "Immutable" }""";

        var card = JsonSerializer.Deserialize<Card>(json, Options);

        Assert.Equal(7, card!.CardId);
        Assert.Equal("Immutable", card.Title);
    }

    [Fact]
    public void Deserialize_PagedResult_MapsPaginationAndData()
    {
        const string json = """
        {
            "pagination": { "all_pages": 4, "current_page": 2, "results_per_page": 25 },
            "data": [ { "card_id": 1 }, { "card_id": 2 } ]
        }
        """;

        var page = JsonSerializer.Deserialize<PagedResult<Card>>(json, Options);

        Assert.NotNull(page);
        Assert.Equal(4, page!.Pagination!.AllPages);
        Assert.Equal(2, page.Pagination.CurrentPage);
        Assert.Equal(2, page.Data.Count);
    }
}
