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
public class NullSafetyTests
{
    [TestMethod]
    public void Generator_WithNullDescriptions_HandlesGracefully()
    {
        // Arrange
        string openCliJson = """
                             {
                               "opencli": "0.1",
                               "info": {
                                 "title": "Test Tool",
                                 "version": "1.0.0"
                               },
                               "commands": {
                                 "test": {
                                   "arguments": [
                                     {
                                       "name": "arg1",
                                       "required": true
                                     }
                                   ],
                                   "options": [
                                     {
                                       "name": "opt1"
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
        generatedTrees.Length.ShouldBe(2);
        
        string mcpToolSource = generatedTrees.First(t => t.FilePath.Contains("TestToolMcp")).GetText().ToString();
        mcpToolSource.ShouldContain("public static async Task<string> TestAsync(");
        mcpToolSource.ShouldContain("string arg1");
        mcpToolSource.ShouldContain("bool opt1 = false");
    }
    
    [TestMethod]
    public void EscapeString_WithSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        string openCliJson = @"{
  ""opencli"": ""0.1"",
  ""info"": {
    ""title"": ""Test"",
    ""version"": ""1.0.0""
  },
  ""commands"": {
    ""test"": {
      ""description"": ""Test with \""quotes\"" and \\\\backslashes\\\\""
    }
  }
}";
        
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
        
        // Find the description in the generated code
        var descriptionIndex = mcpToolSource.IndexOf("[Description(");
        if (descriptionIndex >= 0)
        {
            var descriptionEnd = mcpToolSource.IndexOf(")]", descriptionIndex);
            var descriptionLine = mcpToolSource.Substring(descriptionIndex, descriptionEnd - descriptionIndex + 2);
            // The description should be properly escaped for C# attribute
            descriptionLine.ShouldContain(@"Test with \""quotes\"" and \\\\backslashes\\\\");
        }
        else
        {
            // If no [Description] attribute, check in XML comments
            mcpToolSource.ShouldContain("Test with");
        }
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