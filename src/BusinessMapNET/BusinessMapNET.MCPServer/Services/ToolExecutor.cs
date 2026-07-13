using BusinessMapNET.Application;

namespace BusinessMapNET.MCPServer.Services;

/// <summary>
/// Bridges the transport-agnostic <see cref="BusinessMapServiceException"/> thrown by the business
/// services into the MCP-facing <see cref="ToolException"/>, so the business layer never needs to
/// know about the MCP protocol.
/// </summary>
internal static class ToolExecutor
{
    public static async Task<T> RunAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (BusinessMapServiceException ex)
        {
            throw new ToolException(ex.Message, ex);
        }
    }
}
