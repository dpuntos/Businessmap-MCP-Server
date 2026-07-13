using System.Globalization;
using System.Text.Json;
using BusinessMapNET.MCPServer.Dtos;

namespace BusinessMapNET.MCPServer.Services;

/// <summary>
/// Parses the raw <c>currentStructure</c> JSON of a board (as returned by
/// <see cref="BusinessMapNET.Core.Services.IBoardsApi.GetBoardStructureAsync"/>) into strongly-typed
/// workflow data, and exposes convenient lookups by id and by name.
/// </summary>
/// <remarks>
/// The Businessmap API returns columns/lanes/workflows/types keyed by their id (an object map),
/// but this parser also tolerates array-shaped payloads for forward compatibility.
/// </remarks>
public sealed class BoardStructure
{
    private readonly Dictionary<int, ColumnInfo> _columnsById;
    private readonly Dictionary<int, LaneInfo> _lanesById;
    private readonly Dictionary<int, CardTypeInfo> _typesById;

    private BoardStructure(
        int boardId,
        string? boardName,
        IReadOnlyList<WorkflowSummary> workflows,
        IReadOnlyList<ColumnInfo> columns,
        IReadOnlyList<LaneInfo> lanes,
        IReadOnlyList<CardTypeInfo> cardTypes)
    {
        BoardId = boardId;
        BoardName = boardName;
        Workflows = workflows;
        Columns = columns;
        Lanes = lanes;
        CardTypes = cardTypes;

        _columnsById = columns.ToDictionary(c => c.ColumnId);
        _lanesById = lanes.ToDictionary(l => l.LaneId);
        _typesById = cardTypes.ToDictionary(t => t.TypeId);
    }

    /// <summary>The identifier of the board this structure belongs to.</summary>
    public int BoardId { get; }

    /// <summary>The name of the board, if known.</summary>
    public string? BoardName { get; }

    /// <summary>The workflows defined on the board.</summary>
    public IReadOnlyList<WorkflowSummary> Workflows { get; }

    /// <summary>The columns defined on the board.</summary>
    public IReadOnlyList<ColumnInfo> Columns { get; }

    /// <summary>The lanes defined on the board.</summary>
    public IReadOnlyList<LaneInfo> Lanes { get; }

    /// <summary>The card types available on the board.</summary>
    public IReadOnlyList<CardTypeInfo> CardTypes { get; }

    /// <summary>Gets the display name of a column by id, or <see langword="null"/> when unknown.</summary>
    public string? GetColumnName(int? columnId) =>
        columnId is int id && _columnsById.TryGetValue(id, out var column) ? column.Name : null;

    /// <summary>Gets the display name of a lane by id, or <see langword="null"/> when unknown.</summary>
    public string? GetLaneName(int? laneId) =>
        laneId is int id && _lanesById.TryGetValue(id, out var lane) ? lane.Name : null;

    /// <summary>Determines whether a column with the given id exists on the board.</summary>
    public bool ColumnExists(int columnId) => _columnsById.ContainsKey(columnId);

    /// <summary>Determines whether a lane with the given id exists on the board.</summary>
    public bool LaneExists(int laneId) => _lanesById.ContainsKey(laneId);

    /// <summary>Determines whether a card type with the given id exists on the board.</summary>
    public bool CardTypeExists(int typeId) => _typesById.ContainsKey(typeId);

    /// <summary>
    /// Finds columns whose name matches <paramref name="name"/> (case-insensitive).
    /// Returns all matches so callers can detect ambiguity.
    /// </summary>
    public IReadOnlyList<ColumnInfo> FindColumnsByName(string name) =>
        Columns.Where(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// Finds lanes whose name matches <paramref name="name"/> (case-insensitive).
    /// Returns all matches so callers can detect ambiguity.
    /// </summary>
    public IReadOnlyList<LaneInfo> FindLanesByName(string name) =>
        Lanes.Where(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// Finds card types whose name matches <paramref name="name"/> (case-insensitive).
    /// Returns all matches so callers can detect ambiguity.
    /// </summary>
    public IReadOnlyList<CardTypeInfo> FindCardTypesByName(string name) =>
        CardTypes.Where(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Projects this structure into a serializable <see cref="WorkflowInfo"/> DTO.</summary>
    public WorkflowInfo ToWorkflowInfo() =>
        new(BoardId, BoardName, Workflows, Columns, Lanes, CardTypes);

    /// <summary>
    /// Parses a board <c>currentStructure</c> payload.
    /// </summary>
    /// <param name="root">The raw structure JSON (the unwrapped <c>data</c> element).</param>
    /// <param name="boardId">The identifier of the board.</param>
    /// <param name="boardName">The name of the board, when known.</param>
    public static BoardStructure Parse(JsonElement root, int boardId, string? boardName)
    {
        var workflows = ParseSection(root, "workflows", ParseWorkflow);
        var columns = ParseSection(root, "columns", ParseColumn);
        var lanes = ParseSection(root, "lanes", ParseLane);

        var types = ParseSection(root, "card_types", ParseCardType);
        if (types.Count == 0)
        {
            types = ParseSection(root, "types", ParseCardType);
        }

        return new BoardStructure(boardId, boardName, workflows, columns, lanes, types);
    }

    private static List<T> ParseSection<T>(
        JsonElement root,
        string propertyName,
        Func<string?, JsonElement, T?> factory)
        where T : class
    {
        var results = new List<T>();

        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty(propertyName, out var section))
        {
            return results;
        }

        switch (section.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in section.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Object &&
                        factory(property.Name, property.Value) is { } item)
                    {
                        results.Add(item);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var element in section.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.Object &&
                        factory(null, element) is { } item)
                    {
                        results.Add(item);
                    }
                }
                break;
        }

        return results;
    }

    private static WorkflowSummary? ParseWorkflow(string? key, JsonElement element)
    {
        var id = ReadId(key, element, "workflow_id");
        if (id is null)
        {
            return null;
        }

        return new WorkflowSummary(
            id.Value,
            ReadString(element, "name"),
            ReadInt(element, "type"),
            ReadInt(element, "position"));
    }

    private static ColumnInfo? ParseColumn(string? key, JsonElement element)
    {
        var id = ReadId(key, element, "column_id");
        if (id is null)
        {
            return null;
        }

        return new ColumnInfo(
            id.Value,
            ReadString(element, "name"),
            ReadInt(element, "workflow_id"),
            ReadInt(element, "parent_column_id"),
            ReadInt(element, "position"),
            ReadInt(element, "section"),
            ReadString(element, "type"));
    }

    private static LaneInfo? ParseLane(string? key, JsonElement element)
    {
        var id = ReadId(key, element, "lane_id");
        if (id is null)
        {
            return null;
        }

        return new LaneInfo(
            id.Value,
            ReadString(element, "name"),
            ReadInt(element, "workflow_id"),
            ReadInt(element, "position"),
            ReadString(element, "color"));
    }

    private static CardTypeInfo? ParseCardType(string? key, JsonElement element)
    {
        var id = ReadId(key, element, "type_id");
        if (id is null)
        {
            return null;
        }

        return new CardTypeInfo(
            id.Value,
            ReadString(element, "name"),
            ReadString(element, "color"));
    }

    private static int? ReadId(string? key, JsonElement element, string idPropertyName)
    {
        if (ReadInt(element, idPropertyName) is int explicitId)
        {
            return explicitId;
        }

        return int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            _ => null
        };
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(
                value.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsed) => parsed,
            _ => null
        };
    }
}
