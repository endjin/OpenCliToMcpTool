namespace TaskManager.McpServer.Tests.TestHelpers;

public static class ArgumentVerifiers
{
    public static bool VerifyStatsArgs(IEnumerable<string> args, bool verbose, string? config, string? period, string? project)
    {
        List<string> list = args.ToList();
        int expectedCount = 1; // "stats"
        
        if (verbose) expectedCount += 1;
        if (config != null) expectedCount += 2;
        if (period != null) expectedCount += 2;
        if (project != null) expectedCount += 2;
        
        if (list.Count != expectedCount) return false;
        
        int index = 0;
        if (list[index++] != "stats") return false;
        
        if (verbose && list[index++] != "--verbose") return false;
        
        if (config != null)
        {
            if (list[index++] != "--config") return false;
            if (list[index++] != config) return false;
        }
        
        if (period != null)
        {
            if (list[index++] != "--period") return false;
            if (list[index++] != period) return false;
        }
        
        if (project != null)
        {
            if (list[index++] != "--project") return false;
            if (list[index++] != project) return false;
        }
        
        return true;
    }
    
    public static bool VerifyExactArgs(IEnumerable<string> args, params string[] expected)
    {
        List<string> list = args.ToList();
        if (list.Count != expected.Length) return false;
        
        for (int i = 0; i < expected.Length; i++)
        {
            if (list[i] != expected[i]) return false;
        }
        
        return true;
    }
    
    public static bool VerifyArgsCount(IEnumerable<string> args, int expectedCount)
    {
        return args.Count() == expectedCount;
    }
    
    public static bool VerifyFirstArg(IEnumerable<string> args, string expected)
    {
        return args.FirstOrDefault() == expected;
    }
    
    public static bool VerifyContainsArgs(IEnumerable<string> args, params string[] expected)
    {
        List<string> list = args.ToList();
        return expected.All(e => list.Contains(e));
    }
    
    public static bool VerifyTaskArgs(IEnumerable<string> args, bool verbose, string? config)
    {
        List<string> list = args.ToList();
        int expectedCount = 1; // "task"
        
        if (verbose) expectedCount += 1;
        if (config != null) expectedCount += 2;
        
        if (list.Count != expectedCount) return false;
        
        int index = 0;
        if (list[index++] != "task") return false;
        
        if (verbose && list[index++] != "--verbose") return false;
        
        if (config != null)
        {
            if (list[index++] != "--config") return false;
            if (list[index++] != config) return false;
        }
        
        return true;
    }
    
    public static bool VerifyListArgs(IEnumerable<string> args, bool verbose, string? config, string? status, string? priority, string? assignee, string? project, string? sort, bool showCompleted)
    {
        List<string> list = args.ToList();
        if (!list.Contains("list")) return false;
        
        if (verbose && !list.Contains("--verbose")) return false;
        if (config != null && (!list.Contains("--config") || !list.Contains(config))) return false;
        if (status != null && (!list.Contains("--status") || !list.Contains(status))) return false;
        if (priority != null && (!list.Contains("--priority") || !list.Contains(priority))) return false;
        if (assignee != null && (!list.Contains("--assignee") || !list.Contains(assignee))) return false;
        if (project != null && (!list.Contains("--project") || !list.Contains(project))) return false;
        if (sort != null && (!list.Contains("--sort") || !list.Contains(sort))) return false;
        if (showCompleted && !list.Contains("--show-completed")) return false;
        
        return true;
    }
    
    public static bool VerifyTaskAddArgs(IEnumerable<string> args, string title, bool verbose, string? config, string? description, string? priority, string? due, string? assignee, string? project, string? tags)
    {
        List<string> list = args.ToList();
        if (!list.Contains("task") || !list.Contains("add") || !list.Contains(title)) return false;
        
        if (verbose && !list.Contains("--verbose")) return false;
        if (config != null && (!list.Contains("--config") || !list.Contains(config))) return false;
        if (description != null && (!list.Contains("--description") || !list.Contains(description))) return false;
        if (priority != null && (!list.Contains("--priority") || !list.Contains(priority))) return false;
        if (due != null && (!list.Contains("--due") || !list.Contains(due))) return false;
        if (assignee != null && (!list.Contains("--assignee") || !list.Contains(assignee))) return false;
        if (project != null && (!list.Contains("--project") || !list.Contains(project))) return false;
        if (tags != null && (!list.Contains("--tags") || !list.Contains(tags))) return false;
        
        return true;
    }
    
    public static bool VerifyProjectListArgs(IEnumerable<string> args, bool verbose, string? config, bool active)
    {
        List<string> list = args.ToList();
        if (!list.Contains("project") || !list.Contains("list")) return false;
        
        if (verbose && !list.Contains("--verbose")) return false;
        if (config != null && (!list.Contains("--config") || !list.Contains(config))) return false;
        if (active && !list.Contains("--active")) return false;
        
        return true;
    }
    
    public static bool VerifyMinimumArgs(IEnumerable<string> args, int minCount)
    {
        return args.Count() >= minCount;
    }
}