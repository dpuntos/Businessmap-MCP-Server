using System.Text.Json;
using BusinessMapNET.Core.Http;
using Microsoft.Extensions.Logging;

namespace BusinessMapNET.Application.Internal;

/// <summary>
/// Executes Businessmap API calls, translating <see cref="BusinessMapApiException"/> into a clear
/// <see cref="BusinessMapServiceException"/> that describes the failing action.
/// </summary>
internal static class ApiExecutor
{
    public static async Task<T> ExecuteAsync<T>(
        ILogger logger,
        Func<Task<T>> action,
        string description)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (BusinessMapApiException ex)
        {
            logger.LogError(
                ex,
                "Businessmap API call failed while trying to {Description}. Status: {StatusCode}.",
                description,
                (int)ex.StatusCode);

            throw new BusinessMapServiceException(DescribeApiError(ex, description), ex);
        }
    }

    public static async Task ExecuteAsync(
        ILogger logger,
        Func<Task> action,
        string description)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            await action().ConfigureAwait(false);
        }
        catch (BusinessMapApiException ex)
        {
            logger.LogError(
                ex,
                "Businessmap API call failed while trying to {Description}. Status: {StatusCode}.",
                description,
                (int)ex.StatusCode);

            throw new BusinessMapServiceException(DescribeApiError(ex, description), ex);
        }
    }

    private static string DescribeApiError(BusinessMapApiException ex, string description)
    {
        var code = (int)ex.StatusCode;
        var reason = code switch
        {
            401 or 403 => "the API key is missing or lacks permission for this operation",
            404 => "the requested resource was not found",
            422 => "the request was rejected as invalid (check the provided ids and values)",
            429 => "the API rate limit was exceeded; please retry shortly",
            >= 500 => "the Businessmap service reported a server error; please retry shortly",
            _ => "the Businessmap API returned an error"
        };

        var detail = ExtractApiMessage(ex.ResponseBody);
        var suffix = detail is null ? string.Empty : $" Details: {detail}";
        return $"Could not {description} because {reason} (HTTP {code}).{suffix}";
    }

    private static string? ExtractApiMessage(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString();
                }

                if (error.ValueKind == JsonValueKind.Object &&
                    error.TryGetProperty("message", out var nestedMessage) &&
                    nestedMessage.ValueKind == JsonValueKind.String)
                {
                    return nestedMessage.GetString();
                }
            }

            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            // The body was not JSON; fall through and return a trimmed snippet.
        }

        var trimmed = responseBody.Trim();
        return trimmed.Length > 300 ? trimmed[..300] + "…" : trimmed;
    }
}
