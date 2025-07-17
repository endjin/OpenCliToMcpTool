# TaskManager.McpServer Tests

This test project provides comprehensive unit and integration tests for the TaskManager.McpServer project.

## Test Structure

### Test Files

1. **TaskManagerToolMcpTests.cs**
   - Unit tests for all generated MCP tool methods
   - Tests command building logic for each method
   - Validates parameter handling and error scenarios
   - Ensures proper null/empty parameter handling

2. **ProgramTests.cs**
   - Tests for host configuration and service setup
   - Validates dependency injection configuration
   - Tests CliExecutorOptions loading from configuration
   - Verifies logging configuration

3. **IntegrationTests.cs**
   - End-to-end scenario tests
   - Complete workflow testing (add, list, update, delete)
   - Project management scenarios
   - Error handling scenarios
   - JSON output parsing tests
   - Cancellation token propagation tests

4. **ConfigurableCliExecutorTests.cs**
   - Tests for the ConfigurableCliExecutor class
   - Validates option loading and configuration
   - Tests constructor behavior and logging

5. **McpToolAttributesTests.cs**
   - Validates all MCP attributes are properly generated
   - Ensures method signatures follow conventions
   - Verifies parameter descriptions and types
   - Tests attribute presence on all tool methods

### Mock Implementations

- **Mocks/MockTaskManagerCli.cs**
  - Comprehensive mock implementation of task manager CLI
  - Simulates realistic CLI responses
  - Supports all commands with appropriate responses
  - Tracks command history for verification

## Testing Frameworks

- **MSTest**: Test framework with Microsoft.Testing.Platform
- **Shouldly**: Fluent assertion library for readable test assertions
- **NSubstitute**: Mocking framework for creating test doubles

## Running Tests

```bash
# Build the test project
dotnet build

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test category
dotnet test --filter "Category=Unit"
```

## Test Coverage

The tests provide comprehensive coverage for:
- All MCP tool methods (stats, list, task operations, project operations, export)
- Command-line argument building
- Parameter validation and defaults
- Error handling and exceptions
- Cancellation token propagation
- Configuration loading
- Host setup and dependency injection

## Test Patterns

All tests follow these patterns:
- **AAA Pattern**: Arrange-Act-Assert structure
- **TDD Principles**: Tests written to drive implementation
- **Behavior Testing**: Focus on behavior rather than implementation
- **Real Types**: Uses actual domain types from the main project
- **Comprehensive Scenarios**: Both success and failure paths tested