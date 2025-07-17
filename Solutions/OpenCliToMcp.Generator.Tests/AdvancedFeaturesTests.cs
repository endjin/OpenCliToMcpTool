using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Linq;
using System.Text;
using System.Threading;

namespace OpenCliToMcp.Generator.Tests;

[TestClass]
public class AdvancedFeaturesTests
{
    [TestMethod]
    public void Generator_WithExitCodes_IncludesInDocumentation()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Tool",
                                 "version": "1.0.0"
                               },
                               "commands": {
                                 "validate": {
                                   "description": "Validate configuration",
                                   "exitCodes": [
                                     {
                                       "code": 0,
                                       "description": "Validation successful"
                                     },
                                     {
                                       "code": 1,
                                       "description": "Validation failed"
                                     },
                                     {
                                       "code": 2,
                                       "description": "Invalid configuration file"
                                     }
                                   ]
                                 }
                               }
                             }
                             """;
        
        CSharpCompilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        InMemoryAdditionalText additionalText = new("tool.opencli.json", openCliJson);
        
        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        
        // Assert
        diagnostics.ShouldBeEmpty();
        ImmutableArray<SyntaxTree> generatedTrees = driver.GetRunResult().GeneratedTrees;
        
        string mcpToolSource = generatedTrees.First(t => t.FilePath.Contains("ToolMcp")).GetText().ToString();
        
        // Should include exit codes in XML documentation
        mcpToolSource.ShouldContain("/// Exit codes:");
        mcpToolSource.ShouldContain("/// - 0: Validation successful");
        mcpToolSource.ShouldContain("/// - 1: Validation failed");
        mcpToolSource.ShouldContain("/// - 2: Invalid configuration file");
    }
    
    [TestMethod]
    public void Generator_WithExamples_IncludesInDocumentation()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Tool",
                                 "version": "1.0.0"
                               },
                               "commands": {
                                 "process": {
                                   "description": "Process a file",
                                   "arguments": [
                                     {
                                       "name": "file",
                                       "required": true
                                     }
                                   ],
                                   "options": [
                                     {
                                       "name": "format",
                                       "arguments": [
                                         {
                                           "name": "type",
                                           "required": true
                                         }
                                       ]
                                     }
                                   ],
                                   "examples": [
                                     {
                                       "command": "tool process data.txt",
                                       "description": "Process data.txt with default settings"
                                     },
                                     {
                                       "command": "tool process --format json data.txt",
                                       "description": "Process data.txt and output as JSON"
                                     }
                                   ]
                                 }
                               }
                             }
                             """;
        
        CSharpCompilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        InMemoryAdditionalText additionalText = new("tool.opencli.json", openCliJson);
        
        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        
        // Assert
        diagnostics.ShouldBeEmpty();
        ImmutableArray<SyntaxTree> generatedTrees = driver.GetRunResult().GeneratedTrees;
        
        string mcpToolSource = generatedTrees.First(t => t.FilePath.Contains("ToolMcp")).GetText().ToString();
        
        // Should include examples in XML documentation
        mcpToolSource.ShouldContain("/// Examples:");
        mcpToolSource.ShouldContain("/// - tool process data.txt");
        mcpToolSource.ShouldContain("///   Process data.txt with default settings");
        mcpToolSource.ShouldContain("/// - tool process --format json data.txt");
        mcpToolSource.ShouldContain("///   Process data.txt and output as JSON");
    }
    
    [TestMethod]
    public void Generator_WithGlobalOptions_IncludesInAllCommands()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Tool",
                                 "version": "1.0.0"
                               },
                               "options": [
                                 {
                                   "name": "verbose",
                                   "aliases": ["v"],
                                   "description": "Enable verbose output"
                                 },
                                 {
                                   "name": "config",
                                   "description": "Configuration file path",
                                   "arguments": [
                                     {
                                       "name": "path",
                                       "required": true
                                     }
                                   ]
                                 }
                               ],
                               "commands": {
                                 "run": {
                                   "description": "Run the tool"
                                 },
                                 "test": {
                                   "description": "Test the tool"
                                 }
                               }
                             }
                             """;
        
        CSharpCompilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        InMemoryAdditionalText additionalText = new("tool.opencli.json", openCliJson);
        
        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        
        // Assert
        diagnostics.ShouldBeEmpty();
        ImmutableArray<SyntaxTree> generatedTrees = driver.GetRunResult().GeneratedTrees;
        
        string mcpToolSource = generatedTrees.First(t => t.FilePath.Contains("ToolMcp")).GetText().ToString();
        
        // Both commands should have global options
        mcpToolSource.ShouldContain("bool verbose = false");
        mcpToolSource.ShouldContain("string? config = null");
    }
    
    private static CSharpCompilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("TestAssembly",
            syntaxTrees: string.IsNullOrEmpty(source) ? [] : [CSharpSyntaxTree.ParseText(source)],
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
    
    private class InMemoryAdditionalText : AdditionalText
    {
        private readonly string _path;
        private readonly string _text;
        
        public InMemoryAdditionalText(string path, string text)
        {
            _path = path;
            _text = text;
        }
        
        public override string Path => _path;
        
        public override SourceText GetText(CancellationToken cancellationToken = default)
            => SourceText.From(_text, Encoding.UTF8);
    }
}