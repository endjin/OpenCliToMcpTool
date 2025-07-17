namespace Weather.McpServer.Tests.TestHelpers;

public static class ArgumentVerifiers
{
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
    
    public static bool VerifyContainsArgs(IEnumerable<string> args, params string[] expected)
    {
        List<string> list = args.ToList();
        return expected.All(e => list.Contains(e));
    }
    
    public static bool VerifyLocationAddArgs(IEnumerable<string> args, string city, string? nickname)
    {
        List<string> list = args.ToList();
        if (!list.Contains("location") || !list.Contains("add") || !list.Contains(city)) return false;
        
        if (nickname != null && (!list.Contains("--nickname") || !list.Contains(nickname))) return false;
        
        return true;
    }
    
    public static bool VerifyLocationRemoveArgs(IEnumerable<string> args, string city, bool force)
    {
        List<string> list = args.ToList();
        if (!list.Contains("location") || !list.Contains("remove") || !list.Contains(city)) return false;
        
        if (force && !list.Contains("--force")) return false;
        
        return true;
    }
    
    public static bool VerifyCurrentArgs(IEnumerable<string> args, string? city, string? unit, bool detailed)
    {
        List<string> list = args.ToList();
        if (!list.Contains("current")) return false;
        
        if (city != null && !list.Contains(city)) return false;
        if (unit != null && (!list.Contains("--unit") || !list.Contains(unit))) return false;
        if (detailed && !list.Contains("--detailed")) return false;
        
        return true;
    }
    
    public static bool VerifyCompareArgs(IEnumerable<string> args, string cities, string? unit)
    {
        List<string> list = args.ToList();
        if (!list.Contains("compare") || !list.Contains(cities)) return false;
        
        if (unit != null && (!list.Contains("--unit") || !list.Contains(unit))) return false;
        
        return true;
    }
    
    public static bool VerifyForecastArgs(IEnumerable<string> args, string? city, string? unit, string? days)
    {
        List<string> list = args.ToList();
        if (!list.Contains("forecast")) return false;
        
        if (city != null && !list.Contains(city)) return false;
        if (unit != null && (!list.Contains("--unit") || !list.Contains(unit))) return false;
        if (days != null && (!list.Contains("--days") || !list.Contains(days))) return false;
        
        return true;
    }
    
    public static bool VerifyMinimumArgs(IEnumerable<string> args, int minCount)
    {
        return args.Count() >= minCount;
    }
}