using System.Text.Json;
using System.Text.Json.Serialization;

namespace BusinessMapNET.Core.Http;

/// <summary>
/// Provides the shared <see cref="JsonSerializerOptions"/> used to communicate with the Businessmap API.
/// The API uses <c>snake_case</c> property names.
/// </summary>
internal static class BusinessMapJson
{
    /// <summary>
    /// The default serializer options: snake_case naming, case-insensitive reading and null omission on write.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };
}
