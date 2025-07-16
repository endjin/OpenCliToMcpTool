using System;
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
public class OpenCliToMcpGeneratorTests
{
    [TestMethod]
    public void Generator_WithEmptyAdditionalFiles_GeneratesNothing()
    {
        // Arrange
        CSharpCompilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        
        // Act
        CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        
        // Assert
        diagnostics.ShouldBeEmpty();
        ImmutableArray<SyntaxTree> generatedTrees = driver.GetRunResult().GeneratedTrees;
        
        // Debug output to understand what files are generated
        Console.WriteLine($"Generated {generatedTrees.Length} files:");
        foreach (SyntaxTree tree in generatedTrees)
        {
            Console.WriteLine($"  - {tree.FilePath}");
        }
        
        generatedTrees.Length.ShouldBe(0);
    }
    
    [TestMethod]
    public void Generator_WithSimpleOpenCliJson_GeneratesCliExecutorInterface()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "test",
                                 "version": "1.0.0"
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
        // The generator now produces at least 1 file (ICliExecutor)
        generatedTrees.Length.ShouldBeGreaterThanOrEqualTo(1);
        
        string generatedSource = generatedTrees[0].GetText().ToString();
        generatedSource.ShouldContain("global using ICliExecutor = OpenCliToMcp.Core.ICliExecutor;");
    }
    
    [TestMethod]
    public void Generator_WithOpenCliCommand_GeneratesMcpToolClass()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Git Tool",
                                 "version": "1.0.0"
                               },
                               "commands": {
                                 "status": {
                                   "description": "Show the working tree status"
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
        // The generator now produces 3 files: ICliExecutor + GitToolMcp + partial class with attribute
        generatedTrees.Length.ShouldBeGreaterThanOrEqualTo(2);
        
        string mcpToolSource = generatedTrees.First(t => t.FilePath.Contains("GitToolMcp")).GetText().ToString();
        mcpToolSource.ShouldContain("public static class GitToolMcp");
        mcpToolSource.ShouldContain("[McpServerToolType]");
        mcpToolSource.ShouldContain("[McpServerTool]");
        mcpToolSource.ShouldContain("Show the working tree status");
    }
    
    [TestMethod]
    public void Generator_WithCommandArguments_GeneratesMethodParameters()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Git Tool",
                                 "version": "1.0.0"
                               },
                               "commands": {
                                 "checkout": {
                                   "description": "Switch branches or restore working tree files",
                                   "arguments": [
                                     {
                                       "name": "branch",
                                       "description": "Branch to checkout",
                                       "required": true,
                                       "ordinal": 0
                                     }
                                   ]
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
        mcpToolSource.ShouldContain("ICliExecutor cliExecutor");
        mcpToolSource.ShouldContain("string branch");
        mcpToolSource.ShouldContain("Description(\"Branch to checkout\")");
    }
    
    [TestMethod]
    public void Generator_WithCommandOptions_GeneratesOptionalParameters()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Git Tool",
                                 "version": "1.0.0"
                               },
                               "commands": {
                                 "commit": {
                                   "description": "Record changes to the repository",
                                   "options": [
                                     {
                                       "name": "message",
                                       "aliases": ["m"],
                                       "description": "Commit message",
                                       "arguments": [
                                         {
                                           "name": "text",
                                           "required": true
                                         }
                                       ]
                                     },
                                     {
                                       "name": "all",
                                       "aliases": ["a"],
                                       "description": "Stage all modified files"
                                     }
                                   ]
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
        mcpToolSource.ShouldContain("string? message = null");
        mcpToolSource.ShouldContain("bool all = false");
        mcpToolSource.ShouldContain("Description(\"Commit message\")");
        mcpToolSource.ShouldContain("Description(\"Stage all modified files\")");
    }
    
    [TestMethod]
    public void Generator_WithCommand_GeneratesMethodBodyWithCliExecution()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Git Tool",
                                 "version": "1.0.0"
                               },
                               "commands": {
                                 "add": {
                                   "description": "Add file contents to the index",
                                   "arguments": [
                                     {
                                       "name": "pathspec",
                                       "description": "Files to add",
                                       "required": true,
                                       "ordinal": 0
                                     }
                                   ],
                                   "options": [
                                     {
                                       "name": "all",
                                       "aliases": ["A"],
                                       "description": "Add all files"
                                     },
                                     {
                                       "name": "force",
                                       "aliases": ["f"],
                                       "description": "Allow adding ignored files"
                                     }
                                   ]
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
        
        // Should build args list
        mcpToolSource.ShouldContain("var args = new List<string>();");
        mcpToolSource.ShouldContain("args.Add(\"add\");");
        
        // Should handle options
        mcpToolSource.ShouldContain("if (all)");
        mcpToolSource.ShouldContain("args.Add(\"--all\");");
        
        // Should add arguments
        mcpToolSource.ShouldContain("args.Add(pathspec);");
        
        // Should execute CLI
        mcpToolSource.ShouldContain("return await cliExecutor.ExecuteAsync(\"git\", args, cancellationToken);");
    }
    
    [TestMethod]
    public void Generator_CompleteScenario_GeneratesFullyFunctionalMcpTool()
    {
        // Arrange - A more complete OpenCLI spec
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Docker",
                                 "version": "20.10.0",
                                 "description": "Container management tool"
                               },
                               "commands": {
                                 "run": {
                                   "description": "Run a command in a new container",
                                   "arguments": [
                                     {
                                       "name": "image",
                                       "description": "Docker image to run",
                                       "required": true,
                                       "ordinal": 0
                                     },
                                     {
                                       "name": "command",
                                       "description": "Command to run in container",
                                       "required": false,
                                       "ordinal": 1
                                     }
                                   ],
                                   "options": [
                                     {
                                       "name": "detach",
                                       "aliases": ["d"],
                                       "description": "Run container in background"
                                     },
                                     {
                                       "name": "name",
                                       "description": "Assign a name to the container",
                                       "arguments": [
                                         {
                                           "name": "container-name",
                                           "required": true
                                         }
                                       ]
                                     },
                                     {
                                       "name": "port",
                                       "aliases": ["p"],
                                       "description": "Publish container ports",
                                       "arguments": [
                                         {
                                           "name": "port-mapping",
                                           "required": true
                                         }
                                       ]
                                     }
                                   ]
                                 }
                               }
                             }
                             """;
        
        CSharpCompilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        InMemoryAdditionalText additionalText = new("docker.opencli.json", openCliJson);
        
        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        
        // Assert
        diagnostics.ShouldBeEmpty();
        ImmutableArray<SyntaxTree> generatedTrees = driver.GetRunResult().GeneratedTrees;
        // The generator now produces 3 files: ICliExecutor + DockerMcp + partial class with attribute
        generatedTrees.Length.ShouldBeGreaterThanOrEqualTo(2);
        
        // Check ICliExecutor interface
        string cliExecutorSource = generatedTrees.First(t => t.FilePath.Contains("ICliExecutor")).GetText().ToString();
        cliExecutorSource.ShouldContain("global using ICliExecutor = OpenCliToMcp.Core.ICliExecutor;");
        
        // Check Docker MCP tool
        string dockerMcpSource = generatedTrees.First(t => t.FilePath.Contains("DockerToolMcp")).GetText().ToString();
        
        // Class structure
        dockerMcpSource.ShouldContain("[McpServerToolType]");
        dockerMcpSource.ShouldContain("public static class DockerToolMcp");
        
        // Method signature
        dockerMcpSource.ShouldContain("[McpServerTool]");
        dockerMcpSource.ShouldContain("[Description(\"Run a command in a new container\")]");
        dockerMcpSource.ShouldContain("public static async Task<string> RunAsync(");
        
        // Parameters
        dockerMcpSource.ShouldContain("ICliExecutor cliExecutor");
        dockerMcpSource.ShouldContain("[Description(\"Docker image to run\")] string image");
        dockerMcpSource.ShouldContain("[Description(\"Command to run in container\")] string? command = null");
        dockerMcpSource.ShouldContain("[Description(\"Run container in background\")] bool detach = false");
        dockerMcpSource.ShouldContain("[Description(\"Assign a name to the container\")] string? name = null");
        dockerMcpSource.ShouldContain("[Description(\"Publish container ports\")] string? port = null");
        dockerMcpSource.ShouldContain("CancellationToken cancellationToken = default");
        
        // Method body
        dockerMcpSource.ShouldContain("var args = new List<string>();");
        dockerMcpSource.ShouldContain("args.Add(\"run\");");
        dockerMcpSource.ShouldContain("if (detach)");
        dockerMcpSource.ShouldContain("args.Add(\"--detach\");");
        dockerMcpSource.ShouldContain("if (!string.IsNullOrEmpty(name))");
        dockerMcpSource.ShouldContain("args.Add(\"--name\");");
        dockerMcpSource.ShouldContain("args.Add(name);");
        dockerMcpSource.ShouldContain("args.Add(image);");
        dockerMcpSource.ShouldContain("return await cliExecutor.ExecuteAsync(\"docker\", args, cancellationToken);");
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