namespace BusinessMapNET.Core.Configuration;

/// <summary>
/// Strongly-typed settings used to connect to the Businessmap (Kanbanize) API v2.
/// These values are typically bound from an <c>appsettings.json</c> "BusinessMap" section.
/// </summary>
public sealed class BusinessMapSettings
{
    /// <summary>
    /// The configuration section name that holds these settings.
    /// </summary>
    public const string SectionName = "BusinessMap";

    /// <summary>
    /// The company / account name. This is the sub-domain used to build the API base URL,
    /// e.g. for <c>https://contoso.kanbanize.com</c> the value is <c>contoso</c>.
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// The API key sent in the <c>apikey</c> header on every request.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The full base URL for the API v2 endpoints, built from <see cref="CompanyName"/>.
    /// </summary>
    public string BaseUrl => $"https://{CompanyName}.kanbanize.com/api/v2/";

    /// <summary>
    /// Validates that the required settings have been provided.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a required value is missing.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            throw new InvalidOperationException(
                $"'{nameof(CompanyName)}' is required. Set the '{SectionName}:{nameof(CompanyName)}' configuration value.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException(
                $"'{nameof(ApiKey)}' is required. Set the '{SectionName}:{nameof(ApiKey)}' configuration value.");
        }
    }
}
