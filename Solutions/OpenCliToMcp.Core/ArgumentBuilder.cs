using System.Text;

namespace OpenCliToMcp.Core;

/// <summary>
/// Utility class for building and escaping command line arguments.
/// </summary>
public static class ArgumentBuilder
{
    /// <summary>
    /// Builds a command line argument string from a collection of arguments.
    /// </summary>
    /// <param name="arguments">The arguments to build into a string.</param>
    /// <returns>A properly escaped command line argument string.</returns>
    public static string BuildArgumentString(IEnumerable<string> arguments)
    {
        return string.Join(" ", arguments.Select(EscapeArgument));
    }

    /// <summary>
    /// Escapes a single argument for command line usage.
    /// </summary>
    /// <param name="arg">The argument to escape.</param>
    /// <returns>The escaped argument.</returns>
    public static string EscapeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return "\"\"";

        if (!arg.Contains(' ') && !arg.Contains('"') && !arg.Contains('\\'))
            return arg;

        string escaped = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }
}