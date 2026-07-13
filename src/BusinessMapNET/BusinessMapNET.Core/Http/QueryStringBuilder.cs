using System.Globalization;
using System.Text;

namespace BusinessMapNET.Core.Http;

/// <summary>
/// Helper for building URL-encoded query strings from simple key/value pairs and collections.
/// Businessmap array parameters are serialized as comma-separated values.
/// </summary>
internal sealed class QueryStringBuilder
{
    private readonly List<KeyValuePair<string, string>> _parameters = [];

    /// <summary>
    /// Adds a scalar value to the query string when it is not <see langword="null"/>.
    /// </summary>
    public QueryStringBuilder Add(string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _parameters.Add(new KeyValuePair<string, string>(name, value));
        }

        return this;
    }

    /// <summary>
    /// Adds a nullable integer value to the query string when it has a value.
    /// </summary>
    public QueryStringBuilder Add(string name, int? value)
    {
        if (value.HasValue)
        {
            _parameters.Add(new KeyValuePair<string, string>(name, value.Value.ToString(CultureInfo.InvariantCulture)));
        }

        return this;
    }

    /// <summary>
    /// Adds a nullable boolean value as <c>1</c>/<c>0</c> when it has a value.
    /// </summary>
    public QueryStringBuilder Add(string name, bool? value)
    {
        if (value.HasValue)
        {
            _parameters.Add(new KeyValuePair<string, string>(name, value.Value ? "1" : "0"));
        }

        return this;
    }

    /// <summary>
    /// Adds a collection of integers as a comma-separated value when it is not null or empty.
    /// </summary>
    public QueryStringBuilder Add(string name, IEnumerable<int>? values)
    {
        if (values is null)
        {
            return this;
        }

        var joined = string.Join(",", values.Select(v => v.ToString(CultureInfo.InvariantCulture)));
        if (joined.Length > 0)
        {
            _parameters.Add(new KeyValuePair<string, string>(name, joined));
        }

        return this;
    }

    /// <summary>
    /// Adds a collection of strings as a comma-separated value when it is not null or empty.
    /// </summary>
    public QueryStringBuilder Add(string name, IEnumerable<string>? values)
    {
        if (values is null)
        {
            return this;
        }

        var joined = string.Join(",", values.Where(v => !string.IsNullOrEmpty(v)));
        if (joined.Length > 0)
        {
            _parameters.Add(new KeyValuePair<string, string>(name, joined));
        }

        return this;
    }

    /// <summary>
    /// Builds the query string, including the leading <c>?</c> when at least one parameter exists.
    /// </summary>
    public string Build()
    {
        if (_parameters.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder("?");
        for (var i = 0; i < _parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(_parameters[i].Key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(_parameters[i].Value));
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    public override string ToString() => Build();
}
