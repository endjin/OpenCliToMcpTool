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
public class NestedCommandTests
{
    [TestMethod]
    public void Generator_WithNestedCommand_GeneratesMultipleMethods()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Git",
                                 "version": "2.40.0"
                               },
                               "commands": {
                                 "remote": {
                                   "description": "Manage set of tracked repositories",
                                   "commands": {
                                     "add": {
                                       "description": "Add a remote",
                                       "arguments": [
                                         {
                                           "name": "name",
                                           "description": "Remote name",
                                           "required": true,
                                           "ordinal": 0
                                         },
                                         {
                                           "name": "url",
                                           "description": "Remote URL",
                                           "required": true,
                                           "ordinal": 1
                                         }
                                       ]
                                     },
                                     "remove": {
                                       "description": "Remove a remote",
                                       "arguments": [
                                         {
                                           "name": "name",
                                           "description": "Remote name",
                                           "required": true,
                                           "ordinal": 0
                                         }
                                       ]
                                     }
                                   }
                                 }
                               }
                             }
                             """;
        
        CSharpCompilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        InMemoryAdditionalText additionalText = new("git.opencli.json", openCliJson);
        
        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        
        // Assert
        diagnostics.ShouldBeEmpty();
        ImmutableArray<SyntaxTree> generatedTrees = driver.GetRunResult().GeneratedTrees;
        
        string mcpToolSource = generatedTrees.First(t => t.FilePath.Contains("GitToolMcp")).GetText().ToString();
        
        // Should generate method for parent command
        mcpToolSource.ShouldContain("public static async Task<string> RemoteAsync(");
        
        // Should generate methods for nested commands
        mcpToolSource.ShouldContain("public static async Task<string> RemoteAddAsync(");
        mcpToolSource.ShouldContain("public static async Task<string> RemoteRemoveAsync(");
        
        // Check remote add parameters
        mcpToolSource.ShouldContain("string name");
        mcpToolSource.ShouldContain("string url");
        
        // Check command line construction
        mcpToolSource.ShouldContain("args.Add(\"remote\");");
        mcpToolSource.ShouldContain("args.Add(\"add\");");
    }
    
    [TestMethod]
    public void Generator_WithDeeplyNestedCommands_GeneratesCorrectMethodNames()
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
                                 "level1": {
                                   "description": "Level 1 command",
                                   "commands": {
                                     "level2": {
                                       "description": "Level 2 command",
                                       "commands": {
                                         "level3": {
                                           "description": "Level 3 command"
                                         }
                                       }
                                     }
                                   }
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
        
        // Check method names
        mcpToolSource.ShouldContain("public static async Task<string> Level1Async(");
        mcpToolSource.ShouldContain("public static async Task<string> Level1Level2Async(");
        mcpToolSource.ShouldContain("public static async Task<string> Level1Level2Level3Async(");
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