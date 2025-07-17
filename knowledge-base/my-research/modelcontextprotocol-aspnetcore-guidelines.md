# ModelContextProtocol and ModelContextProtocol.AspNetCore API Specification

## Overview

ModelContextProtocol.AspNetCore is the official .NET SDK for implementing Model Context Protocol (MCP) servers. It provides a comprehensive framework for exposing tools, prompts, and resources to AI systems through a standardized protocol.

## Core Architecture

### Protocol Foundation

MCP is built on JSON-RPC 2.0 and provides:
- **Client-Server Architecture**: AI applications (clients) connect to MCP servers
- **Bidirectional Communication**: Request-response and notification patterns
- **Transport Agnostic**: Supports multiple transport mechanisms
- **Type-Safe**: Strong typing with .NET's type system

### Connection Lifecycle

1. **Initialization Phase**
   - Client sends `initialize` request with protocol version
   - Server responds with capabilities
   - Client sends `initialized` notification

2. **Active Phase**
   - Tool invocations
   - Resource access
   - Prompt generation
   - Progress notifications

3. **Termination Phase**
   - Clean shutdown via protocol
   - Transport disconnection
   - Error-based termination

## API Components

### 1. Attributes System

#### McpServerToolType Attribute

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class McpServerToolTypeAttribute : Attribute
{
    // Marks a class as containing MCP tools
}
```

**Usage:**
```csharp
[McpServerToolType]
public class DatabaseTools
{
    // Tool methods defined here
}
```

#### McpServerTool Attribute

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class McpServerToolAttribute : Attribute
{
    // Marks a method as an MCP tool
}
```

**Usage:**
```csharp
[McpServerTool]
[Description("Query database with SQL")]
public async Task<string> ExecuteQuery(string sql) { }
```

#### McpServerPromptType and McpServerPrompt

```csharp
[McpServerPromptType]
public class AnalysisPrompts
{
    [McpServerPrompt]
    [Description("Generate data analysis prompt")]
    public string GenerateAnalysisPrompt(string dataType)
    {
        return $"Analyze the following {dataType} data...";
    }
}
```

#### McpServerResourceType and McpServerResource

```csharp
[McpServerResourceType]
public class ConfigurationResources
{
    [McpServerResource]
    [Description("Application configuration")]
    public ConfigData GetConfiguration()
    {
        return LoadConfiguration();
    }
}
```

### 2. Server Configuration

#### Basic Setup

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();
            
        await builder.Build().RunAsync();
    }
}
```

#### Advanced Configuration

```csharp
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new ServerInfo
        {
            Name = "MyMcpServer",
            Version = "1.0.0"
        };
    })
    .WithStdioServerTransport(transport =>
    {
        transport.LogToStderr = true;
    })
    .WithTools<DatabaseTools>()
    .WithPrompts<AnalysisPrompts>()
    .WithResources<ConfigurationResources>();
```

### 3. Transport Mechanisms

#### STDIO Transport

Best for subprocess-based communication:

```csharp
.WithStdioServerTransport(options =>
{
    options.LogToStderr = true; // Logs go to stderr
})
```

Features:
- JSON-RPC over stdin/stdout
- Newline-delimited messages
- Ideal for local process communication

#### HTTP/SSE Transport

For web-based deployments:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport();

var app = builder.Build();
app.MapMcp("/mcp"); // Endpoint for MCP
app.Run();
```

Features:
- HTTP POST for client→server
- Server-Sent Events for server→client
- Session management
- Multiple concurrent clients

#### Custom Transport

```csharp
public class CustomTransport : IServerTransport
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Custom transport implementation
    }
}

// Registration
.WithTransport<CustomTransport>()
```

### 4. Dependency Injection

#### Service Injection in Tools

```csharp
[McpServerToolType]
public class DataTools
{
    [McpServerTool]
    public async Task<string> ProcessData(
        IDataService dataService,        // Injected
        ILogger<DataTools> logger,       // Injected
        [Description("Data to process")] string data,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing data");
        return await dataService.ProcessAsync(data, cancellationToken);
    }
}
```

#### Scoped Services

```csharp
// Register scoped service
builder.Services.AddScoped<IRequestContext>();

// Use in tool
[McpServerTool]
public async Task<string> GetUserData(
    IRequestContext context,  // Scoped per request
    string userId)
{
    return await context.GetUserDataAsync(userId);
}
```

### 5. Parameter Handling

#### Supported Parameter Types

1. **Simple Types**: `string`, `int`, `bool`, `double`, etc.
2. **Complex Types**: Custom classes (serialized as JSON)
3. **Collections**: `List<T>`, `T[]`, `Dictionary<string, T>`
4. **Special Parameters**:
   - `CancellationToken` - Automatically bound
   - `IProgress<T>` - For progress reporting
   - `IMcpServer` - Access to MCP server instance

#### Parameter Attributes

```csharp
[McpServerTool]
public async Task<SearchResult> Search(
    [Description("Search query")] string query,
    [Description("Maximum results"), Range(1, 100)] int maxResults = 10,
    [Description("Include metadata")] bool includeMetadata = false)
{
    // Implementation
}
```

### 6. Error Handling

#### Standard Error Codes

```csharp
public enum McpErrorCode
{
    ParseError = -32700,
    InvalidRequest = -32600,
    MethodNotFound = -32601,
    InvalidParams = -32602,
    InternalError = -32603
}
```

#### Custom Error Handling

```csharp
[McpServerTool]
public async Task<string> RiskyOperation(string input)
{
    try
    {
        return await PerformOperation(input);
    }
    catch (ValidationException ex)
    {
        throw new McpException(McpErrorCode.InvalidParams, ex.Message);
    }
    catch (Exception ex)
    {
        throw new McpException(McpErrorCode.InternalError, "Operation failed", ex);
    }
}
```

### 7. Progress Reporting

```csharp
[McpServerTool]
public async Task<string> LongRunningTask(
    IMcpServer server,
    IProgress<ProgressNotificationValue> progress,
    string taskName)
{
    for (int i = 0; i <= 100; i += 10)
    {
        progress.Report(new ProgressNotificationValue
        {
            Progress = i,
            Total = 100,
            ProgressToken = taskName
        });
        
        await Task.Delay(500);
    }
    
    return "Task completed";
}
```

### 8. Resource Management

#### Static Resources

```csharp
[McpServerResourceType]
public class FileResources
{
    [McpServerResource]
    [Description("Configuration files")]
    public IEnumerable<Resource> GetConfigFiles()
    {
        return Directory.GetFiles("./config", "*.json")
            .Select(f => new Resource
            {
                Uri = $"file://{f}",
                Name = Path.GetFileName(f),
                MimeType = "application/json"
            });
    }
}
```

#### Dynamic Resources with Templates

```csharp
[McpServerResource("database://tables/{tableName}")]
[Description("Database table resource")]
public async Task<Resource> GetTableResource(
    string tableName,
    IDatabaseService db)
{
    var schema = await db.GetTableSchemaAsync(tableName);
    return new Resource
    {
        Uri = $"database://tables/{tableName}",
        Name = tableName,
        MimeType = "application/json",
        Text = JsonSerializer.Serialize(schema)
    };
}
```

### 9. Prompt Management

```csharp
[McpServerPromptType]
public class CodeGenerationPrompts
{
    private readonly ITemplateService _templates;
    
    public CodeGenerationPrompts(ITemplateService templates)
    {
        _templates = templates;
    }
    
    [McpServerPrompt]
    [Description("Generate code from specification")]
    public async Task<Prompt> GenerateCodePrompt(
        [Description("Language")] string language,
        [Description("Specification")] string spec)
    {
        var template = await _templates.GetTemplateAsync(language);
        
        return new Prompt
        {
            Name = $"generate-{language}-code",
            Description = $"Generate {language} code from specification",
            Arguments = new[]
            {
                new PromptArgument
                {
                    Name = "specification",
                    Description = "Code specification",
                    Required = true
                }
            },
            Messages = new[]
            {
                new PromptMessage
                {
                    Role = "system",
                    Content = new TextContent
                    {
                        Text = template.SystemPrompt
                    }
                },
                new PromptMessage
                {
                    Role = "user",
                    Content = new TextContent
                    {
                        Text = $"Generate {language} code for: {spec}"
                    }
                }
            }
        };
    }
}
```

### 10. Security Features

#### Origin Validation

```csharp
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.AllowedOrigins = new[] { "https://trusted-app.com" };
        options.RequireOriginHeader = true;
    });
```

#### Authentication

```csharp
public class AuthenticatedMcpServer : DelegatingMcpServer
{
    protected override async Task<TResult> SendRequestAsync<TParams, TResult>(
        string method,
        TParams @params,
        CancellationToken cancellationToken)
    {
        // Validate authentication
        if (!IsAuthenticated())
        {
            throw new McpException(McpErrorCode.InvalidRequest, "Unauthorized");
        }
        
        return await base.SendRequestAsync<TParams, TResult>(method, @params, cancellationToken);
    }
}
```

## Advanced Use Cases

### 1. Database Query Tool

```csharp
[McpServerToolType]
public class DatabaseQueryTool
{
    [McpServerTool]
    [Description("Execute SQL query with parameterized inputs")]
    public async Task<QueryResult> ExecuteQuery(
        ISqlConnection connection,
        ILogger<DatabaseQueryTool> logger,
        [Description("SQL query")] string query,
        [Description("Query parameters")] Dictionary<string, object>? parameters = null,
        [Description("Max rows to return")] int maxRows = 1000,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executing query: {Query}", query);
        
        using var command = connection.CreateCommand(query);
        
        if (parameters != null)
        {
            foreach (var (key, value) in parameters)
            {
                command.AddParameter(key, value);
            }
        }
        
        var results = await command.ExecuteAsync(maxRows, cancellationToken);
        
        return new QueryResult
        {
            Columns = results.Columns,
            Rows = results.Rows,
            RowCount = results.Rows.Count
        };
    }
}
```

### 2. File System Explorer

```csharp
[McpServerToolType]
public class FileSystemTools
{
    private readonly IFileSystemSecurity _security;
    
    public FileSystemTools(IFileSystemSecurity security)
    {
        _security = security;
    }
    
    [McpServerTool]
    [Description("List files in directory")]
    public async Task<FileListResult> ListFiles(
        [Description("Directory path")] string path,
        [Description("File pattern")] string pattern = "*",
        [Description("Include subdirectories")] bool recursive = false)
    {
        // Validate path is allowed
        if (!_security.IsPathAllowed(path))
        {
            throw new McpException(McpErrorCode.InvalidParams, "Access denied");
        }
        
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(path, pattern, searchOption);
        
        return new FileListResult
        {
            Path = path,
            Files = files.Select(f => new FileInfo(f)).Select(fi => new FileEntry
            {
                Name = fi.Name,
                FullPath = fi.FullName,
                Size = fi.Length,
                Modified = fi.LastWriteTimeUtc
            }).ToList()
        };
    }
}
```

### 3. AI Integration

```csharp
[McpServerToolType]
public class AiTools
{
    [McpServerTool]
    [Description("Generate embeddings for text")]
    public async Task<float[]> GenerateEmbedding(
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        [Description("Text to embed")] string text,
        CancellationToken cancellationToken = default)
    {
        var result = await embeddings.GenerateEmbeddingAsync(text, cancellationToken);
        return result.Vector.ToArray();
    }
    
    [McpServerTool]
    [Description("Chat with AI model")]
    public async Task<string> Chat(
        IChatClient chatClient,
        [Description("User message")] string message,
        [Description("System prompt")] string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>();
        
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }
        
        messages.Add(new ChatMessage(ChatRole.User, message));
        
        var response = await chatClient.CompleteAsync(messages, cancellationToken);
        return response.Message.Text ?? string.Empty;
    }
}
```

## Native AOT Support

For deployment as self-contained executables:

```csharp
// Program.cs
var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<MyTools>(); // Explicit registration for AOT

await builder.Build().RunAsync();
```

Project configuration:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <PublishAot>true</PublishAot>
  </PropertyGroup>
</Project>
```

## Best Practices

### 1. Tool Design
- Keep tools focused on single responsibilities
- Use clear, descriptive names
- Provide comprehensive descriptions
- Validate inputs thoroughly

### 2. Error Handling
- Use appropriate MCP error codes
- Provide helpful error messages
- Log errors for debugging
- Clean up resources on failure

### 3. Performance
- Use async/await for I/O operations
- Implement cancellation properly
- Report progress for long operations
- Consider caching for expensive operations

### 4. Security
- Validate all inputs
- Use parameterized queries
- Implement path traversal protection
- Apply principle of least privilege

### 5. Testing
- Unit test tool methods
- Integration test with transport
- Test error scenarios
- Validate descriptions and metadata

## Conclusion

ModelContextProtocol.AspNetCore provides a robust, extensible framework for building MCP servers in .NET. Its integration with the .NET ecosystem, comprehensive attribute system, and flexible transport options make it ideal for exposing tools, prompts, and resources to AI systems in a standardized, type-safe manner.