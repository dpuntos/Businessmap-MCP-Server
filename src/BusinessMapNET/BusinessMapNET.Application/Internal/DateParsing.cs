using System.Globalization;

namespace BusinessMapNET.Application.Internal;

/// <summary>
/// Helpers to parse and validate ISO 8601 date/time values coming from callers or the API.
/// </summary>
internal static class DateParsing
{
    /// <summary>
    /// Parses an ISO 8601 date string into a <see cref="DateTimeOffset"/>, returning
    /// <see langword="null"/> when parsing fails or the input is empty.
    /// </summary>
    public static DateTimeOffset? Parse(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;
}
