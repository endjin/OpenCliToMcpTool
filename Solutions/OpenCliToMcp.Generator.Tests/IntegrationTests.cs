using System.Collections.Generic;
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
public class IntegrationTests
{
    [TestMethod]
    public void Generator_WithCompleteTaskManagerSpec_GeneratesExpectedCode()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Task Manager",
                                 "version": "1.0.0"
                               },
                               "options": [
                                 {
                                   "name": "verbose",
                                   "aliases": ["v"],
                                   "description": "Enable verbose output"
                                 }
                               ],
                               "commands": {
                                 "list": {
                                   "description": "List all tasks",
                                   "options": [
                                     {
                                       "name": "status",
                                       "description": "Filter by status",
                                       "arguments": [
                                         {
                                           "name": "value",
                                           "required": true,
                                           "acceptedValues": ["pending", "completed"]
                                         }
                                       ]
                                     }
                                   ]
                                 },
                                 "task": {
                                   "description": "Manage tasks",
                                   "commands": {
                                     "add": {
                                       "description": "Add a new task",
                                       "arguments": [
                                         {
                                           "name": "title",
                                           "required": true,
                                           "ordinal": 0
                                         }
                                       ],
                                       "options": [
                                         {
                                           "name": "priority",
                                           "arguments": [
                                             {
                                               "name": "level",
                                               "acceptedValues": ["low", "medium", "high"]
                                             }
                                           ]
                                         }
                                       ]
                                     }
                                   }
                                 }
                               }
                             }
                             """;

        // Act
        GeneratorDriverRunResult result = GenerateCode(openCliJson, "taskmanager.opencli.json");

        // Assert
        result.ShouldNotBeNull();
        result.GeneratedTrees.Length.ShouldBe(2); // ICliExecutor + TaskManagerToolMcp

        // Check ICliExecutor interface
        SyntaxTree cliExecutorTree = result.GeneratedTrees.First(t => t.FilePath.Contains("ICliExecutor"));
        string cliExecutorSource = cliExecutorTree.GetText().ToString();
        cliExecutorSource.ShouldContain("global using ICliExecutor = OpenCliToMcp.Core.ICliExecutor;");

        // Check generated MCP tool class
        SyntaxTree mcpToolTree = result.GeneratedTrees.First(t => t.FilePath.Contains("TaskManagerToolMcp"));
        string mcpToolSource = mcpToolTree.GetText().ToString();
        
        // Verify class structure
        mcpToolSource.ShouldContain("[McpServerToolType]");
        mcpToolSource.ShouldContain("public static class TaskManagerToolMcp");
        
        // Verify global options are included
        mcpToolSource.ShouldContain("bool verbose = false");
        
        // Verify list command
        mcpToolSource.ShouldContain("public static async Task<string> ListAsync");
        mcpToolSource.ShouldContain("[Description(\"List all tasks\")]");
        mcpToolSource.ShouldContain("[Description(\"Filter by status\")] string? status = null");
        
        // Verify nested task add command
        mcpToolSource.ShouldContain("public static async Task<string> TaskAddAsync");
        mcpToolSource.ShouldContain("[Description(\"Add a new task\")]");
        mcpToolSource.ShouldContain("string title"); // No description attribute since no description was provided
        
        // Verify command execution logic
        mcpToolSource.ShouldContain("args.Add(\"list\")");
        mcpToolSource.ShouldContain("if (verbose)");
        mcpToolSource.ShouldContain("args.Add(\"--verbose\")");
    }

    [TestMethod]
    public void Generator_WithGitExample_HandlesComplexScenarios()
    {
        // Arrange
        string gitSpec = """
                         {
                           "opencli": "0.1",
                           "info": {
                             "title": "Git",
                             "version": "2.40.0"
                           },
                           "commands": {
                             "commit": {
                               "description": "Record changes",
                               "options": [
                                 {
                                   "name": "message",
                                   "aliases": ["m"],
                                   "description": "Commit message",
                                   "arguments": [
                                     {
                                       "name": "msg",
                                       "required": true
                                     }
                                   ]
                                 },
                                 {
                                   "name": "all",
                                   "aliases": ["a"],
                                   "description": "Stage all changes"
                                 }
                               ]
                             },
                             "remote": {
                               "description": "Manage remotes",
                               "commands": {
                                 "add": {
                                   "description": "Add a remote",
                                   "arguments": [
                                     {
                                       "name": "name",
                                       "required": true,
                                       "ordinal": 0
                                     },
                                     {
                                       "name": "url",
                                       "required": true,
                                       "ordinal": 1
                                     }
                                   ]
                                 }
                               }
                             }
                           }
                         }
                         """;

        // Act
        GeneratorDriverRunResult result = GenerateCode(gitSpec, "git.opencli.json");

        // Assert
        SyntaxTree mcpToolTree = result.GeneratedTrees.First(t => t.FilePath.Contains("GitToolMcp"));
        string source = mcpToolTree.GetText().ToString();

        // Verify commit command with options
        source.ShouldContain("public static async Task<string> CommitAsync");
        source.ShouldContain("[Description(\"Commit message\")] string? message = null");
        source.ShouldContain("[Description(\"Stage all changes\")] bool all = false");

        // Verify nested remote add command
        source.ShouldContain("public static async Task<string> RemoteAddAsync");
        source.ShouldContain("string name"); // No description attribute since no description was provided
        source.ShouldContain("string url"); // No description attribute since no description was provided
        
        // Verify argument ordering
        source.ShouldContain("args.Add(name);");
        int nameIndex = source.IndexOf("args.Add(name);");
        int urlIndex = source.IndexOf("args.Add(url);");
        nameIndex.ShouldBeLessThan(urlIndex); // name comes before url
    }

    [TestMethod]
    public void Generator_WithRealWorldScenarios_ProducesValidCode()
    {
        // Test various real-world CLI patterns
        (string, string)[] scenarios =
        [
            // Docker-like nested commands
            ("""
            {
              "opencli": "0.1",
              "info": { "title": "Docker Tool" },
              "commands": {
                "container": {
                  "description": "Container management",
                  "commands": {
                    "run": {
                      "description": "Run a container",
                      "arguments": [{ "name": "image", "required": true }],
                      "options": [
                        { "name": "detach", "aliases": ["d"] },
                        { "name": "port", "aliases": ["p"], "arguments": [{ "name": "mapping" }] }
                      ]
                    }
                  }
                }
              }
            }
            """, "ContainerRunAsync"),

            // AWS CLI style with global options
            ("""
            {
              "opencli": "0.1",
              "info": { "title": "AWS Tool" },
              "options": [
                { "name": "profile", "arguments": [{ "name": "name" }] },
                { "name": "region", "arguments": [{ "name": "name" }] }
              ],
              "commands": {
                "s3": {
                  "description": "S3 operations",
                  "commands": {
                    "cp": {
                      "description": "Copy files",
                      "arguments": [
                        { "name": "source", "required": true },
                        { "name": "dest", "required": true }
                      ]
                    }
                  }
                }
              }
            }
            """, "S3CpAsync"),

            // Kubectl style with accepted values
            ("""
            {
              "opencli": "0.1",
              "info": { "title": "Kubectl Tool" },
              "commands": {
                "get": {
                  "description": "Get resources",
                  "arguments": [
                    {
                      "name": "resource",
                      "required": true,
                      "acceptedValues": ["pods", "services", "deployments"]
                    }
                  ],
                  "options": [
                    {
                      "name": "output",
                      "aliases": ["o"],
                      "arguments": [
                        {
                          "name": "format",
                          "acceptedValues": ["json", "yaml", "wide"]
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """, "GetAsync")
        ];

        foreach ((string spec, string expectedMethod) in scenarios)
        {
            // Act
            GeneratorDriverRunResult result = GenerateCode(spec, "test.opencli.json");

            // Assert
            result.ShouldNotBeNull();
            
            // Debug: print all generated files
            if (result.GeneratedTrees.Length == 0)
            {
                System.Console.WriteLine("No files were generated!");
            }
            else
            {
                System.Console.WriteLine($"Generated {result.GeneratedTrees.Length} files:");
                foreach (SyntaxTree tree in result.GeneratedTrees)
                {
                    System.Console.WriteLine($"  - {tree.FilePath}");
                }
            }
            
            SyntaxTree? mcpTool = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("Mcp.g.cs"));
            mcpTool.ShouldNotBeNull();
            
            string source = mcpTool.GetText().ToString();
            
            // Debug: print the source if test fails
            if (!source.Contains(expectedMethod))
            {
                System.Console.WriteLine($"Expected method '{expectedMethod}' not found in generated source:");
                System.Console.WriteLine(source);
            }
            
            source.ShouldContain(expectedMethod);
            
            // Verify it compiles
            Compilation compilation = CreateCompilationWithGeneratedCode(result);
            IEnumerable<Diagnostic> diagnostics = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error);
            diagnostics.ShouldBeEmpty();
        }
    }

    [TestMethod]
    public void Generator_EndToEnd_ProducesWorkingMcpServer()
    {
        // Arrange - Create a complete specification
        string spec = """
                      {
                        "opencli": "0.1",
                        "info": {
                          "title": "Demo Tool",
                          "version": "1.0.0",
                          "description": "Demo CLI tool for testing"
                        },
                        "commands": {
                          "hello": {
                            "description": "Say hello",
                            "arguments": [
                              {
                                "name": "name",
                                "description": "Name to greet",
                                "required": true
                              }
                            ],
                            "options": [
                              {
                                "name": "uppercase",
                                "description": "Output in uppercase"
                              },
                              {
                                "name": "times",
                                "description": "Repeat greeting",
                                "arguments": [{ "name": "count", "required": true }]
                              }
                            ],
                            "exitCodes": [
                              { "code": 0, "description": "Success" },
                              { "code": 1, "description": "Invalid input" }
                            ],
                            "examples": [
                              { "command": "hello World", "description": "Basic greeting" },
                              { "command": "hello World --uppercase", "description": "Uppercase greeting" }
                            ]
                          }
                        }
                      }
                      """;

        // Act
        GeneratorDriverRunResult result = GenerateCode(spec, "demo.opencli.json");
        SyntaxTree mcpToolTree = result.GeneratedTrees.First(t => t.FilePath.Contains("DemoToolMcp"));
        string source = mcpToolTree.GetText().ToString();

        // Assert - Verify all features are properly generated
        
        // Class structure
        source.ShouldContain("[McpServerToolType]");
        source.ShouldContain("public static class DemoToolMcp");
        
        // Method signature
        source.ShouldContain("[McpServerTool]");
        source.ShouldContain("[Description(\"Say hello\")]");
        source.ShouldContain("public static async Task<string> HelloAsync");
        
        // Parameters
        source.ShouldContain("[Description(\"Name to greet\")] string name");
        source.ShouldContain("[Description(\"Output in uppercase\")] bool uppercase = false");
        source.ShouldContain("[Description(\"Repeat greeting\")] string? times = null");
        source.ShouldContain("CancellationToken cancellationToken = default");
        
        // Method body
        source.ShouldContain("args.Add(\"hello\");");
        source.ShouldContain("args.Add(name);");
        source.ShouldContain("if (uppercase)");
        source.ShouldContain("args.Add(\"--uppercase\");");
        source.ShouldContain("if (!string.IsNullOrEmpty(times))");
        source.ShouldContain("args.Add(\"--times\");");
        source.ShouldContain("args.Add(times);");
        
        // XML documentation includes examples and exit codes
        source.ShouldContain("/// Exit codes:");
        source.ShouldContain("/// - 0: Success");
        source.ShouldContain("/// - 1: Invalid input");
        source.ShouldContain("/// Examples:");
        source.ShouldContain("/// - hello World");
        source.ShouldContain("/// - hello World --uppercase");
        
        // Verify it compiles successfully
        Compilation compilation = CreateCompilationWithGeneratedCode(result);
        List<Diagnostic> errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        
        errors.ShouldBeEmpty();
    }

    private static GeneratorDriverRunResult GenerateCode(string openCliJson, string fileName)
    {
        Compilation compilation = CreateCompilation("");
        OpenCliToMcpGenerator generator = new();
        InMemoryAdditionalText additionalText = new(fileName, openCliJson);
        
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([additionalText]);
            
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out _, 
            out _);
            
        return driver.GetRunResult();
    }

    private static Compilation CreateCompilationWithGeneratedCode(GeneratorDriverRunResult result)
    {
        List<SyntaxTree> syntaxTrees = result.GeneratedTrees.ToList();
        
        // Add necessary references - get all runtime assemblies
        List<PortableExecutableReference> references = System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();
        
        // Add mock MCP attributes
        string mockMcpCode = """
                             namespace ModelContextProtocol.Server
                             {
                                 [System.AttributeUsage(System.AttributeTargets.Class)]
                                 public class McpServerToolTypeAttribute : System.Attribute { }
                                 
                                 [System.AttributeUsage(System.AttributeTargets.Method)]
                                 public class McpServerToolAttribute : System.Attribute { }
                             }
                             """;
        
        syntaxTrees.Add(CSharpSyntaxTree.ParseText(mockMcpCode));
        
        // Add the Core assembly reference
        string coreAssemblyPath = typeof(OpenCliToMcp.Core.ICliExecutor).Assembly.Location;
        references.Add(MetadataReference.CreateFromFile(coreAssemblyPath));
        
        return CSharpCompilation.Create(
            "TestCompilation",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static Compilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create(
            "TestCompilation",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private class InMemoryAdditionalText : AdditionalText
    {
        private readonly string _path;
        private readonly string _content;

        public InMemoryAdditionalText(string path, string content)
        {
            _path = path;
            _content = content;
        }

        public override string Path => _path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(_content, Encoding.UTF8);
        }
    }
}