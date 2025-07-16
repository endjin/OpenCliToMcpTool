using System.Text.Json;

namespace Weather.McpServer.Tests;

[TestClass]
public class IntegrationTests
{
    private ICliExecutor mockCliExecutor = null!;
    private WeatherCliMcpTool weatherTool = null!;

    [TestInitialize]
    public void Setup()
    {
        mockCliExecutor = Substitute.For<ICliExecutor>();
        weatherTool = new WeatherCliMcpTool(mockCliExecutor);
    }

    #region End-to-End Scenarios

    [TestMethod]
    public async Task CompleteLocationWorkflow_AddListRemove_Success()
    {
        // Scenario: Add a location, list it, then remove it
        
        // Step 1: Add a new location
        mockCliExecutor.ExecuteAsync("weather", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args[0] == "location" && args[1] == "add")
                {
                    return Task.FromResult("Location 'London' added successfully");
                }
                return Task.FromResult("Unknown command");
            });

        string addResult = await weatherTool.LocationAddAsync("London", nickname: "home");
        addResult.ShouldContain("Location 'London' added successfully");

        // Step 2: List locations to verify it was added
        mockCliExecutor.ExecuteAsync("weather", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args[0] == "location" && args[1] == "list")
                {
                    return Task.FromResult("London (home)\nParis\nTokyo");
                }
                return Task.FromResult("Unknown command");
            });

        string listResult = await weatherTool.LocationListAsync();
        listResult.ShouldContain("London (home)");
        listResult.ShouldContain("Paris");
        listResult.ShouldContain("Tokyo");

        // Step 3: Remove the location
        mockCliExecutor.ExecuteAsync("weather", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args[0] == "location" && args[1] == "remove" && args.Contains("London"))
                {
                    return Task.FromResult("Location 'London' removed successfully");
                }
                return Task.FromResult("Unknown command");
            });

        string removeResult = await weatherTool.LocationRemoveAsync("London", force: true);
        removeResult.ShouldContain("Location 'London' removed successfully");
    }

    [TestMethod]
    public async Task WeatherComparison_BetweenTwoCities_Success()
    {
        // Scenario: Get current weather for two cities and compare them

        // Setup all mock responses at once
        mockCliExecutor.ExecuteAsync("weather", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                
                // Handle current weather requests
                if (args[0] == "current" && args.Contains("London"))
                {
                    return Task.FromResult("London: 15°C, Partly cloudy");
                }
                if (args[0] == "current" && args.Contains("Paris"))
                {
                    return Task.FromResult("Paris: 20°C, Sunny");
                }
                
                // Handle compare request
                if (args[0] == "compare" && args.Contains("London Paris"))
                {
                    return Task.FromResult("""
                                           Weather Comparison:
                                           London: 15°C, Partly cloudy
                                           Paris: 20°C, Sunny
                                           Difference: Paris is 5°C warmer
                                           """);
                }
                
                return Task.FromResult("Unknown command");
            });

        // Step 1: Get weather for first city
        string londonWeather = await weatherTool.CurrentAsync("London");
        londonWeather.ShouldContain("15°C");

        // Step 2: Get weather for second city
        string parisWeather = await weatherTool.CurrentAsync("Paris");
        parisWeather.ShouldContain("20°C");

        // Step 3: Compare the two cities
        string comparisonResult = await weatherTool.CompareAsync("London Paris");
        comparisonResult.ShouldContain("London: 15°C");
        comparisonResult.ShouldContain("Paris: 20°C");
        comparisonResult.ShouldContain("5°C warmer");
    }

    #endregion

    #region Error Scenarios

    [TestMethod]
    public async Task LocationOperations_WithInvalidCity_ReturnsError()
    {
        // Arrange
        mockCliExecutor.ExecuteAsync("weather", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args.Contains("InvalidCity123"))
                {
                    return Task.FromResult("Error: City 'InvalidCity123' not found");
                }
                return Task.FromResult("Unknown command");
            });

        // Act
        string addResult = await weatherTool.LocationAddAsync("InvalidCity123");
        string currentResult = await weatherTool.CurrentAsync("InvalidCity123");
        string forecastResult = await weatherTool.ForecastAsync("InvalidCity123");

        // Assert
        addResult.ShouldContain("Error: City 'InvalidCity123' not found");
        currentResult.ShouldContain("Error: City 'InvalidCity123' not found");
        forecastResult.ShouldContain("Error: City 'InvalidCity123' not found");
    }

    [TestMethod]
    public async Task Forecast_WithInvalidDays_ReturnsError()
    {
        // Arrange
        mockCliExecutor.ExecuteAsync("weather", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args.Contains("--days") && args.Contains("30"))
                {
                    return Task.FromResult("Error: Days must be between 1 and 14");
                }
                return Task.FromResult("Forecast successful");
            });

        // Act
        string result = await weatherTool.ForecastAsync("London", days: "30");

        // Assert
        result.ShouldContain("Error: Days must be between 1 and 14");
    }

    #endregion

    #region JSON Output Scenarios

    [TestMethod]
    public async Task Current_WithJsonOutput_ParsesCorrectly()
    {
        // Arrange
        string weatherJson = """
            {
              "location": "Tokyo",
              "temperature": 22,
              "unit": "celsius",
              "condition": "Clear",
              "humidity": 65,
              "wind_speed": 10,
              "timestamp": "2024-01-15T10:00:00Z"
            }
            """;

        mockCliExecutor.ExecuteAsync("weather", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(weatherJson);

        // Act
        string result = await weatherTool.CurrentAsync("Tokyo");

        // Assert
        result.ShouldBe(weatherJson);
        
        // Verify it's valid JSON
        JsonElement parsed = JsonSerializer.Deserialize<JsonElement>(result);
        parsed.GetProperty("location").GetString().ShouldBe("Tokyo");
        parsed.GetProperty("temperature").GetInt32().ShouldBe(22);
        parsed.GetProperty("condition").GetString().ShouldBe("Clear");
    }

    #endregion

    #region Unit Conversion Scenarios

    [TestMethod]
    public async Task Weather_WithDifferentUnits_ReturnsCorrectFormat()
    {
        // Arrange
        Dictionary<string, string> unitResponses = new()
        {
            ["metric"] = "Temperature: 20°C",
            ["imperial"] = "Temperature: 68°F",
            ["kelvin"] = "Temperature: 293.15K"
        };

        mockCliExecutor.ExecuteAsync("weather", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                int unitIndex = args.IndexOf("--unit");
                if (unitIndex >= 0 && unitIndex + 1 < args.Count)
                {
                    string unit = args[unitIndex + 1];
                    if (unitResponses.TryGetValue(unit, out string? response))
                    {
                        return Task.FromResult(response);
                    }
                }
                return Task.FromResult("Temperature: 20°C"); // Default metric
            });

        // Act & Assert
        foreach (string unit in new[] { "metric", "imperial", "kelvin" })
        {
            string result = await weatherTool.CurrentAsync("London", unit: unit);
            result.ShouldBe(unitResponses[unit]);
        }
    }

    #endregion

    #region Cancellation Scenarios

    [TestMethod]
    public async Task LongRunningOperation_WithCancellation_PropagatesCancellation()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        TaskCompletionSource<string> tcs = new();

        mockCliExecutor.ExecuteAsync("weather", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(async info =>
            {
                CancellationToken token = info.Arg<CancellationToken>();
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                    return "Should not reach here";
                }
                catch (OperationCanceledException)
                {
                    tcs.SetResult("Cancelled");
                    throw;
                }
            });

        // Act
        Task<string> forecastTask = weatherTool.ForecastAsync(
            "London",
            days: "14",
            cancellationToken: cts.Token);

        // Cancel after a short delay
        await Task.Delay(100);
        cts.Cancel();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(forecastTask);
        string cancellationResult = await tcs.Task;
        cancellationResult.ShouldBe("Cancelled");
    }

    #endregion
}