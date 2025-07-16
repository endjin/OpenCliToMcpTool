using OpenCliToMcp;
using OpenCliToMcp.Core;

namespace Weather.McpServer;

/// <summary>
/// Example of using the OpenCliTool attribute to generate MCP tool methods.
/// This creates a partial class with instance methods that use an injected ICliExecutor.
/// </summary>
[OpenCliTool("../Weather.Cli/weather-opencli.json")]
public partial class WeatherCliMcpTool
{
    private readonly ICliExecutor cliExecutor;
    
    public WeatherCliMcpTool(ICliExecutor cliExecutor)
    {
        ArgumentNullException.ThrowIfNull(cliExecutor);
        this.cliExecutor = cliExecutor;
    }
}