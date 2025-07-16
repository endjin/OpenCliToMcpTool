# OpenCLI to MCP Tool Conversion Specification

## Overview

This specification documents how to automatically generate Model Context Protocol (MCP) tools from OpenCLI specifications using the ModelContextProtocol.AspNetCore NuGet package in .NET. It provides a bridge between CLI tool definitions and AI-accessible tools.

## Model Context Protocol (MCP) Background

MCP is an open protocol that standardizes how applications provide context to Large Language Models (LLMs). It enables:
- Universal connectivity between AI applications and tools
- Standardized tool discovery and invocation
- Type-safe parameter passing
- Consistent error handling

## ModelContextProtocol.AspNetCore Attributes

### 1. McpServerToolType Attribute

Marks a class as containing MCP tools:

```csharp
[McpServerToolType]
public static class GeneratedCliTools
{
    // Generated tool methods
}
```

**Purpose**: Enables automatic tool discovery via `WithToolsFromAssembly()`

### 2. McpServerTool Attribute

Marks methods as callable MCP tools:

```csharp
[McpServerTool]
public static async Task<string> ExecuteCliCommand(...)
```

**Purpose**: Identifies methods that can be invoked by LLMs

### 3. Description Attribute

Provides metadata for tools and parameters:

```csharp
using System.ComponentModel;

[McpServerTool, Description("Executes a CLI command with specified options")]
public static async Task<string> GitCommit(
    [Description("Commit message")] string message,
    [Description("Include all modified files")] bool all = false)
```

**Purpose**: Helps LLMs understand when and how to use tools

## OpenCLI to MCP Mapping Strategy

### Core Mapping Concepts

1. **OpenCLI Command → MCP Tool Method**
   - Each command becomes a separate tool method
   - Nested commands use hierarchical naming

2. **OpenCLI Arguments → MCP Method Parameters**
   - Positional arguments become required parameters
   - Order preserved based on `ordinal` property

3. **OpenCLI Options → MCP Optional Parameters**
   - Options become optional parameters with defaults
   - Boolean flags for simple options
   - Typed parameters for options with arguments

4. **OpenCLI Descriptions → MCP Description Attributes**
   - Direct mapping of description text
   - Used for both tools and parameters

## Implementation Architecture

### 1. Parser Component

```csharp
public class OpenCliSpecificationParser
{
    public OpenCliSpec Parse(string jsonOrYaml)
    {
        // Deserialize OpenCLI specification
        // Validate against schema
        // Return strongly-typed model
    }
}
```

### 2. Code Generator Component

```csharp
public class McpToolGenerator
{
    public string GenerateToolClass(OpenCliSpec spec)
    {
        var sb = new StringBuilder();
        
        // Generate class with McpServerToolType
        sb.AppendLine("[McpServerToolType]");
        sb.AppendLine($"public static class {spec.Info.Title}Tools");
        sb.AppendLine("{");
        
        // Generate tool methods for each command
        foreach (var command in spec.Commands ?? new())
        {
            GenerateToolMethod(sb, command, spec.Info.Title);
        }
        
        sb.AppendLine("}");
        return sb.ToString();
    }
}
```

### 3. Tool Method Generation

```csharp
private void GenerateToolMethod(StringBuilder sb, Command command, string toolPrefix)
{
    // Method signature
    var methodName = $"{toolPrefix}_{command.Name}".Replace("-", "_");
    
    sb.AppendLine($"    [McpServerTool, Description(\"{command.Description ?? command.Name}\")]");
    sb.AppendLine($"    public static async Task<string> {methodName}(");
    
    // Add CLI executor service
    sb.AppendLine("        ICliExecutor cliExecutor,");
    
    // Generate parameters from arguments
    foreach (var arg in command.Arguments ?? new())
    {
        var paramType = DetermineParameterType(arg);
        var paramName = ToCamelCase(arg.Name);
        
        sb.AppendLine($"        [Description(\"{arg.Description ?? arg.Name}\")] {paramType} {paramName},");
    }
    
    // Generate parameters from options
    foreach (var option in command.Options ?? new())
    {
        GenerateOptionParameter(sb, option);
    }
    
    // Add cancellation token
    sb.AppendLine("        CancellationToken cancellationToken = default)");
    sb.AppendLine("    {");
    
    // Generate method body
    GenerateMethodBody(sb, command);
    
    sb.AppendLine("    }");
}
```

## Conversion Examples

### Example 1: Simple Command

**OpenCLI Specification:**
```yaml
opencli: "0.1"
info:
  title: "git"
commands:
  - name: "status"
    description: "Show the working tree status"
    options:
      - name: "short"
        aliases: ["s"]
        description: "Give output in short format"
```

**Generated MCP Tool:**
```csharp
[McpServerToolType]
public static class GitTools
{
    [McpServerTool, Description("Show the working tree status")]
    public static async Task<string> Git_status(
        ICliExecutor cliExecutor,
        [Description("Give output in short format")] bool shortFormat = false,
        CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "status" };
        
        if (shortFormat)
            args.Add("--short");
            
        return await cliExecutor.ExecuteAsync("git", args, cancellationToken);
    }
}
```

### Example 2: Command with Arguments

**OpenCLI Specification:**
```yaml
commands:
  - name: "checkout"
    description: "Switch branches"
    arguments:
      - name: "branch"
        description: "Branch name to checkout"
        required: true
        ordinal: 0
    options:
      - name: "create"
        aliases: ["b"]
        description: "Create new branch"
```

**Generated MCP Tool:**
```csharp
[McpServerTool, Description("Switch branches")]
public static async Task<string> Git_checkout(
    ICliExecutor cliExecutor,
    [Description("Branch name to checkout")] string branch,
    [Description("Create new branch")] bool create = false,
    CancellationToken cancellationToken = default)
{
    var args = new List<string> { "checkout" };
    
    if (create)
        args.Add("-b");
        
    args.Add(branch);
    
    return await cliExecutor.ExecuteAsync("git", args, cancellationToken);
}
```

### Example 3: Options with Arguments

**OpenCLI Specification:**
```yaml
commands:
  - name: "commit"
    description: "Record changes to repository"
    options:
      - name: "message"
        aliases: ["m"]
        description: "Commit message"
        arguments:
          - name: "text"
            required: true
      - name: "all"
        aliases: ["a"]
        description: "Commit all modified files"
```

**Generated MCP Tool:**
```csharp
[McpServerTool, Description("Record changes to repository")]
public static async Task<string> Git_commit(
    ICliExecutor cliExecutor,
    [Description("Commit message")] string? message = null,
    [Description("Commit all modified files")] bool all = false,
    CancellationToken cancellationToken = default)
{
    var args = new List<string> { "commit" };
    
    if (!string.IsNullOrEmpty(message))
    {
        args.Add("-m");
        args.Add(message);
    }
    
    if (all)
        args.Add("--all");
    
    return await cliExecutor.ExecuteAsync("git", args, cancellationToken);
}
```

## Advanced Features

### 1. Nested Commands

OpenCLI supports nested commands, which map to hierarchical tool names:

```csharp
// OpenCLI: git remote add <name> <url>
[McpServerTool, Description("Add a remote repository")]
public static async Task<string> Git_remote_add(
    ICliExecutor cliExecutor,
    [Description("Remote name")] string name,
    [Description("Remote URL")] string url,
    CancellationToken cancellationToken = default)
```

### 2. Parameter Type Mapping

```csharp
private string DetermineParameterType(Argument arg)
{
    // Map OpenCLI arity to C# types
    return arg.Arity switch
    {
        "zero-or-one" => "string?",
        "zero-or-more" => "string[]",
        "one-or-more" => "string[]",
        _ => arg.Required ? "string" : "string?"
    };
}
```

### 3. Exit Code Handling

```csharp
public class CliExecutionResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; }
    public string Error { get; set; }
    
    public string ToJson() => JsonSerializer.Serialize(this);
}
```

### 4. Validation Support

```csharp
private void ValidateArgument(Argument arg, string value)
{
    if (arg.AcceptedValues?.Any() == true)
    {
        if (!arg.AcceptedValues.Contains(value))
        {
            throw new ArgumentException(
                $"Invalid value '{value}'. Accepted values: {string.Join(", ", arg.AcceptedValues)}"
            );
        }
    }
}
```

## Service Registration

### Basic Setup

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Register CLI executor
builder.Services.AddSingleton<ICliExecutor, CliExecutor>();

// Optionally register specific CLI tool generators
builder.Services.AddSingleton<GitToolsGenerator>();

await builder.Build().RunAsync();
```

### CLI Executor Implementation

```csharp
public interface ICliExecutor
{
    Task<string> ExecuteAsync(string command, IEnumerable<string> args, CancellationToken cancellationToken);
}

public class CliExecutor : ICliExecutor
{
    public async Task<string> ExecuteAsync(string command, IEnumerable<string> args, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = string.Join(" ", args.Select(EscapeArgument)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        process.Start();
        
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        
        await process.WaitForExitAsync(cancellationToken);
        
        if (process.ExitCode != 0)
        {
            return JsonSerializer.Serialize(new 
            {
                success = false,
                exitCode = process.ExitCode,
                output,
                error
            });
        }
        
        return output;
    }
    
    private string EscapeArgument(string arg)
    {
        // Proper shell escaping logic
        return arg.Contains(' ') ? $"\"{arg}\"" : arg;
    }
}
```

## Code Generation Tool

### Complete Generator Implementation

```csharp
public class OpenCliToMcpConverter
{
    private readonly ILogger<OpenCliToMcpConverter> _logger;
    
    public async Task<string> ConvertSpecificationAsync(string openCliSpec)
    {
        // 1. Parse OpenCLI specification
        var spec = ParseOpenCliSpec(openCliSpec);
        
        // 2. Generate C# code
        var code = GenerateMcpToolsClass(spec);
        
        // 3. Format code
        var formattedCode = await FormatCodeAsync(code);
        
        return formattedCode;
    }
    
    private string GenerateMcpToolsClass(OpenCliSpec spec)
    {
        var template = @"
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace GeneratedTools
{
    [McpServerToolType]
    public static class {{ClassName}}
    {
{{Methods}}
    }
}";
        
        var className = $"{SanitizeName(spec.Info.Title)}Tools";
        var methods = GenerateAllMethods(spec);
        
        return template
            .Replace("{{ClassName}}", className)
            .Replace("{{Methods}}", methods);
    }
}
```

## Best Practices

### 1. Tool Naming
- Use clear, descriptive names
- Maintain consistency with original CLI
- Handle special characters appropriately

### 2. Error Handling
- Return structured error information
- Include exit codes in responses
- Provide helpful error messages

### 3. Parameter Validation
- Validate required parameters
- Check accepted values
- Provide clear validation messages

### 4. Documentation
- Map all descriptions from OpenCLI
- Add implementation notes where needed
- Include usage examples

### 5. Security
- Sanitize all inputs
- Validate command execution permissions
- Log tool invocations for auditing

## Future Enhancements

### 1. Advanced Type Support
- Custom type converters for complex arguments
- File path validation and handling
- Environment variable expansion

### 2. Interactive Commands
- Support for commands requiring user input
- Progress reporting for long-running operations
- Streaming output support

### 3. Caching and Optimization
- Cache frequently used command outputs
- Batch similar operations
- Connection pooling for remote CLIs

### 4. Enhanced Error Recovery
- Retry logic for transient failures
- Fallback strategies
- Detailed error diagnostics

## Conclusion

This specification provides a comprehensive approach to automatically converting OpenCLI specifications into MCP tools using ModelContextProtocol.AspNetCore. By following these patterns, developers can quickly expose CLI tools to AI assistants while maintaining type safety, proper error handling, and clear documentation.