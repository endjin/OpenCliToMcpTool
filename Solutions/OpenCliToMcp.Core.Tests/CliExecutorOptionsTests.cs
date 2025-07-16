using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace OpenCliToMcp.Core.Tests;

[TestClass]
public class CliExecutorOptionsTests
{
    [TestMethod]
    public void CliExecutorOptions_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        CliExecutorOptions options = new();

        // Assert
        options.ExecutableName.ShouldBeNull();
        options.ExecutablePath.ShouldBeNull();
        options.WorkingDirectory.ShouldBeNull();
        options.TimeoutSeconds.ShouldBe(30);
        options.EnvironmentVariables.ShouldBeNull();
        options.SearchPaths.ShouldBeNull();
        options.SearchInPath.ShouldBeTrue();
        options.ThrowOnError.ShouldBeFalse();
        options.ResponseFormat.ShouldBe(ResponseFormat.Json);
    }

    [TestMethod]
    public void CliExecutorOptions_WithCustomValues_StoresThemCorrectly()
    {
        // Arrange
        Dictionary<string, string> envVars = new()
        {
            ["VAR1"] = "value1",
            ["VAR2"] = "value2"
        };
        string[] searchPaths = ["/path1", "/path2"];

        // Act
        CliExecutorOptions options = new()
        {
            ExecutableName = "test-cli",
            ExecutablePath = "/usr/bin/test-cli",
            WorkingDirectory = "/working/dir",
            TimeoutSeconds = 60,
            EnvironmentVariables = envVars,
            SearchPaths = searchPaths,
            SearchInPath = false,
            ThrowOnError = true,
            ResponseFormat = ResponseFormat.Raw
        };

        // Assert
        options.ExecutableName.ShouldBe("test-cli");
        options.ExecutablePath.ShouldBe("/usr/bin/test-cli");
        options.WorkingDirectory.ShouldBe("/working/dir");
        options.TimeoutSeconds.ShouldBe(60);
        options.EnvironmentVariables.ShouldBe(envVars);
        options.SearchPaths.ShouldBe(searchPaths);
        options.SearchInPath.ShouldBeFalse();
        options.ThrowOnError.ShouldBeTrue();
        options.ResponseFormat.ShouldBe(ResponseFormat.Raw);
    }

    [TestMethod]
    public void CliExecutorOptions_AsRecord_SupportsValueEquality()
    {
        // Arrange
        CliExecutorOptions options1 = new()
        {
            ExecutableName = "git",
            TimeoutSeconds = 45,
            ResponseFormat = ResponseFormat.PlainText
        };

        CliExecutorOptions options2 = new()
        {
            ExecutableName = "git",
            TimeoutSeconds = 45,
            ResponseFormat = ResponseFormat.PlainText
        };

        CliExecutorOptions options3 = new()
        {
            ExecutableName = "git",
            TimeoutSeconds = 30, // Different
            ResponseFormat = ResponseFormat.PlainText
        };

        // Act & Assert
        options1.ShouldBe(options2);
        options1.ShouldNotBe(options3);
        (options1 == options2).ShouldBeTrue();
        (options1 == options3).ShouldBeFalse();
    }

    [TestMethod]
    public void CliExecutorOptions_WithModification_CreatesNewInstance()
    {
        // Arrange
        CliExecutorOptions original = new()
        {
            ExecutableName = "original",
            TimeoutSeconds = 30
        };

        // Act
        CliExecutorOptions modified = original with { TimeoutSeconds = 60 };

        // Assert
        original.TimeoutSeconds.ShouldBe(30);
        modified.TimeoutSeconds.ShouldBe(60);
        modified.ExecutableName.ShouldBe("original");
        ReferenceEquals(original, modified).ShouldBeFalse();
    }

    [TestMethod]
    public void ResponseFormat_HasExpectedValues()
    {
        // Arrange & Act
        ResponseFormat[] allFormats = Enum.GetValues<ResponseFormat>();

        // Assert
        allFormats.ShouldContain(ResponseFormat.Json);
        allFormats.ShouldContain(ResponseFormat.Raw);
        allFormats.ShouldContain(ResponseFormat.PlainText);
        allFormats.Length.ShouldBe(3);
    }

    [TestMethod]
    public void CliExecutorOptions_EnvironmentVariables_CanBeModified()
    {
        // Arrange
        Dictionary<string, string> envVars = new() { ["KEY1"] = "value1" };
        CliExecutorOptions options = new() { EnvironmentVariables = envVars };

        // Act
        envVars["KEY2"] = "value2";

        // Assert
        options.EnvironmentVariables!.Count.ShouldBe(2);
        options.EnvironmentVariables["KEY1"].ShouldBe("value1");
        options.EnvironmentVariables["KEY2"].ShouldBe("value2");
    }

    [TestMethod]
    public void CliExecutorOptions_SearchPaths_CanBeModified()
    {
        // Arrange
        string[] paths = ["/path1", "/path2"];
        CliExecutorOptions options = new() { SearchPaths = paths };

        // Act
        paths[1] = "/modified-path2";

        // Assert
        options.SearchPaths![1].ShouldBe("/modified-path2");
    }
}