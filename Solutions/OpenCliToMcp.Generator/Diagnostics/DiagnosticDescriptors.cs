using Microsoft.CodeAnalysis;

namespace OpenCliToMcp.Generator.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string Category = "OpenCliToMcp";

    public static readonly DiagnosticDescriptor InvalidJsonFormat = new(
        id: "OCMCP001",
        title: "Invalid OpenCLI JSON format",
        messageFormat: "The OpenCLI JSON file '{0}' contains invalid JSON",
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The OpenCLI JSON file could not be parsed. Ensure it contains valid JSON.");

    public static readonly DiagnosticDescriptor FileNotFound = new(
        id: "OCMCP002",
        title: "OpenCLI file not found",
        messageFormat: "The OpenCLI specification file '{0}' referenced in OpenCliToolAttribute was not found in AdditionalFiles{1}",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The file path specified in OpenCliToolAttribute must reference a file included as an AdditionalFile in the project.");

    public static readonly DiagnosticDescriptor ClassNotPartial = new(
        id: "OCMCP003",
        title: "Class must be partial",
        messageFormat: "The class '{0}' with OpenCliToolAttribute must be declared as partial",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Classes marked with OpenCliToolAttribute must be partial to allow source generation.");

    public static readonly DiagnosticDescriptor ClassIsStatic = new(
        id: "OCMCP004",
        title: "Class cannot be static",
        messageFormat: "The class '{0}' with OpenCliToolAttribute cannot be static",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Classes marked with OpenCliToolAttribute cannot be static as they need instance members.");

    public static readonly DiagnosticDescriptor InvalidCommandName = new(
        id: "OCMCP005",
        title: "Invalid command name",
        messageFormat: "The command name '{0}' in file '{1}' is not a valid C# identifier and cannot be used to generate method names",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Command names must be valid C# identifiers to be used as method names in the generated code.");

    public static readonly DiagnosticDescriptor DuplicateCommandName = new(
        id: "OCMCP006",
        title: "Duplicate command name",
        messageFormat: "The command name '{0}' appears multiple times at the same level in file '{1}'",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Command names must be unique at each level to avoid generating conflicting methods.");
}