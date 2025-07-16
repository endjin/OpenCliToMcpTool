using ModelContextProtocol.Server;

using OpenCliToMcp.Generated;

using System.Reflection;

using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace TaskManager.McpServer.Tests;

[TestClass]
public class McpToolAttributesTests
{
    [TestMethod]
    public void TaskManagerToolMcp_HasMcpServerToolTypeAttribute()
    {
        // Arrange
        Type type = typeof(TaskManagerToolMcp);

        // Act
        McpServerToolTypeAttribute? attribute = type.GetCustomAttribute<McpServerToolTypeAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
    }

    [TestMethod]
    public void AllMcpToolMethods_HaveMcpServerToolAttribute()
    {
        // Arrange
        Type type = typeof(TaskManagerToolMcp);
        IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name.EndsWith("Async"));

        // Act & Assert
        foreach (MethodInfo method in methods)
        {
            McpServerToolAttribute? attribute = method.GetCustomAttribute<McpServerToolAttribute>();
            attribute.ShouldNotBeNull($"Method {method.Name} is missing McpServerToolAttribute");
        }
    }

    [TestMethod]
    public void AllMcpToolMethods_HaveDescriptionAttribute()
    {
        // Arrange
        Type type = typeof(TaskManagerToolMcp);
        IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name.EndsWith("Async"));

        // Act & Assert
        foreach (MethodInfo method in methods)
        {
            DescriptionAttribute? attribute = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            attribute.ShouldNotBeNull($"Method {method.Name} is missing DescriptionAttribute");
            attribute.Description.ShouldNotBeNullOrWhiteSpace($"Method {method.Name} has empty description");
        }
    }

    [TestMethod]
    public void AllParameters_HaveProperDescriptions()
    {
        // Arrange
        Type type = typeof(TaskManagerToolMcp);
        IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name.EndsWith("Async"));

        // Act & Assert
        foreach (MethodInfo method in methods)
        {
            IEnumerable<ParameterInfo> parameters = method.GetParameters().Where(p => p.ParameterType != typeof(ICliExecutor) && p.ParameterType != typeof(CancellationToken));

            foreach (ParameterInfo parameter in parameters)
            {
                DescriptionAttribute? attribute = parameter.GetCustomAttribute<DescriptionAttribute>();
                
                // All user-facing parameters should have descriptions
                if (parameter.Name != "cancellationToken")
                {
                    attribute.ShouldNotBeNull($"Parameter {parameter.Name} in method {method.Name} is missing DescriptionAttribute");

                    attribute?.Description.ShouldNotBeNullOrWhiteSpace($"Parameter {parameter.Name} in method {method.Name} has empty description");
                }
            }
        }
    }

    [TestMethod]
    public void VerifyMethodSignatures()
    {
        // Verify that all async methods follow the correct pattern
        Type type = typeof(TaskManagerToolMcp);
        IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name.EndsWith("Async"));

        foreach (MethodInfo method in methods)
        {
            // Should return Task<string>
            method.ReturnType.ShouldBe(typeof(Task<string>), 
                $"Method {method.Name} should return Task<string>");

            // First parameter should be ICliExecutor
            ParameterInfo? firstParam = method.GetParameters().FirstOrDefault();
            firstParam.ShouldNotBeNull($"Method {method.Name} has no parameters");
            firstParam!.ParameterType.ShouldBe(typeof(ICliExecutor), 
                $"Method {method.Name} first parameter should be ICliExecutor");

            // Last parameter should be CancellationToken with default value
            ParameterInfo? lastParam = method.GetParameters().LastOrDefault();
            lastParam.ShouldNotBeNull();
            lastParam!.ParameterType.ShouldBe(typeof(CancellationToken), 
                $"Method {method.Name} last parameter should be CancellationToken");
            lastParam.HasDefaultValue.ShouldBeTrue(
                $"Method {method.Name} CancellationToken should have default value");
        }
    }

    [TestMethod]
    public void VerifySpecificMethodDescriptions()
    {
        // Verify specific important methods have meaningful descriptions
        Type type = typeof(TaskManagerToolMcp);

        var testCases = new[]
        {
            new { MethodName = "StatsAsync", ExpectedDescription = "Show task statistics" },
            new { MethodName = "ListAsync", ExpectedDescription = "List all tasks" },
            new { MethodName = "TaskAddAsync", ExpectedDescription = "Add a new task" },
            new { MethodName = "TaskUpdateAsync", ExpectedDescription = "Update an existing task" },
            new { MethodName = "TaskDeleteAsync", ExpectedDescription = "Delete a task" },
            new { MethodName = "ExportAsync", ExpectedDescription = "Export tasks to various formats" }
        };

        foreach (var testCase in testCases)
        {
            MethodInfo? method = type.GetMethod(testCase.MethodName);
            method.ShouldNotBeNull($"Method {testCase.MethodName} not found");

            DescriptionAttribute? description = method!.GetCustomAttribute<DescriptionAttribute>();
            description.ShouldNotBeNull();
            description!.Description.ShouldBe(testCase.ExpectedDescription);
        }
    }

    [TestMethod]
    public void VerifyParameterTypes()
    {
        // Verify common parameter patterns
        Type type = typeof(TaskManagerToolMcp);
        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

        foreach (MethodInfo method in methods.Where(m => m.Name.EndsWith("Async")))
        {
            ParameterInfo[] parameters = method.GetParameters();

            foreach (ParameterInfo param in parameters)
            {
                // Boolean parameters should have default values (except CancellationToken)
                if (param.ParameterType == typeof(bool))
                {
                    param.HasDefaultValue.ShouldBeTrue($"Boolean parameter {param.Name} in {method.Name} should have default value");
                    param.DefaultValue.ShouldBe(false, $"Boolean parameter {param.Name} in {method.Name} should default to false");
                }

                // Optional string parameters should be nullable with default null
                if (param.ParameterType == typeof(string) && param.Name != "title" && param.Name != "id" && param.Name != "name" && param.Name != "output")
                {
                    param.HasDefaultValue.ShouldBeTrue($"Optional string parameter {param.Name} in {method.Name} should have default value");
                    param.DefaultValue.ShouldBe(null, $"Optional string parameter {param.Name} in {method.Name} should default to null");
                }
            }
        }
    }
}