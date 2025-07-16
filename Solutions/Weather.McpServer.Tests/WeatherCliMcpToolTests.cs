using Weather.McpServer.Tests.TestHelpers;

namespace Weather.McpServer.Tests;

[TestClass]
public class WeatherCliMcpToolTests
{
    private ICliExecutor cliExecutor = null!;
    private WeatherCliMcpTool weatherTool = null!;
    private CancellationToken cancellationToken;

    [TestInitialize]
    public void Setup()
    {
        cliExecutor = Substitute.For<ICliExecutor>();
        weatherTool = new WeatherCliMcpTool(cliExecutor);
        cancellationToken = CancellationToken.None;
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithNullCliExecutor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new WeatherCliMcpTool(null!)).ParamName.ShouldBe("cliExecutor");
    }

    [TestMethod]
    public void Constructor_WithValidCliExecutor_CreatesInstance()
    {
        // Arrange
        ICliExecutor executor = Substitute.For<ICliExecutor>();

        // Act
        WeatherCliMcpTool tool = new(executor);

        // Assert
        tool.ShouldNotBeNull();
    }

    #endregion

    #region Location Command Tests

    [TestMethod]
    public async Task LocationAsync_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Location help output");

        // Act
        string result = await weatherTool.LocationAsync(cancellationToken);

        // Assert
        result.ShouldBe("Location help output");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "location")), cancellationToken);
    }

    [TestMethod]
    public async Task LocationAddAsync_WithCityOnly_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Location added");

        // Act
        string result = await weatherTool.LocationAddAsync("London", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Location added");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyLocationAddArgs(args, "London", null)), cancellationToken);
    }

    [TestMethod]
    public async Task LocationAddAsync_WithCityAndNickname_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Location added with nickname");

        // Act
        string result = await weatherTool.LocationAddAsync("New York", nickname: "home", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Location added with nickname");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyLocationAddArgs(args, "New York", "home")), cancellationToken);
    }

    [TestMethod]
    public async Task LocationListAsync_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("London\nParis\nTokyo");

        // Act
        string result = await weatherTool.LocationListAsync(cancellationToken);

        // Assert
        result.ShouldBe("London\nParis\nTokyo");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "location", "list")), cancellationToken);
    }

    [TestMethod]
    public async Task LocationRemoveAsync_WithoutForce_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Location removed");

        // Act
        string result = await weatherTool.LocationRemoveAsync("London", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Location removed");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyLocationRemoveArgs(args, "London", false)), cancellationToken);
    }

    [TestMethod]
    public async Task LocationRemoveAsync_WithForce_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Location force removed");

        // Act
        string result = await weatherTool.LocationRemoveAsync("London", force: true, cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Location force removed");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyLocationRemoveArgs(args, "London", true)), cancellationToken);
    }

    #endregion

    #region Current Weather Command Tests

    [TestMethod]
    public async Task CurrentAsync_WithCity_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Current weather in London");

        // Act
        string result = await weatherTool.CurrentAsync("London", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Current weather in London");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyCurrentArgs(args, "London", null, false)), cancellationToken);
    }

    [TestMethod]
    public async Task CurrentAsync_WithCityAndUnit_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Weather in Paris: 68°F");

        // Act
        string result = await weatherTool.CurrentAsync("Paris", unit: "fahrenheit", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Weather in Paris: 68°F");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyCurrentArgs(args, "Paris", "fahrenheit", false)), cancellationToken);
    }

    [TestMethod]
    public async Task CurrentAsync_WithAllParameters_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Detailed weather data");

        // Act
        string result = await weatherTool.CurrentAsync(
            city: "Tokyo",
            unit: "metric",
            detailed: true,
            format: "json",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Detailed weather data");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyCurrentArgs(args, "Tokyo", "metric", true)), cancellationToken);
    }

    #endregion

    #region Compare Command Tests

    [TestMethod]
    public async Task CompareAsync_WithCities_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("London: 15°C, Paris: 20°C");

        // Act
        string result = await weatherTool.CompareAsync("London Paris", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("London: 15°C, Paris: 20°C");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "compare", "London Paris")), cancellationToken);
    }

    [TestMethod]
    public async Task CompareAsync_WithUnit_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("London: 59°F, Paris: 68°F");

        // Act
        string result = await weatherTool.CompareAsync(cities: "London Paris", unit: "imperial", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("London: 59°F, Paris: 68°F");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "compare", "--unit", "imperial", "London Paris")), cancellationToken);
    }

    #endregion

    #region Forecast Command Tests

    [TestMethod]
    public async Task ForecastAsync_WithCity_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("7-day forecast for London");

        // Act
        string result = await weatherTool.ForecastAsync("London", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("7-day forecast for London");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "forecast", "London")), cancellationToken);
    }

    [TestMethod]
    public async Task ForecastAsync_WithCityAndDays_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("10-day forecast for Berlin");

        // Act
        string result = await weatherTool.ForecastAsync("Berlin", days: "10", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("10-day forecast for Berlin");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "forecast", "--days", "10", "Berlin")), cancellationToken);
    }

    [TestMethod]
    public async Task ForecastAsync_WithAllParameters_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("3-day forecast in Fahrenheit");

        // Act
        string result = await weatherTool.ForecastAsync(city: "Sydney", days: "3", unit: "imperial", format: "chart", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("3-day forecast in Fahrenheit");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "forecast", "--days", "3", "--unit", "imperial", "--format", "chart", "Sydney")), cancellationToken);
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public async Task LocationAddAsync_WhenCliExecutorThrows_PropagatesException()
    {
        // Arrange
        InvalidOperationException expectedException = new("CLI not found");
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<string>(expectedException));

        // Act & Assert
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(weatherTool.LocationAddAsync("London", cancellationToken: cancellationToken));
        
        exception.Message.ShouldBe("CLI not found");
    }

    [TestMethod]
    public async Task CurrentAsync_WithCancellation_PassesCancellationToken()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Cancelled");

        // Act
        await weatherTool.CurrentAsync("London", cancellationToken: cts.Token);

        // Assert
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Any<IEnumerable<string>>(), cts.Token);
    }

    #endregion

    #region Edge Case Tests

    [TestMethod]
    public async Task LocationAddAsync_WithSpecialCharactersInCity_HandlesCorrectly()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Location added");

        string cityWithSpecialChars = "São Paulo & Rio";

        // Act
        string result = await weatherTool.LocationAddAsync(cityWithSpecialChars, cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Location added");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "location", "add", cityWithSpecialChars)), cancellationToken);
    }

    [TestMethod]
    public async Task LocationAddAsync_WithLongCityName_HandlesCorrectly()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Location added");

        string longCityName = new('A', 255); // Very long city name

        // Act
        string result = await weatherTool.LocationAddAsync(longCityName, cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Location added");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "location", "add", longCityName)), cancellationToken);
    }

    [TestMethod]
    public async Task CurrentAsync_WithCityContainingSpaces_HandlesCorrectly()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Weather data");

        string cityWithSpaces = "New York City";

        // Act
        string result = await weatherTool.CurrentAsync(cityWithSpaces, cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Weather data");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "current", cityWithSpaces)), cancellationToken);
    }

    [TestMethod]
    public async Task CompareAsync_WithCitiesContainingQuotes_HandlesCorrectly()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Comparison result");

        string cities = "L'Aquila Val-d'Or";

        // Act
        string result = await weatherTool.CompareAsync(cities, cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Comparison result");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "compare", cities)), cancellationToken);
    }

    #endregion

    #region Null and Empty Parameter Tests

    [TestMethod]
    public async Task LocationAddAsync_WithEmptyNickname_OmitsNicknameParameter()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Location added");

        // Act
        string result = await weatherTool.LocationAddAsync(city: "London", nickname: "", cancellationToken: cancellationToken);
        // Assert
        result.ShouldBe("Location added");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "location", "add", "London") && 
            !ArgumentVerifiers.VerifyContainsArgs(args, "--nickname")), cancellationToken);
    }

    [TestMethod]
    public async Task CurrentAsync_WithEmptyUnit_OmitsUnitParameter()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Current weather");

        // Act
        string result = await weatherTool.CurrentAsync(city: "Paris", unit: "", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Current weather");
        await cliExecutor.Received(1).ExecuteAsync(
            "weather",
            Arg.Is<IEnumerable<string>>(args => 
                ArgumentVerifiers.VerifyContainsArgs(args, "current", "Paris") &&
                !ArgumentVerifiers.VerifyContainsArgs(args, "--unit")),
            cancellationToken);
    }

    [TestMethod]
    public async Task ForecastAsync_WithEmptyDays_OmitsDaysParameter()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns("Default forecast");

        // Act
        string result = await weatherTool.ForecastAsync("London", days: "", cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Default forecast");
        await cliExecutor.Received(1).ExecuteAsync("weather", Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "forecast", "London")), cancellationToken);
    }

    #endregion
}