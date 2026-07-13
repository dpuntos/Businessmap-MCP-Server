using System.Net;
using BusinessMapNET.Core.Http;
using BusinessMapNET.Core.Models;
using BusinessMapNET.Core.Services;
using BusinessMapNET.Core.Tests.TestHelpers;
using Xunit;

namespace BusinessMapNET.Core.Tests.Services;

public sealed class CardsApiTests
{
    [Fact]
    public async Task GetCardAsync_UnwrapsDataEnvelope()
    {
        const string json = """
        { "data": { "card_id": 42, "title": "Hello", "board_id": 7, "is_blocked": 1 } }
        """;
        using var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, json);
        var api = new CardsApi(handler.CreateClient());

        var card = await api.GetCardAsync(42);

        Assert.Equal(42, card.CardId);
        Assert.Equal("Hello", card.Title);
        Assert.Equal(7, card.BoardId);
        Assert.Equal(1, card.IsBlocked);
    }

    [Fact]
    public async Task GetCardAsync_RequestsExpectedRelativeUri()
    {
        using var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "data": { "card_id": 5 } }""");
        var api = new CardsApi(handler.CreateClient());

        await api.GetCardAsync(5);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("https://test.kanbanize.com/api/v2/cards/5", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetCardsAsync_AppendsQueryString()
    {
        using var handler = StubHttpMessageHandler.Json(
            HttpStatusCode.OK,
            """{ "data": { "data": [ { "card_id": 1 } ], "pagination": { "all_pages": 1, "current_page": 1, "results_per_page": 25 } } }""");
        var api = new CardsApi(handler.CreateClient());

        var result = await api.GetCardsAsync(new CardsQuery { BoardIds = new[] { 3 }, Page = 1 });

        Assert.Single(result.Data);
        Assert.Equal(1, result.Pagination!.CurrentPage);
        var uri = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("cards?", uri, StringComparison.Ordinal);
        Assert.Contains("board_ids=3", uri, StringComparison.Ordinal);
        Assert.Contains("page=1", uri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetCardsAsync_UnwrapsNestedDataEnvelope()
    {
        // The v2 /cards endpoint nests the paged result inside an outer "data" envelope:
        // { "data": { "pagination": { ... }, "data": [ ... ] } }.
        const string json = """
        {
          "data": {
            "pagination": { "all_pages": 3, "current_page": 2, "results_per_page": 10 },
            "data": [ { "card_id": 11, "title": "First" }, { "card_id": 12, "title": "Second" } ]
          }
        }
        """;
        using var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, json);
        var api = new CardsApi(handler.CreateClient());

        var result = await api.GetCardsAsync();

        Assert.Equal(2, result.Data.Count);
        Assert.Equal(11, result.Data[0].CardId);
        Assert.Equal("Second", result.Data[1].Title);
        Assert.NotNull(result.Pagination);
        Assert.Equal(3, result.Pagination!.AllPages);
        Assert.Equal(2, result.Pagination.CurrentPage);
        Assert.Equal(10, result.Pagination.ResultsPerPage);
    }

    [Fact]
    public async Task GetCardsAsync_WhenPagedPayloadMissing_ThrowsBusinessMapApiException()
    {
        using var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "pagination": null }""");
        var api = new CardsApi(handler.CreateClient());

        await Assert.ThrowsAsync<BusinessMapApiException>(() => api.GetCardsAsync());
    }

    [Fact]
    public async Task CreateCardAsync_SendsSerializedBodyAndUnwrapsResult()
    {
        using var handler = StubHttpMessageHandler.Json(HttpStatusCode.Created, """{ "data": { "card_id": 99 } }""");
        var api = new CardsApi(handler.CreateClient());

        var created = await api.CreateCardAsync(new CreateCardRequest { ColumnId = 11, Title = "New" });

        Assert.Equal(99, created.CardId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.NotNull(handler.LastRequestBody);
        // snake_case serialization and null omission.
        Assert.Contains("\"column_id\":11", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("\"title\":\"New\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.DoesNotContain("lane_id", handler.LastRequestBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateCardAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var handler = StubHttpMessageHandler.Status(HttpStatusCode.OK);
        var api = new CardsApi(handler.CreateClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => api.CreateCardAsync(null!));
    }

    [Fact]
    public async Task DeleteCardAsync_SendsDeleteAndIgnoresBody()
    {
        using var handler = StubHttpMessageHandler.Status(HttpStatusCode.NoContent);
        var api = new CardsApi(handler.CreateClient());

        await api.DeleteCardAsync(5);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Equal("https://test.kanbanize.com/api/v2/cards/5", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetCardAsync_OnErrorStatus_ThrowsBusinessMapApiException()
    {
        using var handler = StubHttpMessageHandler.Json(HttpStatusCode.NotFound, """{ "error": "not found" }""");
        var api = new CardsApi(handler.CreateClient());

        var exception = await Assert.ThrowsAsync<BusinessMapApiException>(() => api.GetCardAsync(123));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal("GET", exception.RequestMethod);
        Assert.Contains("cards/123", exception.RequestUri!, StringComparison.Ordinal);
        Assert.Contains("not found", exception.ResponseBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetCardAsync_WhenDataMissing_ThrowsBusinessMapApiException()
    {
        using var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "pagination": null }""");
        var api = new CardsApi(handler.CreateClient());

        await Assert.ThrowsAsync<BusinessMapApiException>(() => api.GetCardAsync(1));
    }
}
