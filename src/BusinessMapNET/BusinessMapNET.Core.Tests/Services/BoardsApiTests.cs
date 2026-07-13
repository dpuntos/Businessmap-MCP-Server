using System.Net;
using BusinessMapNET.Core.Services;
using BusinessMapNET.Core.Tests.TestHelpers;
using Xunit;

namespace BusinessMapNET.Core.Tests.Services;

public sealed class BoardsApiTests
{
    [Fact]
    public async Task GetBoardsAsync_UnwrapsDataArray()
    {
        const string json = """
        { "data": [ { "board_id": 1, "name": "Alpha" }, { "board_id": 2, "name": "Beta" } ] }
        """;
        using var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, json);
        var api = new BoardsApi(handler.CreateClient());

        var boards = await api.GetBoardsAsync();

        Assert.Equal(2, boards.Count);
        Assert.Equal("Alpha", boards[0].Name);
        Assert.Equal(2, boards[1].BoardId);
        Assert.Equal("https://test.kanbanize.com/api/v2/boards", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetBoardStructureAsync_RequestsCurrentStructureEndpoint()
    {
        using var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "data": { "columns": {} } }""");
        var api = new BoardsApi(handler.CreateClient());

        var structure = await api.GetBoardStructureAsync(15);

        Assert.Equal(
            "https://test.kanbanize.com/api/v2/boards/15/currentStructure",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.True(structure.TryGetProperty("columns", out _));
    }

    [Fact]
    public async Task UpdateBoardAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var handler = StubHttpMessageHandler.Status(HttpStatusCode.OK);
        var api = new BoardsApi(handler.CreateClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => api.UpdateBoardAsync(1, null!));
    }
}
