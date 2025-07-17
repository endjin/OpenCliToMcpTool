using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using OpenCliToMcp.Generator.Diagnostics;
using OpenCliToMcp.Generator.Models;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace OpenCliToMcp.Generator;

// Internal types for pipeline processing
internal sealed record ParseResult(string FilePath, OpenCliSpec? Spec, string? Error);

internal sealed record AttributeTarget(
    INamedTypeSymbol ClassSymbol,
    string FilePath,
    Location AttributeLocation);

internal sealed class ParseResultComparer : IEqualityComparer<ParseResult>
{
    public static readonly ParseResultComparer Instance = new();
    
    public bool Equals(ParseResult? x, ParseResult? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }
        
        return x.FilePath == y.FilePath &&
               EqualityComparer<OpenCliSpec?>.Default.Equals(x.Spec, y.Spec) &&
               x.Error == y.Error;
    }
    
    public int GetHashCode(ParseResult obj)
    {
        return Generator.HashCode.Combine(obj.FilePath, obj.Spec, obj.Error);
    }
}

internal sealed class OpenCliSpecComparer : IEqualityComparer<OpenCliSpec>
{
    public static readonly OpenCliSpecComparer Instance = new();
    
    public bool Equals(OpenCliSpec? x, OpenCliSpec? y)
    {
        return EqualityComparer<OpenCliSpec?>.Default.Equals(x, y);
    }
    
    public int GetHashCode(OpenCliSpec obj)
    {
        return obj.GetHashCode();
    }
}

[Generator]
public class OpenCliToMcpGenerator : IIncrementalGenerator
{
    private static readonly HashSet<string> CSharpKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
        "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
        "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Detect classes with OpenCliToolAttribute
        IncrementalValuesProvider<AttributeTarget> attributeTargets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "OpenCliToMcp.OpenCliToolAttribute",
                predicate: (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax,
                transform: (ctx, ct) =>
                {
                    INamedTypeSymbol classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
                    AttributeData attribute = ctx.Attributes.First();
                    
                    // Extract file path from attribute
                    string? filePath = attribute.ConstructorArguments[0].Value as string;
                    return string.IsNullOrEmpty(filePath) ? null : new AttributeTarget(classSymbol, filePath!, attribute.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation() ?? Location.None);
                })
            .Where(target => target != null)
            .Select((target, _) => target!);

        // Parse OpenCLI files once and transform through pipeline
        IncrementalValuesProvider<ParseResult> openCliSpecs = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".opencli.json", StringComparison.OrdinalIgnoreCase))
            .Select((file, ct) =>
            {
                string? content = file.GetText(ct)?.ToString();
                if (string.IsNullOrEmpty(content))
                    return new ParseResult(file.Path, null, "File is empty");
                
                try
                {
                    OpenCliSpec? spec = ParseOpenCliSpec(content!);
                    if (spec?.Info?.Title == null)
                        return new ParseResult(file.Path, null, "Missing required 'info.title' field");
                    
                    return new ParseResult(file.Path, spec, null);
                }
                catch (Exception ex)
                {
                    return new ParseResult(file.Path, null, $"Invalid JSON: {ex.Message}");
                }
            })
            .WithComparer(ParseResultComparer.Instance);


        // Report parse errors
        context.RegisterSourceOutput(
            openCliSpecs.Where(r => r.Error != null),
            (ctx, result) =>
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidJsonFormat,
                    Location.None,
                    result.FilePath));
            });

        // Extract valid specs
        IncrementalValuesProvider<OpenCliSpec> validSpecs = openCliSpecs
            .Where(r => r.Spec != null)
            .Select((r, ct) => r.Spec!)
            .WithComparer(OpenCliSpecComparer.Instance);
        
        // Validate command names in valid specs
        context.RegisterSourceOutput(
            openCliSpecs.Where(r => r.Spec != null),
            (ctx, result) =>
            {
                if (result.Spec?.Commands != null)
                {
                    HashSet<string> commandNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    ValidateCommandNames(result.Spec.Commands, commandNames, result.FilePath, ctx.ReportDiagnostic);
                }
            });
        
        // Generate ICliExecutor interface when any valid spec exists
        context.RegisterSourceOutput(
            validSpecs.Collect(),
            (ctx, specs) =>
            {
                if (specs.Any())
                {
                    string source = GenerateCliExecutorInterface();
                    SourceText sourceText = SourceText.From(source, Encoding.UTF8);
                    ctx.AddSource("ICliExecutor.g.cs", sourceText);
                }
            });
        
        // Generate MCP tool classes (implementation details, so use RegisterImplementationSourceOutput)
        context.RegisterImplementationSourceOutput(
            validSpecs.Where(spec => spec.Commands?.Count > 0),
            (ctx, spec) =>
            {
                string toolName = GetToolName(spec.Info!.Title);
                string source = GenerateMcpToolClass(toolName, spec);
                SourceText sourceText = SourceText.From(source, Encoding.UTF8);
                ctx.AddSource($"{toolName}Mcp.g.cs", sourceText);
            });

        // Validate attribute targets
        context.RegisterSourceOutput(
            attributeTargets,
            (ctx, target) =>
            {
                // Check if class is partial
                if (!target.ClassSymbol.DeclaringSyntaxReferences.Any(r => 
                    r.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax cds && 
                    cds.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ClassNotPartial,
                        target.AttributeLocation,
                        target.ClassSymbol.Name));
                }
                
                // Check if class is static
                if (target.ClassSymbol.IsStatic)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ClassIsStatic,
                        target.AttributeLocation,
                        target.ClassSymbol.Name));
                }
            });

        // Process attribute targets and find matching specs
        IncrementalValuesProvider<(AttributeTarget target, ParseResult?, ImmutableArray<AdditionalText> additionalFiles)> attributeWithSpecs = attributeTargets
            .Combine(context.AdditionalTextsProvider.Collect())
            .Select((pair, ct) =>
            {
                (AttributeTarget? target, ImmutableArray<AdditionalText> additionalFiles) = pair;
                
                // Normalize the file path from the attribute
                string normalizedPath = NormalizeFilePath(target.FilePath, target.ClassSymbol);
                
                // Find matching AdditionalFile
                Microsoft.CodeAnalysis.AdditionalText? matchingFile = null;
                foreach (AdditionalText? file in additionalFiles)
                {
                    try
                    {
                        string normalizedFilePath = System.IO.Path.GetFullPath(file.Path);
                        if (PathsMatch(normalizedFilePath, normalizedPath))
                        {
                            matchingFile = file;
                            break;
                        }
                    }
                    catch { }
                }
                
                if (matchingFile == null)
                {
                    return (target, null as ParseResult, additionalFiles);
                }
                
                // Parse the file
                string? content = matchingFile.GetText(ct)?.ToString();
                if (string.IsNullOrEmpty(content))
                {
                    return (target, new ParseResult(matchingFile.Path, null, "File is empty"), additionalFiles);
                }
                
                try
                {
                    OpenCliSpec? spec = ParseOpenCliSpec(content!);
                    if (spec?.Info?.Title == null)
                    {
                        return (target, new ParseResult(matchingFile.Path, null, "Missing required 'info.title' field"), additionalFiles);
                    }
                    
                    return (target, new ParseResult(matchingFile.Path, spec, null), additionalFiles);
                }
                catch (Exception ex)
                {
                    return (target, new ParseResult(matchingFile.Path, null, $"Invalid JSON: {ex.Message}"), additionalFiles);
                }
            });

        // Report missing files
        context.RegisterSourceOutput(
            attributeWithSpecs.Where(pair => pair.Item2 == null),
            (ctx, pair) =>
            {
                // Debug: list all available files with normalized paths
                string normalizedPath = NormalizeFilePath(pair.target.FilePath, pair.target.ClassSymbol);
                string availableFiles = string.Join(", ", pair.additionalFiles.Select(f => 
                {
                    try 
                    {
                        return $"'{f.Path}' -> '{System.IO.Path.GetFullPath(f.Path)}'";
                    }
                    catch 
                    {
                        return $"'{f.Path}' (failed to normalize)";
                    }
                }));
                
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FileNotFound,
                    pair.target.AttributeLocation,
                    pair.target.FilePath,
                    $" (looking for: '{normalizedPath}', available: {availableFiles})"));
            });

        // Report errors from attribute processing
        context.RegisterSourceOutput(
            attributeWithSpecs.Where(pair => pair.Item2?.Error != null),
            (ctx, pair) =>
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidJsonFormat,
                    pair.target.AttributeLocation,
                    pair.Item2!.FilePath));
            });
        
        // Generate partial classes for attribute targets
        context.RegisterImplementationSourceOutput(
            attributeWithSpecs.Where(pair => pair.Item2?.Spec != null && pair.Item2.Spec.Commands?.Count > 0),
            (ctx, pair) =>
            {
                string source = GeneratePartialClass(pair.target.ClassSymbol, pair.Item2!.Spec!);
                SourceText sourceText = SourceText.From(source, Encoding.UTF8);
                ctx.AddSource($"{pair.target.ClassSymbol.Name}.g.cs", sourceText);
            });
    }
    
    private static string GenerateCliExecutorInterface()
    {
        // Instead of generating the interface, we'll generate a using alias
        // This allows existing code to work while using the Core library's interface
        return """
            // <auto-generated/>
            // This file provides compatibility with the Core library

            global using ICliExecutor = OpenCliToMcp.Core.ICliExecutor;
            """;
    }
    
    private static OpenCliSpec? ParseOpenCliSpec(string jsonContent)
    {
        JsonValue root = SimpleJsonParser.Parse(jsonContent);
        
        string? opencli = root.GetProperty("opencli")?.GetString();
        
        // Parse info section
        OpenCliInfo? info = null;
        JsonValue? infoObj = root.GetProperty("info");
        if (infoObj != null)
        {
            string? title = infoObj.GetProperty("title")?.GetString();
            string? version = infoObj.GetProperty("version")?.GetString();
            string? description = infoObj.GetProperty("description")?.GetString();
            
            info = new OpenCliInfo(title, version, description);
        }
        
        // Parse global options
        IReadOnlyList<OpenCliOption>? options = null;
        JsonValue? globalOptions = root.GetProperty("options");
        if (globalOptions != null && globalOptions.Type == JsonValueType.Array)
        {
            options = ParseOptions(globalOptions);
        }
        
        // Parse commands
        IReadOnlyDictionary<string, OpenCliCommand>? commands = null;
        JsonValue? commandsObj = root.GetProperty("commands");
        if (commandsObj != null)
        {
            Dictionary<string, OpenCliCommand> commandsDict = new Dictionary<string, OpenCliCommand>();
            
            foreach (KeyValuePair<string, JsonValue> cmd in commandsObj.EnumerateObject())
            {
                OpenCliCommand? command = ParseCommand(cmd.Value);
                if (command != null)
                {
                    commandsDict[cmd.Key] = command;
                }
            }
            
            if (commandsDict.Count > 0)
            {
                commands = commandsDict.ToImmutableDictionary();
            }
        }
        
        return new OpenCliSpec(opencli, info, commands, options);
    }
    
    private static OpenCliCommand? ParseCommand(JsonValue cmdValue)
    {
        string? description = cmdValue.GetProperty("description")?.GetString();
        
        // Parse arguments
        IReadOnlyList<OpenCliArgument>? arguments = null;
        JsonValue? argumentsArray = cmdValue.GetProperty("arguments");
        if (argumentsArray is { Type: JsonValueType.Array })
        {
            List<OpenCliArgument> argsList = [];
            
            foreach (JsonValue? arg in argumentsArray.EnumerateArray())
            {
                string? name = arg.GetProperty("name")?.GetString();
                string? desc = arg.GetProperty("description")?.GetString();
                bool required = arg.GetProperty("required")?.GetBoolean() ?? false;
                int ordinal = arg.GetProperty("ordinal")?.GetInt32() ?? 0;
                
                argsList.Add(new OpenCliArgument(name, desc, required, ordinal));
            }
            
            if (argsList.Count > 0)
            {
                arguments = argsList.ToImmutableList();
            }
        }
        
        // Parse options
        IReadOnlyList<OpenCliOption>? options = null;
        JsonValue? optionsArray = cmdValue.GetProperty("options");
        if (optionsArray is { Type: JsonValueType.Array })
        {
            options = ParseOptions(optionsArray);
        }
        
        // Parse exit codes
        IReadOnlyList<OpenCliExitCode>? exitCodes = null;
        JsonValue? exitCodesArray = cmdValue.GetProperty("exitCodes");
        if (exitCodesArray is { Type: JsonValueType.Array })
        {
            List<OpenCliExitCode> exitCodesList = [];
            
            foreach (JsonValue? exitCode in exitCodesArray.EnumerateArray())
            {
                int code = exitCode.GetProperty("code")?.GetInt32() ?? 0;
                string? exitDesc = exitCode.GetProperty("description")?.GetString();
                
                exitCodesList.Add(new OpenCliExitCode(code, exitDesc));
            }
            
            if (exitCodesList.Count > 0)
            {
                exitCodes = exitCodesList.ToImmutableList();
            }
        }
        
        // Parse examples
        IReadOnlyList<OpenCliExample>? examples = null;
        JsonValue? examplesArray = cmdValue.GetProperty("examples");
        if (examplesArray is { Type: JsonValueType.Array })
        {
            List<OpenCliExample> examplesList = [];
            
            foreach (JsonValue? example in examplesArray.EnumerateArray())
            {
                string? cmd = example.GetProperty("command")?.GetString();
                string? exDesc = example.GetProperty("description")?.GetString();
                
                examplesList.Add(new OpenCliExample(cmd, exDesc));
            }
            
            if (examplesList.Count > 0)
            {
                examples = examplesList.ToImmutableList();
            }
        }
        
        // Parse nested commands
        IReadOnlyDictionary<string, OpenCliCommand>? nestedCommands = null;
        JsonValue? nestedCommandsObj = cmdValue.GetProperty("commands");
        if (nestedCommandsObj != null)
        {
            Dictionary<string, OpenCliCommand> nestedDict = new Dictionary<string, OpenCliCommand>();
            
            foreach (KeyValuePair<string, JsonValue> nestedCmd in nestedCommandsObj.EnumerateObject())
            {
                OpenCliCommand? nestedCommand = ParseCommand(nestedCmd.Value);
                if (nestedCommand != null)
                {
                    nestedDict[nestedCmd.Key] = nestedCommand;
                }
            }
            
            if (nestedDict.Count > 0)
            {
                nestedCommands = nestedDict.ToImmutableDictionary();
            }
        }
        
        return new OpenCliCommand(description, arguments, options, nestedCommands, exitCodes, examples);
    }
    
    private static IReadOnlyList<OpenCliOption> ParseOptions(JsonValue optionsArray)
    {
        List<OpenCliOption> options = [];
        
        foreach (JsonValue? opt in optionsArray.EnumerateArray())
        {
            string? name = opt.GetProperty("name")?.GetString();
            string? description = opt.GetProperty("description")?.GetString();
            
            // Parse aliases
            IReadOnlyList<string>? aliases = null;
            JsonValue? aliasesArray = opt.GetProperty("aliases");
            if (aliasesArray is { Type: JsonValueType.Array })
            {
                List<string> aliasesList = [];
                foreach (JsonValue? alias in aliasesArray.EnumerateArray())
                {
                    string? aliasStr = alias.GetString();
                    if (aliasStr != null)
                    {
                        aliasesList.Add(aliasStr);
                    }
                }
                
                if (aliasesList.Count > 0)
                {
                    aliases = aliasesList.ToImmutableList();
                }
            }
            
            // Parse option arguments
            IReadOnlyList<OpenCliArgument>? arguments = null;
            JsonValue? argumentsArray = opt.GetProperty("arguments");

            if (argumentsArray is { Type: JsonValueType.Array })
            {
                List<OpenCliArgument> argsList = [];
                
                foreach (JsonValue? arg in argumentsArray.EnumerateArray())
                {
                    string? argName = arg.GetProperty("name")?.GetString();
                    string? argDesc = arg.GetProperty("description")?.GetString();
                    bool argRequired = arg.GetProperty("required")?.GetBoolean() ?? false;
                    
                    argsList.Add(new OpenCliArgument(argName, argDesc, argRequired, 0));
                }
                
                if (argsList.Count > 0)
                {
                    arguments = argsList.ToImmutableList();
                }
            }
            
            options.Add(new OpenCliOption(name, aliases, description, arguments));
        }
        
        return options.Count > 0 ? options.ToImmutableList() : ImmutableList<OpenCliOption>.Empty;
    }
    
    private static string GetToolName(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "UnknownTool";
        
        // Remove "Tool" suffix if present
        string trimmedTitle = title!.Trim();
        ReadOnlySpan<char> name = trimmedTitle.AsSpan();
        if (trimmedTitle.EndsWith(" Tool", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Slice(0, name.Length - 5);
        }
        
        // Convert to PascalCase with StringBuilder
        StringBuilder sb = new StringBuilder(name.Length + 4);
        bool capitalizeNext = true;
        
        foreach (char c in name)
        {
            if (c == ' ')
            {
                capitalizeNext = true;
            }
            else if (capitalizeNext)
            {
                sb.Append(char.ToUpper(c));
                capitalizeNext = false;
            }
            else
            {
                sb.Append(char.ToLower(c));
            }
        }
        
        sb.Append("Tool");
        return sb.ToString();
    }
    
    private static string GenerateMcpToolClass(string toolName, OpenCliSpec spec)
    {
        StringBuilder sb = new();
        sb.AppendLine($$"""
            // <auto-generated/>
            #nullable enable
            using ModelContextProtocol.Server;
            using OpenCliToMcp.Core;
            using OpenCliToMcp.Generated;
            using System.Collections.Generic;
            using System.ComponentModel;
            using System.Threading;
            using System.Threading.Tasks;

            namespace OpenCliToMcp.Generated
            {
                [McpServerToolType]
                public static class {{toolName}}Mcp
                {
            """);
        
        if (spec.Commands != null)
        {
            foreach (KeyValuePair<string, OpenCliCommand> cmd in spec.Commands)
            {
                GenerateCommandMethod(sb, cmd.Key, cmd.Value, spec.Info?.Title, ImmutableList<string>.Empty, spec.Options);
            }
        }
        
        sb.AppendLine("""
                }
            }
            """);
        
        return sb.ToString();
    }
    
    private static void GenerateCommandMethod(StringBuilder sb, string commandName, OpenCliCommand? command, string? toolTitle, IReadOnlyList<string> parentCommands, IReadOnlyList<OpenCliOption>? globalOptions)
    {
        if (command == null) return;

        List<string> commandPath = parentCommands.Concat([commandName]).ToList();
        string methodName = string.Join("", commandPath.Select(c => GetMethodName(c)));
        
        // Generate XML documentation
        string xmlDoc = GenerateXmlDocumentation(command, "        ");
        sb.Append(xmlDoc);
        sb.AppendLine($"        [McpServerTool]");
        if (!string.IsNullOrEmpty(command.Description))
        {
            sb.AppendLine($"        [Description(\"{EscapeString(command.Description!)}\")]");
        }
        
        // Start method signature  
        sb.Append($"        public static async Task<string> {methodName}Async(");
        
        // Generate parameters using the helper method
        var (parameters, argumentParameterMap, optionParameterMap) = GenerateParameterLists(command, globalOptions, includeCliExecutor: true);
        
        sb.AppendLine(string.Join(", ", parameters) + ")");
        
        sb.AppendLine("        {");
        
        // Generate method body
        GenerateMethodBody(sb, commandPath, command, toolTitle, argumentParameterMap, optionParameterMap, globalOptions);
        
        sb.AppendLine("        }");
        sb.AppendLine();
        
        // Generate methods for nested commands
        if (command.Commands != null)
        {
            foreach (KeyValuePair<string, OpenCliCommand> nestedCmd in command.Commands)
            {
                GenerateCommandMethod(sb, nestedCmd.Key, nestedCmd.Value, toolTitle, commandPath, globalOptions);
            }
        }
    }
    
    private static string GetMethodName(string commandName)
    {
        return char.ToUpper(commandName[0]) + commandName.Substring(1);
    }
    
    private static bool IsValidCSharpIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
            
        // Check if first character is valid (letter or underscore)
        if (!char.IsLetter(name[0]) && name[0] != '_')
            return false;
            
        // Check remaining characters (letters, digits, or underscore)
        for (int i = 1; i < name.Length; i++)
        {
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                return false;
        }
        
        // Check if it's a C# keyword
        return !CSharpKeywords.Contains(name);
    }
    
    private static bool ValidateCommandNames(IReadOnlyDictionary<string, OpenCliCommand>? commands, HashSet<string> usedNames, string filePath, Action<Diagnostic> reportDiagnostic)
    {
        if (commands == null)
            return true;
            
        bool isValid = true;
        
        foreach (var kvp in commands)
        {
            string commandName = kvp.Key;
            
            // Check for valid C# identifier
            if (!IsValidCSharpIdentifier(commandName))
            {
                reportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidCommandName,
                    Location.None,
                    commandName,
                    filePath));
                isValid = false;
            }
            
            // Check for duplicates at this level
            if (!usedNames.Add(commandName))
            {
                reportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DuplicateCommandName,
                    Location.None,
                    commandName,
                    filePath));
                isValid = false;
            }
            
            // Recursively validate nested commands
            if (kvp.Value?.Commands != null)
            {
                HashSet<string> nestedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!ValidateCommandNames(kvp.Value.Commands, nestedNames, filePath, reportDiagnostic))
                    isValid = false;
            }
        }
        
        return isValid;
    }
    
    private static (List<string> parameters, Dictionary<string, string> argumentMap, Dictionary<string, string> optionMap) 
        GenerateParameterLists(OpenCliCommand? command, IReadOnlyList<OpenCliOption>? globalOptions, bool includeCliExecutor)
    {
        List<string> parameters = [];
        HashSet<string> usedParameterNames = ["cliExecutor", "cancellationToken"];
        Dictionary<string, string> argumentParameterMap = new Dictionary<string, string>();
        Dictionary<string, string> optionParameterMap = new Dictionary<string, string>();
        
        // Add ICliExecutor if needed (for static methods)
        if (includeCliExecutor)
        {
            parameters.Add("ICliExecutor cliExecutor");
        }
        
        // Add arguments ordered by ordinal
        if (command?.Arguments != null)
        {
            IOrderedEnumerable<OpenCliArgument> orderedArgs = command.Arguments.OrderBy(a => a.Ordinal);
            foreach (OpenCliArgument? arg in orderedArgs)
            {
                string paramType = arg.Required ? "string" : "string?";
                string originalParamName = ToCamelCase(arg.Name ?? "arg");
                string paramName = EscapeParameterName(originalParamName, usedParameterNames);
                argumentParameterMap[arg.Name ?? "arg"] = paramName;
                string defaultValue = arg.Required ? "" : " = null";
                
                if (!string.IsNullOrEmpty(arg.Description))
                {
                    parameters.Add($"[Description(\"{EscapeString(arg.Description!)}\")] {paramType} {paramName}{defaultValue}");
                }
                else
                {
                    parameters.Add($"{paramType} {paramName}{defaultValue}");
                }
            }
        }
        
        // Add global options first
        if (globalOptions != null)
        {
            foreach (OpenCliOption? option in globalOptions)
            {
                AddOptionParameter(option, parameters, optionParameterMap, usedParameterNames);
            }
        }
        
        // Add command-specific options
        if (command?.Options != null)
        {
            foreach (OpenCliOption? option in command.Options)
            {
                AddOptionParameter(option, parameters, optionParameterMap, usedParameterNames);
            }
        }
        
        // Always add CancellationToken last
        parameters.Add("CancellationToken cancellationToken = default");
        
        return (parameters, argumentParameterMap, optionParameterMap);
    }
    
    private static void AddOptionParameter(OpenCliOption option, List<string> parameters, 
        Dictionary<string, string> optionParameterMap, HashSet<string> usedParameterNames)
    {
        string originalParamName = ToCamelCase(option.Name ?? "option");
        string paramName = EscapeParameterName(originalParamName, usedParameterNames);
        optionParameterMap[option.Name ?? "option"] = paramName;
        
        // Determine if option has arguments
        if (option.Arguments?.Any() == true)
        {
            // Option with value
            string paramType = "string?";
            string defaultValue = " = null";
            
            if (!string.IsNullOrEmpty(option.Description))
            {
                parameters.Add($"[Description(\"{EscapeString(option.Description!)}\")] {paramType} {paramName}{defaultValue}");
            }
            else
            {
                parameters.Add($"{paramType} {paramName}{defaultValue}");
            }
        }
        else
        {
            // Boolean flag option
            string paramType = "bool";
            string defaultValue = " = false";
            
            if (!string.IsNullOrEmpty(option.Description))
            {
                parameters.Add($"[Description(\"{EscapeString(option.Description!)}\")] {paramType} {paramName}{defaultValue}");
            }
            else
            {
                parameters.Add($"{paramType} {paramName}{defaultValue}");
            }
        }
    }
    
    private static string ToCamelCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        // Handle snake_case and kebab-case
        string[] parts = text.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            return parts[0].ToLower() + string.Join("", parts.Skip(1).Select(p => 
                char.ToUpper(p[0]) + p.Substring(1).ToLower()));
        }
        
        // Simple case - just lowercase first letter
        return char.ToLower(text[0]) + text.Substring(1);
    }
    
    private static string EscapeParameterName(string paramName, HashSet<string> usedNames)
    {
        // Escape C# keywords
        if (CSharpKeywords.Contains(paramName))
        {
            paramName = "@" + paramName;
        }
        
        // Handle name conflicts
        string originalName = paramName;
        int suffix = 1;
        while (usedNames.Contains(paramName))
        {
            paramName = originalName + (originalName.StartsWith("@") ? suffix.ToString() : "Option");
            suffix++;
        }
        
        usedNames.Add(paramName);
        return paramName;
    }
    
    private static string EscapeString(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? "";
            
        // Replace backslashes first, then quotes
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
    
    private static void GenerateMethodBody(StringBuilder sb, List<string> commandPath, OpenCliCommand? command, string? toolTitle, Dictionary<string, string> argumentParameterMap, Dictionary<string, string> optionParameterMap, IReadOnlyList<OpenCliOption>? globalOptions)
    {
        // Build arguments list
        sb.AppendLine("            var args = new List<string>();");
        
        // Add all commands in the path
        if (commandPath.Any())
        {
            sb.AppendLine();
            foreach (string? cmd in commandPath)
            {
                sb.AppendLine($"            args.Add(\"{cmd}\");");
            }
        }
        
        sb.AppendLine();
        
        // Handle global options first
        if (globalOptions != null)
        {
            foreach (OpenCliOption? option in globalOptions)
            {
                string? paramName = optionParameterMap.TryGetValue(option.Name ?? "option", out string? escaped) ? escaped : ToCamelCase(option.Name ?? "option");
                
                if (option.Arguments?.Any() == true)
                {
                    // Option with value
                    sb.AppendLine($"            if (!string.IsNullOrEmpty({paramName}))");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                args.Add(\"--{option.Name}\");");
                    sb.AppendLine($"                args.Add({paramName});");
                    sb.AppendLine("            }");
                }
                else
                {
                    // Boolean flag
                    sb.AppendLine($"            if ({paramName})");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                args.Add(\"--{option.Name}\");");
                    sb.AppendLine("            }");
                }
                sb.AppendLine();
            }
        }
        
        // Handle command-specific options
        if (command?.Options != null)
        {
            foreach (OpenCliOption? option in command.Options)
            {
                string? paramName = optionParameterMap.TryGetValue(option.Name ?? "option", out string? escaped) ? escaped : ToCamelCase(option.Name ?? "option");
                
                if (option.Arguments?.Any() == true)
                {
                    // Option with value
                    sb.AppendLine($"            if (!string.IsNullOrEmpty({paramName}))");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                args.Add(\"--{option.Name}\");");
                    sb.AppendLine($"                args.Add({paramName});");
                    sb.AppendLine("            }");
                }
                else
                {
                    // Boolean flag
                    sb.AppendLine($"            if ({paramName})");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                args.Add(\"--{option.Name}\");");
                    sb.AppendLine("            }");
                }
                sb.AppendLine();
            }
        }
        
        // Handle arguments in order
        if (command?.Arguments != null)
        {
            IOrderedEnumerable<OpenCliArgument> orderedArgs = command.Arguments.OrderBy(a => a.Ordinal);
            foreach (OpenCliArgument? arg in orderedArgs)
            {
                string? paramName = argumentParameterMap.TryGetValue(arg.Name ?? "arg", out string? escaped) ? escaped : ToCamelCase(arg.Name ?? "arg");
                
                if (arg.Required)
                {
                    sb.AppendLine($"            args.Add({paramName});");
                }
                else
                {
                    sb.AppendLine($"            if (!string.IsNullOrEmpty({paramName}))");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                args.Add({paramName});");
                    sb.AppendLine("            }");
                }
            }
            
            if (command.Arguments.Any())
            {
                sb.AppendLine();
            }
        }
        
        // Execute the command
        string cliCommand = GetCliCommandName(toolTitle);
        sb.AppendLine($"            return await cliExecutor.ExecuteAsync(\"{cliCommand}\", args, cancellationToken);");
    }
    
    private static string GetCliCommandName(string? toolTitle)
    {
        if (string.IsNullOrWhiteSpace(toolTitle))
            return "cli";
            
        // Remove "Tool" suffix and convert to lowercase
        string name = toolTitle!.Trim();
        if (name.EndsWith(" Tool", StringComparison.OrdinalIgnoreCase))
            name = name.Substring(0, name.Length - 5);
            
        return name.ToLower();
    }
    
    private static string NormalizeFilePath(string filePath, INamedTypeSymbol classSymbol)
    {
        // If already absolute, return as-is
        if (System.IO.Path.IsPathRooted(filePath))
        {
            return System.IO.Path.GetFullPath(filePath);
        }
        
        // Get the source file location
        SyntaxReference? syntaxReference = classSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference == null)
            return filePath;
            
        string? sourceFilePath = syntaxReference.SyntaxTree.FilePath;
        if (string.IsNullOrEmpty(sourceFilePath))
            return filePath;
            
        // Resolve relative to source file
        string? sourceDirectory = System.IO.Path.GetDirectoryName(sourceFilePath);
        if (string.IsNullOrEmpty(sourceDirectory))
            return filePath;
            
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(sourceDirectory, filePath));
    }
    
    private static bool PathsMatch(string path1, string path2)
    {
        try
        {
            // Normalize both paths to compare them accurately
            string normalized1 = System.IO.Path.GetFullPath(path1).Replace('\\', '/');
            string normalized2 = System.IO.Path.GetFullPath(path2).Replace('\\', '/');
            
            // Use case-insensitive comparison for cross-platform compatibility
            return string.Equals(normalized1, normalized2, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // If path normalization fails, try simple string comparison as fallback
            return string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase);
        }
    }
    
    private static string GeneratePartialClass(INamedTypeSymbol classSymbol, OpenCliSpec spec)
    {
        StringBuilder sb = new();
        
        // Add the header
        sb.AppendLine("""
            // <auto-generated/>
            #nullable enable
            using ModelContextProtocol.Server;
            using OpenCliToMcp.Core;
            using System.Collections.Generic;
            using System.ComponentModel;
            using System.Threading;
            using System.Threading.Tasks;
            """);
        
        // Add namespace if needed
        if (!classSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            sb.AppendLine();
            sb.AppendLine($"namespace {classSymbol.ContainingNamespace.ToDisplayString()}");
            sb.AppendLine("{");
        }
        
        // Generate partial class
        string indent = classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : "    ";
        sb.AppendLine($"{indent}[McpServerToolType]");
        sb.AppendLine($"{indent}public partial class {classSymbol.Name}");
        sb.AppendLine($"{indent}{{");
        
        if (spec.Commands != null)
        {
            foreach (KeyValuePair<string, OpenCliCommand> cmd in spec.Commands)
            {
                GenerateInstanceMethod(sb, cmd.Key, cmd.Value, spec.Info?.Title, ImmutableList<string>.Empty, spec.Options, indent + "    ");
            }
        }
        
        sb.AppendLine($"{indent}}}");
        
        if (!classSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            sb.AppendLine("}");
        }
        
        return sb.ToString();
    }
    
    private static void GenerateInstanceMethod(StringBuilder sb, string commandName, OpenCliCommand? command, string? toolTitle, IReadOnlyList<string> parentCommands, IReadOnlyList<OpenCliOption>? globalOptions, string indent)
    {
        if (command == null) return;

        List<string> commandPath = parentCommands.Concat([commandName]).ToList();
        string methodName = string.Join("", commandPath.Select(c => GetMethodName(c)));
        
        // Generate XML documentation
        string xmlDoc = GenerateXmlDocumentation(command, indent);
        sb.Append(xmlDoc);
        sb.AppendLine($"{indent}[McpServerTool]");
        if (!string.IsNullOrEmpty(command.Description))
        {
            sb.AppendLine($"{indent}[Description(\"{EscapeString(command.Description!)}\")]");
        }
        
        // Start method signature - note: no ICliExecutor parameter for instance methods
        sb.Append($"{indent}public async Task<string> {methodName}Async(");
        
        // Generate parameters using the helper method (no ICliExecutor for instance methods)
        var (parameters, argumentParameterMap, optionParameterMap) = GenerateParameterLists(command, globalOptions, includeCliExecutor: false);
        
        // Join parameters with proper formatting
        if (parameters.Count > 0)
        {
            sb.AppendLine();
            for (int i = 0; i < parameters.Count; i++)
            {
                sb.Append($"{indent}    {parameters[i]}");
                if (i < parameters.Count - 1)
                    sb.AppendLine(",");
            }
        }
        
        sb.AppendLine(")");
        sb.AppendLine($"{indent}{{");
        
        // Generate method body - using this.cliExecutor instead of parameter
        GenerateInstanceMethodBody(sb, commandPath, command, toolTitle, argumentParameterMap, optionParameterMap, globalOptions, indent + "    ");
        
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
        
        // Generate methods for nested commands
        if (command.Commands != null)
        {
            foreach (KeyValuePair<string, OpenCliCommand> nestedCmd in command.Commands)
            {
                GenerateInstanceMethod(sb, nestedCmd.Key, nestedCmd.Value, toolTitle, commandPath, globalOptions, indent);
            }
        }
    }
    
    private static void GenerateInstanceMethodBody(StringBuilder sb, List<string> commandPath, OpenCliCommand? command, string? toolTitle, Dictionary<string, string> argumentParameterMap, Dictionary<string, string> optionParameterMap, IReadOnlyList<OpenCliOption>? globalOptions, string indent)
    {
        // Build arguments list
        sb.AppendLine($"{indent}var args = new List<string>();");
        
        // Add all commands in the path
        foreach (string? cmd in commandPath)
        {
            sb.AppendLine($"{indent}args.Add(\"{cmd}\");");
        }
        
        sb.AppendLine();
        
        // Handle global options first
        if (globalOptions != null)
        {
            foreach (OpenCliOption? option in globalOptions)
            {
                string? paramName = optionParameterMap.TryGetValue(option.Name ?? "option", out string? escaped) ? escaped : ToCamelCase(option.Name ?? "option");
                
                if (option.Arguments?.Any() == true)
                {
                    // Option with value
                    sb.AppendLine($"{indent}if (!string.IsNullOrEmpty({paramName}))");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    args.Add(\"--{option.Name}\");");
                    sb.AppendLine($"{indent}    args.Add({paramName});");
                    sb.AppendLine($"{indent}}}");
                }
                else
                {
                    // Boolean flag
                    sb.AppendLine($"{indent}if ({paramName})");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    args.Add(\"--{option.Name}\");");
                    sb.AppendLine($"{indent}}}");
                }
                sb.AppendLine();
            }
        }
        
        // Handle command-specific options
        if (command?.Options != null)
        {
            foreach (OpenCliOption? option in command.Options)
            {
                string? paramName = optionParameterMap.TryGetValue(option.Name ?? "option", out string? escaped) ? escaped : ToCamelCase(option.Name ?? "option");
                
                if (option.Arguments?.Any() == true)
                {
                    // Option with value
                    sb.AppendLine($"{indent}if (!string.IsNullOrEmpty({paramName}))");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    args.Add(\"--{option.Name}\");");
                    sb.AppendLine($"{indent}    args.Add({paramName});");
                    sb.AppendLine($"{indent}}}");
                }
                else
                {
                    // Boolean flag
                    sb.AppendLine($"{indent}if ({paramName})");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    args.Add(\"--{option.Name}\");");
                    sb.AppendLine($"{indent}}}");
                }
                sb.AppendLine();
            }
        }
        
        // Handle arguments in order
        if (command?.Arguments != null)
        {
            IOrderedEnumerable<OpenCliArgument> orderedArgs = command.Arguments.OrderBy(a => a.Ordinal);
            foreach (OpenCliArgument? arg in orderedArgs)
            {
                string? paramName = argumentParameterMap.TryGetValue(arg.Name ?? "arg", out string? escaped) ? escaped : ToCamelCase(arg.Name ?? "arg");
                
                if (arg.Required)
                {
                    sb.AppendLine($"{indent}args.Add({paramName});");
                }
                else
                {
                    sb.AppendLine($"{indent}if (!string.IsNullOrEmpty({paramName}))");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    args.Add({paramName});");
                    sb.AppendLine($"{indent}}}");
                }
            }
            
            if (command.Arguments.Any())
            {
                sb.AppendLine();
            }
        }
        
        // Execute the command using instance field
        string cliCommand = GetCliCommandName(toolTitle);
        sb.AppendLine($"{indent}return await this.cliExecutor.ExecuteAsync(\"{cliCommand}\", args, cancellationToken);");
    }
    
    private static string GenerateXmlDocumentation(OpenCliCommand command, string indent)
    {
        var sb = new StringBuilder();
        
        // Basic summary
        sb.AppendLine($$$"""
            {{{indent}}}/// <summary>
            {{{indent}}}/// {{{command.Description ?? "No description"}}}
            """);
        
        // Add exit codes if present
        if (command.ExitCodes?.Any() == true)
        {
            sb.AppendLine($"{indent}/// Exit codes:");
            foreach (var exitCode in command.ExitCodes)
            {
                sb.AppendLine($"{indent}/// - {exitCode.Code}: {exitCode.Description ?? "No description"}");
            }
        }
        
        // Add examples if present
        if (command.Examples?.Any() == true)
        {
            sb.AppendLine($"{indent}/// Examples:");
            foreach (var example in command.Examples)
            {
                sb.AppendLine($"{indent}/// - {example.Command}");
                if (!string.IsNullOrEmpty(example.Description))
                {
                    sb.AppendLine($"{indent}///   {example.Description}");
                }
            }
        }
        
        sb.AppendLine($"{indent}/// </summary>");
        
        return sb.ToString();
    }
}