using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCliToMcp.Generator.Models;

// Value-equatable records for proper incremental generator caching
public sealed record OpenCliSpec(
    string? Opencli,
    OpenCliInfo? Info,
    IReadOnlyDictionary<string, OpenCliCommand>? Commands,
    IReadOnlyList<OpenCliOption>? Options
) : IEquatable<OpenCliSpec>
{
    public bool Equals(OpenCliSpec? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Opencli == other.Opencli &&
               Equals(Info, other.Info) &&
               DictionaryEquals(Commands, other.Commands) &&
               ListEquals(Options, other.Options);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Opencli,
            Info,
            Commands?.Count ?? 0,
            Options?.Count ?? 0);
    }

    internal static bool DictionaryEquals<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue>? a,
        IReadOnlyDictionary<TKey, TValue>? b)
        where TKey : notnull
        where TValue : IEquatable<TValue>
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return a is null && b is null;
        if (a.Count != b.Count) return false;

        foreach (KeyValuePair<TKey, TValue> kvp in a)
        {
            if (!b.TryGetValue(kvp.Key, out TValue? bValue)) return false;
            if (!EqualityComparer<TValue>.Default.Equals(kvp.Value, bValue)) return false;
        }
        return true;
    }

    internal static bool ListEquals<T>(IReadOnlyList<T>? a, IReadOnlyList<T>? b)
        where T : IEquatable<T>
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return a is null && b is null;
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(a[i], b[i])) return false;
        }
        return true;
    }
}

public sealed record OpenCliInfo(
    string? Title,
    string? Version,
    string? Description
) : IEquatable<OpenCliInfo>
{
    public bool Equals(OpenCliInfo? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Title == other.Title &&
               Version == other.Version &&
               Description == other.Description;
    }

    public override int GetHashCode() => HashCode.Combine(Title, Version, Description);
}

public sealed record OpenCliCommand(
    string? Description,
    IReadOnlyList<OpenCliArgument>? Arguments,
    IReadOnlyList<OpenCliOption>? Options,
    IReadOnlyDictionary<string, OpenCliCommand>? Commands,
    IReadOnlyList<OpenCliExitCode>? ExitCodes,
    IReadOnlyList<OpenCliExample>? Examples
) : IEquatable<OpenCliCommand>
{
    public bool Equals(OpenCliCommand? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Description == other.Description &&
               OpenCliSpec.ListEquals(Arguments, other.Arguments) &&
               OpenCliSpec.ListEquals(Options, other.Options) &&
               OpenCliSpec.DictionaryEquals(Commands, other.Commands) &&
               OpenCliSpec.ListEquals(ExitCodes, other.ExitCodes) &&
               OpenCliSpec.ListEquals(Examples, other.Examples);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Description,
            Arguments?.Count ?? 0,
            Options?.Count ?? 0,
            Commands?.Count ?? 0,
            ExitCodes?.Count ?? 0,
            Examples?.Count ?? 0);
    }
}

public sealed record OpenCliArgument(
    string? Name,
    string? Description,
    bool Required,
    int Ordinal
) : IEquatable<OpenCliArgument>
{
    public bool Equals(OpenCliArgument? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Name == other.Name &&
               Description == other.Description &&
               Required == other.Required &&
               Ordinal == other.Ordinal;
    }

    public override int GetHashCode() => HashCode.Combine(Name, Description, Required, Ordinal);
}

public sealed record OpenCliOption(
    string? Name,
    IReadOnlyList<string>? Aliases,
    string? Description,
    IReadOnlyList<OpenCliArgument>? Arguments
) : IEquatable<OpenCliOption>
{
    public bool Equals(OpenCliOption? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Name == other.Name &&
               Description == other.Description &&
               OpenCliSpec.ListEquals(Arguments, other.Arguments) &&
               AliasesEqual(Aliases, other.Aliases);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Name,
            Description,
            Arguments?.Count ?? 0,
            Aliases?.Count ?? 0);
    }

    private static bool AliasesEqual(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return a is null && b is null;
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }
}

public sealed record OpenCliExitCode(
    int Code,
    string? Description
) : IEquatable<OpenCliExitCode>
{
    public bool Equals(OpenCliExitCode? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Code == other.Code && Description == other.Description;
    }

    public override int GetHashCode() => HashCode.Combine(Code, Description);
}

public sealed record OpenCliExample(
    string? Command,
    string? Description
) : IEquatable<OpenCliExample>
{
    public bool Equals(OpenCliExample? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Command == other.Command && Description == other.Description;
    }

    public override int GetHashCode() => HashCode.Combine(Command, Description);
}