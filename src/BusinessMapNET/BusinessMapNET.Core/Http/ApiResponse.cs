using System.Text.Json.Serialization;

namespace BusinessMapNET.Core.Http;

/// <summary>
/// Represents the standard Businessmap response envelope where the payload is wrapped in a <c>data</c> property.
/// </summary>
/// <typeparam name="T">The type of the wrapped payload.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// The actual payload returned by the API.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; init; }
}
