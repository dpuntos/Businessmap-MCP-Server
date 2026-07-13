namespace BusinessMapNET.MCPServer.Services;

/// <summary>
/// Represents an error that should be surfaced to the calling LLM/agent as a clear,
/// actionable message (for example, an ambiguous board name or a missing destination column).
/// Messages are intended to be safe to show to the user.
/// </summary>
public sealed class ToolException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolException"/> class.
    /// </summary>
    /// <param name="message">A clear, user-facing description of what went wrong and how to fix it.</param>
    public ToolException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolException"/> class.
    /// </summary>
    /// <param name="message">A clear, user-facing description of what went wrong and how to fix it.</param>
    /// <param name="innerException">The underlying exception.</param>
    public ToolException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
