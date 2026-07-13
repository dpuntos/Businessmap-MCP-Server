namespace BusinessMapNET.Application;

/// <summary>
/// Represents a business-level error that should be surfaced to the caller as a clear,
/// actionable message (for example, an ambiguous board name, a missing destination column
/// or a translated Businessmap API failure). Messages are intended to be safe to show to the user.
/// </summary>
/// <remarks>
/// The Application layer is transport-agnostic: it never depends on the MCP protocol. Hosts
/// (such as the MCP server) are expected to catch this exception and translate it into whatever
/// error shape their transport requires.
/// </remarks>
public class BusinessMapServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessMapServiceException"/> class.
    /// </summary>
    /// <param name="message">A clear, user-facing description of what went wrong and how to fix it.</param>
    public BusinessMapServiceException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessMapServiceException"/> class.
    /// </summary>
    /// <param name="message">A clear, user-facing description of what went wrong and how to fix it.</param>
    /// <param name="innerException">The underlying exception.</param>
    public BusinessMapServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
