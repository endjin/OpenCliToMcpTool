# OpenCliToMcp.Core

Core library for creating MCP (Model Context Protocol) servers from OpenCLI tool definitions.

## Overview

OpenCliToMcp.Core provides the foundational components needed to execute CLI tools and convert their outputs into MCP-compatible responses. It includes:

- **CLI Executors**: Base classes and interfaces for executing command-line tools
- **Process Management**: Robust process execution with timeout and error handling
- **Response Formatting**: Flexible response formatting (JSON, plain text, structured data)
- **Configuration**: Options pattern for configuring CLI execution behavior

## Installation

```bash
dotnet add package OpenCliToMcp.Core
```

## Basic Usage

```csharp
using OpenCliToMcp.Core;

// Create a simple CLI executor
var executor = new SimplifiedCliExecutor(new SimplifiedCliExecutorOptions
{
    ExecutablePath = "git",
    ResponseFormat = ResponseFormat.Json
});

// Execute a command
var response = await executor.ExecuteAsync(new[] { "status", "--porcelain" });
Console.WriteLine(response.Content);
```

## Key Components

### ICliExecutor
The main interface for executing CLI commands:
```csharp
public interface ICliExecutor
{
    Task<CliResponse> ExecuteAsync(string[] arguments, CancellationToken cancellationToken = default);
}
```

### CliExecutorBase
Abstract base class providing common CLI execution functionality with built-in logging and error handling.

### ConfigurableCliExecutor
Advanced executor with configurable executable resolution, custom process options, and response formatting.

### Response Formats
- **Json**: Structured JSON output
- **PlainText**: Raw text output
- **Auto**: Automatic format detection

## Advanced Features

### Custom Process Execution
```csharp
var executor = new ConfigurableCliExecutor(
    executableResolver,
    processExecutor,
    responseFormatter,
    logger);
```

### Executable Resolution
Automatically find executables using glob patterns:
```csharp
var resolver = new GlobbingExecutableResolver(new GlobbingExecutableResolverOptions
{
    SearchPattern = "**/bin/my-tool.exe"
});
```

## License

Licensed under the Apache License, Version 2.0. See [LICENSE](https://github.com/OpenCliToMcp/OpenCliToMcpTool/blob/main/LICENSE) for details.