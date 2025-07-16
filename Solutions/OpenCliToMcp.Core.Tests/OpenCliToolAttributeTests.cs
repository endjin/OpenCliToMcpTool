using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace OpenCliToMcp.Core.Tests;

[TestClass]
public class OpenCliToolAttributeTests
{
    [TestMethod]
    public void Constructor_WithValidPath_SetsFilePath()
    {
        // Arrange
        string expectedPath = "test.opencli.json";
        
        // Act
        OpenCliToolAttribute attribute = new(expectedPath);
        
        // Assert
        attribute.FilePath.ShouldBe(expectedPath);
    }
    
    [TestMethod]
    public void Constructor_WithNullPath_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullPath = null;
        
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new OpenCliToolAttribute(nullPath!))
            .ParamName.ShouldBe("filePath");
    }
    
    [TestMethod]
    public void FilePath_Get_ReturnsSetValue()
    {
        // Arrange
        string expectedPath = "../relative/path/to/spec.json";
        OpenCliToolAttribute attribute = new(expectedPath);
        
        // Act
        string actualPath = attribute.FilePath;
        
        // Assert
        actualPath.ShouldBe(expectedPath);
    }
}