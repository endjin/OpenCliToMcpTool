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
public class ValidationAndErrorHandlingTests
{
    [TestMethod]
    public void Generator_WithInvalidJson_GeneratesNothing()
    {
        // Arrange
        string invalidJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Test",
                                 // This is invalid JSON
                             }
                             """;
        
        CSharpCompilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        InMemoryAdditionalText additionalText = new("test.opencli.json", invalidJson);
        
        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        
        // Assert
        // The generator should produce a warning for invalid JSON
        diagnostics.Length.ShouldBe(1);
        diagnostics[0].Id.ShouldBe("OCMCP001");
        diagnostics[0].GetMessage().ShouldContain("invalid JSON");
        ImmutableArray<SyntaxTree> generatedTrees = driver.GetRunResult().GeneratedTrees;
        generatedTrees.Length.ShouldBe(0); // No files generated for invalid JSON
    }
    
    [TestMethod]
    public void Generator_WithMissingRequiredFields_GeneratesNothing()
    {
        // Arrange
        string jsonMissingInfo = """
                                 {
                                   "opencli": "0.1"
                                 }
                                 """;
        
        CSharpCompilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        InMemoryAdditionalText additionalText = new("test.opencli.json", jsonMissingInfo);
        
        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        
        // Assert
        // The generator should produce a warning for missing required fields
        diagnostics.Length.ShouldBe(1);
        diagnostics[0].Id.ShouldBe("OCMCP001");
        diagnostics[0].GetMessage().ShouldContain("invalid JSON");
        ImmutableArray<SyntaxTree> generatedTrees = driver.GetRunResult().GeneratedTrees;
        generatedTrees.Length.ShouldBe(0); // No files generated when info is missing
    }
    
    [TestMethod]
    public void Generator_WithNoCommands_GeneratesOnlyInterface()
    {
        // Arrange
        string jsonNoCommands = """
                                {
                                  "opencli": "0.1",
                                  "info": {
                                    "title": "Empty Tool",
                                    "version": "1.0.0"
                                  }
                                }
                                """;
        
        CSharpCompilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        InMemoryAdditionalText additionalText = new("empty.opencli.json", jsonNoCommands);
        
        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        
        // Assert
        diagnostics.ShouldBeEmpty();
        ImmutableArray<SyntaxTree> generatedTrees = driver.GetRunResult().GeneratedTrees;
        // When no commands exist, we still generate ICliExecutor and possibly a static class
        generatedTrees.Length.ShouldBeGreaterThanOrEqualTo(1);
        generatedTrees.Any(t => t.FilePath.Contains("ICliExecutor")).ShouldBeTrue();
    }
    
    [TestMethod]
    public void Generator_WithDuplicateParameterNames_HandlesGracefully()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Test",
                                 "version": "1.0.0"
                               },
                               "commands": {
                                 "test": {
                                   "description": "Test command",
                                   "arguments": [
                                     {
                                       "name": "name",
                                       "required": true,
                                       "ordinal": 0
                                     }
                                   ],
                                   "options": [
                                     {
                                       "name": "name",
                                       "description": "This conflicts with the argument name"
                                     }
                                   ]
                                 }
                               }
                             }
                             """;
        
        CSharpCompilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        InMemoryAdditionalText additionalText = new("test.opencli.json", openCliJson);
        
        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        
        // Assert
        diagnostics.ShouldBeEmpty();
        ImmutableArray<SyntaxTree> generatedTrees = driver.GetRunResult().GeneratedTrees;
        
        string mcpToolSource = generatedTrees.First(t => t.FilePath.Contains("TestToolMcp")).GetText().ToString();
        
        // Should rename conflicting parameters
        mcpToolSource.ShouldContain("string name");
        mcpToolSource.ShouldContain("bool nameOption = false");
    }
    
    [TestMethod]
    public void Generator_WithReservedKeywordAsParameterName_EscapesName()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Test",
                                 "version": "1.0.0"
                               },
                               "commands": {
                                 "test": {
                                   "description": "Test command",
                                   "arguments": [
                                     {
                                       "name": "class",
                                       "description": "Class name",
                                       "required": true
                                     }
                                   ],
                                   "options": [
                                     {
                                       "name": "namespace",
                                       "description": "Namespace"
                                     }
                                   ]
                                 }
                               }
                             }
                             """;
        
        CSharpCompilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        InMemoryAdditionalText additionalText = new("test.opencli.json", openCliJson);
        
        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        
        // Assert
        diagnostics.ShouldBeEmpty();
        ImmutableArray<SyntaxTree> generatedTrees = driver.GetRunResult().GeneratedTrees;
        
        string mcpToolSource = generatedTrees.First(t => t.FilePath.Contains("TestToolMcp")).GetText().ToString();
        
        // Should escape C# keywords
        mcpToolSource.ShouldContain("string @class");
        mcpToolSource.ShouldContain("bool @namespace = false");
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