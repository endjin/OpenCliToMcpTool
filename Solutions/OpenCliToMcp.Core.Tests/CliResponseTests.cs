using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace OpenCliToMcp.Core.Tests;

[TestClass]
public class CliResponseTests
{
    [TestMethod]
    public void CreateSuccess_WithOutput_CreatesSuccessfulResponse()
    {
        // Arrange
        string output = "Command executed successfully";

        // Act
        CliResponse response = CliResponse.CreateSuccess(output);

        // Assert
        response.Success.ShouldBeTrue();
        response.ExitCode.ShouldBe(0);
        response.Output.ShouldBe(output);
        response.Error.ShouldBe(string.Empty);
        response.Timestamp.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        response.Metadata.ShouldBeNull();
    }

    [TestMethod]
    public void CreateSuccess_WithCustomExitCode_UsesProvidedExitCode()
    {
        // Arrange
        string output = "Warning: deprecated feature used";
        int exitCode = 2; // Non-zero but still success

        // Act
        CliResponse response = CliResponse.CreateSuccess(output, exitCode);

        // Assert
        response.Success.ShouldBeTrue();
        response.ExitCode.ShouldBe(2);
        response.Output.ShouldBe(output);
    }

    [TestMethod]
    public void CreateSuccess_WithTrailingWhitespace_TrimsOutput()
    {
        // Arrange
        string output = "Output with trailing spaces   \n\r\n";

        // Act
        CliResponse response = CliResponse.CreateSuccess(output);

        // Assert
        response.Output.ShouldBe("Output with trailing spaces");
    }

    [TestMethod]
    public void CreateError_WithErrorMessage_CreatesErrorResponse()
    {
        // Arrange
        string error = "Command not found";

        // Act
        CliResponse response = CliResponse.CreateError(error);

        // Assert
        response.Success.ShouldBeFalse();
        response.ExitCode.ShouldBe(-1);
        response.Output.ShouldBe(string.Empty);
        response.Error.ShouldBe(error);
    }

    [TestMethod]
    public void CreateError_WithCustomExitCodeAndOutput_UsesProvidedValues()
    {
        // Arrange
        string error = "Permission denied";
        int exitCode = 126;
        string output = "Partial output before error";

        // Act
        CliResponse response = CliResponse.CreateError(error, exitCode, output);

        // Assert
        response.Success.ShouldBeFalse();
        response.ExitCode.ShouldBe(126);
        response.Output.ShouldBe(output);
        response.Error.ShouldBe(error);
    }

    [TestMethod]
    public void CreateError_WithTrailingWhitespace_TrimsErrorAndOutput()
    {
        // Arrange
        string error = "Error message  \n";
        string output = "Output  \r\n";

        // Act
        CliResponse response = CliResponse.CreateError(error, 1, output);

        // Assert
        response.Error.ShouldBe("Error message");
        response.Output.ShouldBe("Output");
    }

    [TestMethod]
    public void ToJson_WithAllFields_SerializesCorrectly()
    {
        // Arrange
        CliResponse response = new()
        {
            Success = true,
            ExitCode = 0,
            Output = "Test output",
            Error = "",
            Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Metadata = new Dictionary<string, object>
            {
                ["duration"] = 1500,
                ["command"] = "test"
            }
        };

        // Act
        string json = response.ToJson(writeIndented: false);

        // Assert
        json.ShouldContain("\"success\":true");
        json.ShouldContain("\"exitCode\":0");
        json.ShouldContain("\"output\":\"Test output\"");
        json.ShouldContain("\"error\":\"\"");
        json.ShouldContain("\"timestamp\":\"2024-01-15T10:30:00Z\"");
        json.ShouldContain("\"metadata\":{");
        json.ShouldContain("\"duration\":1500");
        json.ShouldContain("\"command\":\"test\"");
    }

    [TestMethod]
    public void ToJson_WithNullMetadata_OmitsMetadataField()
    {
        // Arrange
        CliResponse response = CliResponse.CreateSuccess("Output");

        // Act
        string json = response.ToJson(writeIndented: false);

        // Assert
        json.ShouldNotContain("\"metadata\"");
    }

    [TestMethod]
    public void ToJson_WithIndentation_FormatsJsonProperly()
    {
        // Arrange
        CliResponse response = CliResponse.CreateSuccess("Test");

        // Act
        string json = response.ToJson(writeIndented: true);

        // Assert
        json.ShouldContain("\n");
        json.ShouldContain("  ");
    }

    [TestMethod]
    public void CliResponse_AsRecord_SupportsValueEquality()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        CliResponse response1 = new()
        {
            Success = true,
            ExitCode = 0,
            Output = "Output",
            Error = "",
            Timestamp = timestamp
        };

        CliResponse response2 = new()
        {
            Success = true,
            ExitCode = 0,
            Output = "Output",
            Error = "",
            Timestamp = timestamp
        };

        CliResponse response3 = new()
        {
            Success = false, // Different
            ExitCode = 0,
            Output = "Output",
            Error = "",
            Timestamp = timestamp
        };

        // Act & Assert
        response1.ShouldBe(response2);
        response1.ShouldNotBe(response3);
        (response1 == response2).ShouldBeTrue();
        (response1 == response3).ShouldBeFalse();
    }

    [TestMethod]
    public void CliResponse_WithModification_CreatesNewInstance()
    {
        // Arrange
        CliResponse original = CliResponse.CreateSuccess("Original output");

        // Act
        CliResponse modified = original with { Output = "Modified output" };

        // Assert
        original.Output.ShouldBe("Original output");
        modified.Output.ShouldBe("Modified output");
        modified.Success.ShouldBe(original.Success);
        ReferenceEquals(original, modified).ShouldBeFalse();
    }

    [TestMethod]
    public void CliResponse_Deserialization_WorksCorrectly()
    {
        // Arrange
        string json = """
            {
              "success": true,
              "exitCode": 0,
              "output": "Deserialized output",
              "error": "",
              "timestamp": "2024-01-15T10:30:00Z",
              "metadata": {
                "key": "value"
              }
            }
            """;

        // Act
        CliResponse? response = JsonSerializer.Deserialize<CliResponse>(json);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.ExitCode.ShouldBe(0);
        response.Output.ShouldBe("Deserialized output");
        response.Error.ShouldBe("");
        response.Timestamp.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        response.Metadata.ShouldNotBeNull();
        response.Metadata!["key"].ToString().ShouldBe("value");
    }
}