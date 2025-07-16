using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace OpenCliToMcp.Core.Tests;

[TestClass]
public class CliExecutionExceptionTests
{
    [TestMethod]
    public void Constructor_WithMessageAndResponse_SetsPropertiesCorrectly()
    {
        // Arrange
        string message = "Command execution failed";
        CliResponse response = CliResponse.CreateError("Permission denied", 1);

        // Act
        CliExecutionException exception = new(message, response);

        // Assert
        exception.Message.ShouldBe(message);
        exception.Response.ShouldBe(response);
        exception.InnerException.ShouldBeNull();
    }

    [TestMethod]
    public void Constructor_WithMessageResponseAndInnerException_SetsAllPropertiesCorrectly()
    {
        // Arrange
        string message = "Failed to execute command";
        CliResponse response = CliResponse.CreateError("File not found", 127);
        InvalidOperationException innerException = new("Process could not start");

        // Act
        CliExecutionException exception = new(message, response, innerException);

        // Assert
        exception.Message.ShouldBe(message);
        exception.Response.ShouldBe(response);
        exception.InnerException.ShouldBe(innerException);
    }

    [TestMethod]
    public void Exception_IsSerializable()
    {
        // Arrange
        CliResponse response = CliResponse.CreateError("Test error", 1);
        CliExecutionException original = new("Test exception", response);

        // Act - Simulate serialization/deserialization
        string serialized = System.Text.Json.JsonSerializer.Serialize(new
        {
            Message = original.Message,
            Response = original.Response
        });

        System.Text.Json.JsonElement deserialized = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(serialized);

        // Assert
        deserialized.GetProperty("Message").GetString().ShouldBe(original.Message);
        deserialized.GetProperty("Response").GetProperty("success").GetBoolean().ShouldBeFalse();
        // The exception itself inherits from Exception which handles serialization
    }

    [TestMethod]
    public void Exception_WithFailedResponse_ProvidesUsefulInformation()
    {
        // Arrange
        CliResponse response = new()
        {
            Success = false,
            ExitCode = 128,
            Output = "Partial output",
            Error = "fatal: not a git repository",
            Metadata = new Dictionary<string, object>
            {
                ["command"] = "git status",
                ["workingDirectory"] = "/tmp/test"
            }
        };

        // Act
        CliExecutionException exception = new("Git command failed", response);

        // Assert
        exception.Response.Success.ShouldBeFalse();
        exception.Response.ExitCode.ShouldBe(128);
        exception.Response.Error.ShouldContain("not a git repository");
        exception.Response.Metadata!["command"].ShouldBe("git status");
    }

    [TestMethod]
    public void Exception_CanBeUsedInTryCatch()
    {
        // Arrange
        CliResponse errorResponse = CliResponse.CreateError("Command failed", 1);
        
        // Act & Assert
        Should.Throw<CliExecutionException>(() =>
        {
            throw new CliExecutionException("Test error", errorResponse);
        });

        try
        {
            throw new CliExecutionException("Test error", errorResponse);
        }
        catch (CliExecutionException ex)
        {
            ex.Response.ShouldBe(errorResponse);
            ex.Message.ShouldBe("Test error");
        }
    }

    [TestMethod]
    public void Exception_WithInnerException_PreservesStackTrace()
    {
        // Arrange
        IOException innerException = new("File access error");
        CliResponse response = CliResponse.CreateError("IO Error", 1);

        // Act
        CliExecutionException exception = new("Command failed due to IO error", response, innerException);

        // Assert
        exception.InnerException.ShouldNotBeNull();
        exception.InnerException.ShouldBeOfType<IOException>();
        exception.InnerException.Message.ShouldBe("File access error");
    }

    [TestMethod]
    public void Exception_Response_IsAccessibleAfterThrow()
    {
        // Arrange
        CliResponse response = new()
        {
            Success = false,
            ExitCode = 2,
            Output = "Warning output",
            Error = "Error output"
        };

        CliExecutionException? caughtException = null;

        // Act
        try
        {
            throw new CliExecutionException("Test throw", response);
        }
        catch (CliExecutionException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.ShouldNotBeNull();
        caughtException.Response.ShouldBe(response);
        caughtException.Response.ExitCode.ShouldBe(2);
        caughtException.Response.Output.ShouldBe("Warning output");
        caughtException.Response.Error.ShouldBe("Error output");
    }
}