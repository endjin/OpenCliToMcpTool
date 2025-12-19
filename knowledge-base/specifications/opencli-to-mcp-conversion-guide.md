# OpenCLI to MCP Tool Conversion Guide

## Overview

This guide details the process of automatically generating Model Context Protocol (MCP) tools from OpenCLI specifications. It provides patterns and implementation strategies for bridging CLI tool definitions with AI-accessible MCP servers.

## Conversion Architecture

### Mapping Strategy

The conversion follows these core principles:

1. **Command Mapping**: Each OpenCLI command becomes an MCP tool method
2. **Argument Mapping**: Positional arguments become required method parameters
3. **Option Mapping**: CLI options become optional parameters with defaults
4. **Description Mapping**: All descriptions transfer to MCP Description attributes

### Component Overview

```
OpenCLI Spec → Parser → Code Generator → MCP Tool Class
```

## Implementation Components

### 1. OpenCLI Parser

```csharp
public class OpenCliSpecificationParser
{
    public OpenCliSpec Parse(string jsonOrYaml)
    {
        if (IsYaml(jsonOrYaml))
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<OpenCliSpec>(jsonOrYaml);
        }
        
        return JsonSerializer.Deserialize<OpenCliSpec>(jsonOrYaml);
    }
    
    public void Validate(OpenCliSpec spec)
    {
        // Validate against OpenCLI schema
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(spec.OpenCli))
            errors.Add("OpenCLI version is required");
            
        if (spec.Info == null)
            errors.Add("Info section is required");
            
        if (errors.Any())
            throw new ValidationException(string.Join(", ", errors));
    }
}
```

### 2. Code Generation Engine

```csharp
public class McpToolGenerator
{
    private readonly ILogger<McpToolGenerator> _logger;
    
    public McpToolGenerator(ILogger<McpToolGenerator> logger)
    {
        _logger = logger;
    }
    
    public string GenerateToolClass(OpenCliSpec spec)
    {
        var builder = new CodeBuilder();
        
        // Add using statements
        builder.AddUsings(
            "System",
            "System.Collections.Generic",
            "System.ComponentModel",
            "System.Threading",
            "System.Threading.Tasks",
            "ModelContextProtocol.Server"
        );
        
        // Generate namespace and class
        builder.BeginNamespace("GeneratedTools");
        builder.BeginClass($"{SanitizeName(spec.Info.Title)}Tools", 
            attributes: new[] { "McpServerToolType" });
        
        // Generate root command if it has arguments/options
        if (spec.Arguments?.Any() == true || spec.Options?.Any() == true)
        {
            GenerateRootCommand(builder, spec);
        }
        
        // Generate methods for each command
        foreach (var command in spec.Commands ?? Enumerable.Empty<Command>())
        {
            GenerateCommandMethod(builder, command, spec.Info.Title);
        }
        
        builder.EndClass();
        builder.EndNamespace();
        
        return builder.ToString();
    }
    
    private void GenerateCommandMethod(CodeBuilder builder, Command command, string toolPrefix)
    {
        var methodName = GenerateMethodName(toolPrefix, command.Name);
        var description = command.Description ?? $"Execute {command.Name} command";
        
        // Method attributes
        builder.AddMethodAttribute("McpServerTool");
        builder.AddMethodAttribute($"Description(\"{EscapeString(description)}\")");
        
        // Method signature
        builder.BeginMethod(
            "public static async Task<string>",
            methodName,
            GenerateParameters(command)
        );
        
        // Method body
        GenerateMethodBody(builder, command);
        
        builder.EndMethod();
        
        // Recursively generate sub-commands
        foreach (var subCommand in command.Commands ?? Enumerable.Empty<Command>())
        {
            GenerateCommandMethod(builder, subCommand, $"{toolPrefix}_{command.Name}");
        }
    }
}
```

### 3. Parameter Generation

```csharp
public class ParameterGenerator
{
    public List<MethodParameter> GenerateParameters(Command command)
    {
        var parameters = new List<MethodParameter>();
        
        // Always add CLI executor
        parameters.Add(new MethodParameter
        {
            Type = "ICliExecutor",
            Name = "cliExecutor"
        });
        
        // Add arguments (ordered by ordinal)
        var orderedArgs = (command.Arguments ?? Enumerable.Empty<Argument>())
            .OrderBy(a => a.Ordinal ?? 0);
            
        foreach (var arg in orderedArgs)
        {
            parameters.Add(new MethodParameter
            {
                Type = DetermineArgumentType(arg),
                Name = ToCamelCase(arg.Name),
                Description = arg.Description,
                IsRequired = arg.Required ?? false
            });
        }
        
        // Add options
        foreach (var option in command.Options ?? Enumerable.Empty<Option>())
        {
            parameters.Add(GenerateOptionParameter(option));
        }
        
        // Always add cancellation token
        parameters.Add(new MethodParameter
        {
            Type = "CancellationToken",
            Name = "cancellationToken",
            DefaultValue = "default"
        });
        
        return parameters;
    }
    
    private string DetermineArgumentType(Argument arg)
    {
        return arg.Arity switch
        {
            "zero-or-one" => "string?",
            "zero-or-more" => "string[]",
            "one-or-more" => "string[]",
            "exactly-one" => arg.Required ?? true ? "string" : "string?",
            _ => "string"
        };
    }
    
    private MethodParameter GenerateOptionParameter(Option option)
    {
        var hasArgument = option.Arguments?.Any() == true;
        
        if (hasArgument)
        {
            var optionArg = option.Arguments!.First();
            return new MethodParameter
            {
                Type = DetermineArgumentType(optionArg) + "?",
                Name = ToCamelCase(option.Name),
                Description = option.Description,
                DefaultValue = "null"
            };
        }
        
        // Boolean flag
        return new MethodParameter
        {
            Type = "bool",
            Name = ToCamelCase(option.Name),
            Description = option.Description,
            DefaultValue = "false"
        };
    }
}
```

### 4. Method Body Generation

```csharp
public class MethodBodyGenerator
{
    public void GenerateMethodBody(CodeBuilder builder, Command command)
    {
        builder.AddLine("var args = new List<string>();");
        
        // Add command name
        builder.AddLine($"args.Add(\"{command.Name}\");");
        
        // Handle options
        foreach (var option in command.Options ?? Enumerable.Empty<Option>())
        {
            GenerateOptionHandling(builder, option);
        }
        
        // Handle arguments
        foreach (var arg in command.Arguments ?? Enumerable.Empty<Argument>())
        {
            GenerateArgumentHandling(builder, arg);
        }
        
        // Execute command
        builder.AddLine();
        builder.AddLine("try");
        builder.BeginBlock();
        builder.AddLine("var result = await cliExecutor.ExecuteAsync(");
        builder.AddLine("    commandName,");
        builder.AddLine("    args,");
        builder.AddLine("    cancellationToken);");
        builder.AddLine();
        builder.AddLine("return result;");
        builder.EndBlock();
        
        // Error handling
        builder.AddLine("catch (CliExecutionException ex)");
        builder.BeginBlock();
        builder.AddLine("return JsonSerializer.Serialize(new");
        builder.BeginBlock();
        builder.AddLine("success = false,");
        builder.AddLine("exitCode = ex.ExitCode,");
        builder.AddLine("error = ex.Message,");
        builder.AddLine("output = ex.Output");
        builder.EndBlock(");");
        builder.EndBlock();
    }
    
    private void GenerateOptionHandling(CodeBuilder builder, Option option)
    {
        var paramName = ToCamelCase(option.Name);
        var hasArgument = option.Arguments?.Any() == true;
        
        if (hasArgument)
        {
            builder.AddLine($"if (!string.IsNullOrEmpty({paramName}))");
            builder.BeginBlock();
            builder.AddLine($"args.Add(\"--{option.Name}\");");
            builder.AddLine($"args.Add({paramName});");
            builder.EndBlock();
        }
        else
        {
            builder.AddLine($"if ({paramName})");
            builder.BeginBlock();
            builder.AddLine($"args.Add(\"--{option.Name}\");");
            builder.EndBlock();
        }
    }
}
```

## Conversion Examples

### Example 1: Git Status Command

**OpenCLI Input:**
```yaml
opencli: "0.1"
info:
  title: "git"
  version: "2.34.0"
commands:
  - name: "status"
    description: "Show working tree status"
    options:
      - name: "short"
        aliases: ["s"]
        description: "Give output in short format"
      - name: "branch"
        aliases: ["b"]
        description: "Show branch information"
```

**Generated MCP Tool:**
```csharp
[McpServerToolType]
public static class GitTools
{
    [McpServerTool]
    [Description("Show working tree status")]
    public static async Task<string> Git_status(
        ICliExecutor cliExecutor,
        [Description("Give output in short format")] bool shortFormat = false,
        [Description("Show branch information")] bool branch = false,
        CancellationToken cancellationToken = default)
    {
        var args = new List<string>();
        args.Add("status");
        
        if (shortFormat)
            args.Add("--short");
            
        if (branch)
            args.Add("--branch");
        
        try
        {
            var result = await cliExecutor.ExecuteAsync(
                "git",
                args,
                cancellationToken);
                
            return result;
        }
        catch (CliExecutionException ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                exitCode = ex.ExitCode,
                error = ex.Message,
                output = ex.Output
            });
        }
    }
}
```

### Example 2: Complex Command with Arguments

**OpenCLI Input:**
```yaml
commands:
  - name: "clone"
    description: "Clone a repository"
    arguments:
      - name: "repository"
        description: "Repository URL"
        required: true
        ordinal: 0
      - name: "directory"
        description: "Target directory"
        required: false
        ordinal: 1
    options:
      - name: "depth"
        description: "Create shallow clone"
        arguments:
          - name: "depth"
            description: "Number of commits"
```

**Generated MCP Tool:**
```csharp
[McpServerTool]
[Description("Clone a repository")]
public static async Task<string> Git_clone(
    ICliExecutor cliExecutor,
    [Description("Repository URL")] string repository,
    [Description("Target directory")] string? directory = null,
    [Description("Create shallow clone")] string? depth = null,
    CancellationToken cancellationToken = default)
{
    var args = new List<string>();
    args.Add("clone");
    
    if (!string.IsNullOrEmpty(depth))
    {
        args.Add("--depth");
        args.Add(depth);
    }
    
    args.Add(repository);
    
    if (!string.IsNullOrEmpty(directory))
    {
        args.Add(directory);
    }
    
    // ... execution logic
}
```

## CLI Executor Implementation

```csharp
public interface ICliExecutor
{
    Task<string> ExecuteAsync(
        string command, 
        IEnumerable<string> arguments, 
        CancellationToken cancellationToken);
}

public class CliExecutor : ICliExecutor
{
    private readonly ILogger<CliExecutor> _logger;
    private readonly CliExecutorOptions _options;
    
    public CliExecutor(ILogger<CliExecutor> logger, IOptions<CliExecutorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }
    
    public async Task<string> ExecuteAsync(
        string command, 
        IEnumerable<string> arguments, 
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = string.Join(" ", arguments.Select(EscapeArgument)),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _options.WorkingDirectory
        };
        
        // Add environment variables if configured
        foreach (var (key, value) in _options.EnvironmentVariables)
        {
            startInfo.Environment[key] = value;
        }
        
        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start process: {command}");
        }
        
        // Read output asynchronously
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        
        await process.WaitForExitAsync(cancellationToken);
        
        var output = await outputTask;
        var error = await errorTask;
        
        if (process.ExitCode != 0)
        {
            throw new CliExecutionException(command, process.ExitCode, output, error);
        }
        
        return output;
    }
    
    private string EscapeArgument(string arg)
    {
        // Handle special characters and spaces
        if (string.IsNullOrEmpty(arg))
            return "\"\"";
            
        if (!arg.Contains(' ') && !arg.Contains('"') && !arg.Contains('\\'))
            return arg;
            
        // Escape quotes and backslashes
        var escaped = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }
}
```

## Integration with MCP Server

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        // Configure services
        builder.Services.Configure<CliExecutorOptions>(
            builder.Configuration.GetSection("CliExecutor"));
        
        builder.Services.AddSingleton<ICliExecutor, CliExecutor>();
        
        // Add MCP server with generated tools
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<GitTools>()      // Generated from git.yaml
            .WithTools<DockerTools>()   // Generated from docker.yaml
            .WithTools<KubectlTools>(); // Generated from kubectl.yaml
        
        await builder.Build().RunAsync();
    }
}
```

## Advanced Features

### 1. Validation Support

```csharp
private void ValidateAndAddArgument(List<string> args, Argument arg, string value)
{
    // Validate against accepted values
    if (arg.AcceptedValues?.Any() == true)
    {
        if (!arg.AcceptedValues.Contains(value))
        {
            throw new McpException(
                McpErrorCode.InvalidParams,
                $"Invalid value '{value}'. Accepted: {string.Join(", ", arg.AcceptedValues)}"
            );
        }
    }
    
    args.Add(value);
}
```

### 2. Progress Reporting

```csharp
[McpServerTool]
public static async Task<string> Git_clone_with_progress(
    ICliExecutor cliExecutor,
    IMcpServer server,
    string repository,
    IProgress<ProgressNotificationValue> progress,
    CancellationToken cancellationToken = default)
{
    // Use process output to report progress
    var process = new Process();
    // ... setup process
    
    process.OutputDataReceived += (sender, e) =>
    {
        if (ParseProgress(e.Data, out var percent))
        {
            progress.Report(new ProgressNotificationValue
            {
                Progress = percent,
                Total = 100,
                ProgressToken = $"clone-{repository}"
            });
        }
    };
    
    // ... execute and return
}
```

### 3. Caching Support

```csharp
public class CachedCliExecutor : ICliExecutor
{
    private readonly IMemoryCache _cache;
    private readonly ICliExecutor _inner;
    
    public async Task<string> ExecuteAsync(
        string command, 
        IEnumerable<string> arguments, 
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{command}:{string.Join(",", arguments)}";
        
        if (_cache.TryGetValue<string>(cacheKey, out var cached))
        {
            return cached;
        }
        
        var result = await _inner.ExecuteAsync(command, arguments, cancellationToken);
        
        // Cache read-only operations
        if (IsReadOnlyCommand(command, arguments))
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        }
        
        return result;
    }
}
```

## Conclusion

This guide provides a comprehensive approach to converting OpenCLI specifications into MCP tools. The generated tools maintain the semantics of the original CLI while providing a standardized interface for AI systems to interact with command-line applications.