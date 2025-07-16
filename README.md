# OpenCliToMcp Framework

A comprehensive framework for automatically generating Model Context Protocol (MCP) servers from [OpenCLI specifications](https://opencli.org/), enabling seamless integration of command-line tools with MCP-compatible clients.

## 🚀 Overview

OpenCliToMcp.Generator is a .NET Standard 2.0 Source Generator that transforms OpenCLI-compliant command-line applications into MCP servers, using the [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk), allowing them to be used as tools within LLM conversations. 

## 📁 Repository Structure

```
OpenCliToMcp/
├── Solutions/
│   ├── OpenCliToMcp.Core/              # Core framework components
│   ├── OpenCliToMcp.Generator/         # Source generator for MCP servers
│   ├── TaskManager.Cli/                # Demo CLI application
│   ├── TaskManager.McpServer/          # MCP server (STDIO transport)
│   ├── Weather.Cli/                    # Demo CLI application
│   ├── Weather.McpServer/              # MCP server (HTTP transport)
│   └── *.Tests/                        # Comprehensive test suites
├── knowledge-base/                     # Documentation and specifications
└── README.md                           # This file
```

### Core Components

- **OpenCliToMcp.Core**: Foundation classes for CLI execution and MCP integration
- **OpenCliToMcp.Generator**: Source generator that creates MCP server implementations
- **Demo Applications**: TaskManager and Weather CLIs with full OpenCLI specifications
- **MCP Servers**: Generated servers demonstrating both transport types

## 🛠️ Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code with C# Dev Kit
- Claude Desktop or VS Code with MCP extension

### Building the Project

```bash
cd Solutions
dotnet build
```

### Running the Demo Servers

#### TaskManager MCP Server (STDIO)
```bash
cd TaskManager.McpServer
dotnet run
```

#### Weather MCP Server (HTTP)
```bash
cd Weather.McpServer
dotnet run
```

## 🔧 MCP Client Configuration

### VS Code Configuration

Create or update your MCP configuration file for use in VS Code:

**Windows**: `%APPDATA%\Code\User\mcp.json`
**macOS**: `~/Library/Application Support/Code/User/mcp.json`
**Linux**: `~/.config/Code/User/mcp.json`

```json
{
    "servers": {
        "weather-cli-app": {
            "url": "http://localhost:5000/",
            "type": "http"
        },
        "task-manager-cli-app": {
            "type": "stdio",
            "command": "C:\\path\\to\\Solutions\\TaskManager.McpServer\\bin\\Debug\\net9.0\\TaskManager.McpServer.exe",
            "args": []
        }
    },
    "inputs": []
}
```

### Cross-Platform Path Configuration

#### Windows
```json
{
    "command": "C:\\path\\to\\project\\TaskManager.McpServer\\bin\\Debug\\net9.0\\TaskManager.McpServer.exe"
}
```

#### macOS/Linux
```json
{
    "command": "/path/to/project/TaskManager.McpServer/bin/Debug/net9.0/TaskManager.McpServer"
}
```

## 🎯 Available Tools

### TaskManager Tools

- **Task Management**: Create, update, delete, and show tasks
- **Project Management**: Create, list, and archive projects
- **Statistics**: View task statistics with filtering options
- **Export**: Export tasks to JSON, CSV, or Markdown formats
- **List & Filter**: Advanced filtering by status, priority, assignee, project

#### Example Usage
```
Create a high-priority task called "Review pull request" assigned to john
```

### Weather Tools

- **Current Weather**: Get current weather conditions for any city
- **Forecast**: Multi-day weather forecasts with customizable periods
- **Compare**: Compare weather between multiple cities
- **Location Management**: Save and manage favorite locations
- **Unit Conversion**: Support for Celsius, Fahrenheit, and Kelvin

#### Example Usage
```
What's the weather like in London compared to Paris?
```

## 🔨 Using the Source Generator

The OpenCliToMcp.Generator provides two approaches for generating MCP servers from OpenCLI specifications:

### Approach 1: OpenCliTool Attribute

Use the `[OpenCliTool]` attribute to generate MCP tool methods in a partial class:

```csharp
using OpenCliToMcp;
using OpenCliToMcp.Core;

namespace Weather.McpServer;

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
```

This approach:
- Creates a partial class with generated MCP tool methods
- Uses dependency injection for the `ICliExecutor`
- Ideal for custom implementations and testing

### Approach 2: AdditionalFiles ItemGroup

Add the OpenCLI specification as an `AdditionalFiles` item in your project file:

```xml
<ItemGroup>
    <AdditionalFiles Include="..\TaskManager.Cli\taskmanager.opencli.json" 
                     Link="taskmanager.opencli.json" />
</ItemGroup>
```

This approach:
- Automatically generates the complete MCP server implementation
- No manual coding required
- Ideal for quick MCP server creation from existing CLIs

### Project Configuration

Both approaches require adding the source generator to your project:

```xml
<ItemGroup>
    <ProjectReference Include="..\OpenCliToMcp.Generator\OpenCliToMcp.Generator.csproj" 
                      OutputItemType="Analyzer" 
                      ReferenceOutputAssembly="false" />
    <ProjectReference Include="..\OpenCliToMcp.Core\OpenCliToMcp.Core.csproj" />
</ItemGroup>
```

## 🏗️ Architecture

### Code Generation Process

1. **OpenCLI Specification**: Define CLI commands in `*.opencli.json`
2. **Source Generator**: Automatically generates MCP server implementations
3. **Runtime Integration**: Seamless integration with MCP clients

### Transport Types

#### HTTP Transport (Weather.McpServer)
- Runs as web service on localhost:5000
- Suitable for development and local testing
- Automatic service discovery

#### STDIO Transport (TaskManager.McpServer)
- Direct process communication
- Lower overhead for frequent operations
- Ideal for production deployments

## 📝 CLI Specifications

### TaskManager Commands

The TaskManager CLI supports comprehensive task and project management:

- **Tasks**: `add`, `update`, `delete`, `show`, `list`
- **Projects**: `create`, `list`, `archive`
- **Statistics**: Detailed reporting with filtering
- **Export**: Multiple format support (JSON, CSV, Markdown)

### Weather Commands

The Weather CLI provides comprehensive weather information:

- **Current**: Real-time weather data
- **Forecast**: Multi-day predictions (1-14 days)
- **Compare**: Side-by-side city comparisons
- **Locations**: Favorite location management

## 📄 License

This project is licensed under the APACHE License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Model Context Protocol specification
- OpenCLI specification contributors

---

*Built entirely from prompts with Claude Code using Opus 4 & Sonnet 4 models*