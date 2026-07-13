using System.Text.Json.Serialization;

namespace BusinessMapNET.Core.Models;

/// <summary>
/// Represents the currently authenticated Businessmap user (<c>GET /me</c>).
/// </summary>
public sealed class User
{
    /// <summary>The unique identifier of the user.</summary>
    [JsonPropertyName("user_id")]
    public int UserId { get; init; }

    /// <summary>The email address of the user.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>The username of the user.</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>The real (display) name of the user.</summary>
    [JsonPropertyName("realname")]
    public string? RealName { get; init; }

    /// <summary>The avatar identifier of the user.</summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }

    /// <summary>Whether two-factor authentication is enabled (<c>1</c>) or not (<c>0</c>).</summary>
    [JsonPropertyName("is_tfa_enabled")]
    public int IsTfaEnabled { get; init; }

    /// <summary>Whether the user is enabled (<c>1</c>) or not (<c>0</c>).</summary>
    [JsonPropertyName("is_enabled")]
    public int IsEnabled { get; init; }

    /// <summary>Whether the user is confirmed (<c>1</c>) or not (<c>0</c>).</summary>
    [JsonPropertyName("is_confirmed")]
    public int IsConfirmed { get; init; }

    /// <summary>The registration date of the user in <c>YYYY-MM-DD</c> format.</summary>
    [JsonPropertyName("registration_date")]
    public string? RegistrationDate { get; init; }

    /// <summary>The timezone of the user, e.g. <c>Europe/Sofia</c>.</summary>
    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    /// <summary>The selected language of the user, e.g. <c>en</c>.</summary>
    [JsonPropertyName("language")]
    public string? Language { get; init; }
}
