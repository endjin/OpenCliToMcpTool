using System;
using System.Collections.Generic;
using System.Text.Json;

namespace OpenCliToMcp.Generator.Models;

// Value-equatable records for proper incremental generator caching
public sealed record OpenCliSpec(
    string Opencli,
    OpenCliInfo Info,
    OpenCliConventions? Conventions,
    IReadOnlyList<OpenCliArgument>? Arguments,
    IReadOnlyList<OpenCliOption>? Options,
    IReadOnlyDictionary<string, OpenCliCommand>? Commands,
    IReadOnlyList<OpenCliExitCode>? ExitCodes,
    IReadOnlyList<string>? Examples,
    bool Interactive,
    IReadOnlyList<OpenCliMetadata>? Metadata
) : IEquatable<OpenCliSpec>
{
    public bool Equals(OpenCliSpec? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Opencli == other.Opencli &&
               Equals(Info, other.Info) &&
               Equals(Conventions, other.Conventions) &&
               ListEquals(Arguments, other.Arguments) &&
               ListEquals(Options, other.Options) &&
               DictionaryEquals(Commands, other.Commands) &&
               ListEquals(ExitCodes, other.ExitCodes) &&
               ListEquals(Examples, other.Examples) &&
               Interactive == other.Interactive &&
               ListEquals(Metadata, other.Metadata);
    }

    public override int GetHashCode()
    {
        int hashCode = -1521134295;
        hashCode = hashCode * -1521134295 + Opencli.GetHashCode();
        hashCode = hashCode * -1521134295 + Info.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<OpenCliConventions?>.Default.GetHashCode(Conventions);
        hashCode = hashCode * -1521134295 + (Arguments?.Count ?? 0);
        hashCode = hashCode * -1521134295 + (Options?.Count ?? 0);
        hashCode = hashCode * -1521134295 + (Commands?.Count ?? 0);
        hashCode = hashCode * -1521134295 + (ExitCodes?.Count ?? 0);
        hashCode = hashCode * -1521134295 + (Examples?.Count ?? 0);
        hashCode = hashCode * -1521134295 + Interactive.GetHashCode();
        hashCode = hashCode * -1521134295 + (Metadata?.Count ?? 0);
        return hashCode;
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
    string Title,
    string? Summary,
    string? Description,
    OpenCliContact? Contact,
    OpenCliLicense? License,
    string Version
) : IEquatable<OpenCliInfo>
{
    public bool Equals(OpenCliInfo? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Title == other.Title &&
               Summary == other.Summary &&
               Description == other.Description &&
               Equals(Contact, other.Contact) &&
               Equals(License, other.License) &&
               Version == other.Version;
    }

    public override int GetHashCode()
    {
        int hashCode = -1521134295;
        hashCode = hashCode * -1521134295 + Title.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Summary);
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Description);
        hashCode = hashCode * -1521134295 + EqualityComparer<OpenCliContact?>.Default.GetHashCode(Contact);
        hashCode = hashCode * -1521134295 + EqualityComparer<OpenCliLicense?>.Default.GetHashCode(License);
        hashCode = hashCode * -1521134295 + Version.GetHashCode();
        return hashCode;
    }
}

public sealed record OpenCliConventions(
    bool GroupOptions,
    string? OptionArgumentSeparator
) : IEquatable<OpenCliConventions>
{
    public bool Equals(OpenCliConventions? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return GroupOptions == other.GroupOptions &&
               OptionArgumentSeparator == other.OptionArgumentSeparator;
    }

    public override int GetHashCode()
    {
        int hashCode = -1521134295;
        hashCode = hashCode * -1521134295 + GroupOptions.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(OptionArgumentSeparator);
        return hashCode;
    }
}

public sealed record OpenCliContact(
    string? Name,
    string? Url,
    string? Email
) : IEquatable<OpenCliContact>
{
    public bool Equals(OpenCliContact? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Name == other.Name && Url == other.Url && Email == other.Email;
    }

    public override int GetHashCode()
    {
        int hashCode = -1521134295;
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Name);
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Url);
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Email);
        return hashCode;
    }
}

public sealed record OpenCliLicense(
    string? Name,
    string? Identifier,
    string? Url
) : IEquatable<OpenCliLicense>
{
    public bool Equals(OpenCliLicense? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Name == other.Name && Identifier == other.Identifier && Url == other.Url;
    }

    public override int GetHashCode()
    {
        int hashCode = -1521134295;
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Name);
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Identifier);
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Url);
        return hashCode;
    }
}

public sealed record OpenCliMetadata(
    string Name,
    JsonElement? Value
) : IEquatable<OpenCliMetadata>
{
    public bool Equals(OpenCliMetadata? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        return Name == other.Name && JsonElementEquals(Value, other.Value);
    }

    public override int GetHashCode()
    {
        int hashCode = -1521134295;
        hashCode = hashCode * -1521134295 + Name.GetHashCode();
        hashCode = hashCode * -1521134295 + (Value?.GetRawText().GetHashCode() ?? 0);
        return hashCode;
    }

    private static bool JsonElementEquals(JsonElement? a, JsonElement? b)
    {
        if (!a.HasValue && !b.HasValue) return true;
        if (!a.HasValue || !b.HasValue) return false;
        return a.Value.GetRawText() == b.Value.GetRawText();
    }
}

public sealed record OpenCliCommand(
    string Name,
    IReadOnlyList<string>? Aliases,
    string? Description,
    IReadOnlyList<OpenCliArgument>? Arguments,
    IReadOnlyList<OpenCliOption>? Options,
    IReadOnlyDictionary<string, OpenCliCommand>? Commands,
    IReadOnlyList<OpenCliExitCode>? ExitCodes,
    bool Hidden,
    IReadOnlyList<string>? Examples,
    bool Interactive,
    IReadOnlyList<OpenCliMetadata>? Metadata
) : IEquatable<OpenCliCommand>
{
    public bool Equals(OpenCliCommand? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Name == other.Name &&
               OpenCliSpec.ListEquals(Aliases, other.Aliases) &&
               Description == other.Description &&
               OpenCliSpec.ListEquals(Arguments, other.Arguments) &&
               OpenCliSpec.ListEquals(Options, other.Options) &&
               OpenCliSpec.DictionaryEquals(Commands, other.Commands) &&
               OpenCliSpec.ListEquals(ExitCodes, other.ExitCodes) &&
               Hidden == other.Hidden &&
               OpenCliSpec.ListEquals(Examples, other.Examples) &&
               Interactive == other.Interactive &&
               OpenCliSpec.ListEquals(Metadata, other.Metadata);
    }

    public override int GetHashCode()
    {
        int hashCode = -1521134295;
        hashCode = hashCode * -1521134295 + Name.GetHashCode();
        hashCode = hashCode * -1521134295 + (Aliases?.Count ?? 0);
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Description);
        hashCode = hashCode * -1521134295 + (Arguments?.Count ?? 0);
        hashCode = hashCode * -1521134295 + (Options?.Count ?? 0);
        hashCode = hashCode * -1521134295 + (Commands?.Count ?? 0);
        hashCode = hashCode * -1521134295 + (ExitCodes?.Count ?? 0);
        hashCode = hashCode * -1521134295 + Hidden.GetHashCode();
        hashCode = hashCode * -1521134295 + (Examples?.Count ?? 0);
        hashCode = hashCode * -1521134295 + Interactive.GetHashCode();
        hashCode = hashCode * -1521134295 + (Metadata?.Count ?? 0);
        return hashCode;
    }
}

public sealed record OpenCliArgument(
    string Name,
    bool Required,
    OpenCliArity? Arity,
    IReadOnlyList<string>? AcceptedValues,
    string? Group,
    string? Description,
    bool Hidden,
    IReadOnlyList<OpenCliMetadata>? Metadata
) : IEquatable<OpenCliArgument>
{
    public bool Equals(OpenCliArgument? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Name == other.Name &&
               Required == other.Required &&
               Equals(Arity, other.Arity) &&
               OpenCliSpec.ListEquals(AcceptedValues, other.AcceptedValues) &&
               Group == other.Group &&
               Description == other.Description &&
               Hidden == other.Hidden &&
               OpenCliSpec.ListEquals(Metadata, other.Metadata);
    }

    public override int GetHashCode()
    {
        int hashCode = -1521134295;
        hashCode = hashCode * -1521134295 + Name.GetHashCode();
        hashCode = hashCode * -1521134295 + Required.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<OpenCliArity?>.Default.GetHashCode(Arity);
        hashCode = hashCode * -1521134295 + (AcceptedValues?.Count ?? 0);
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Group);
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Description);
        hashCode = hashCode * -1521134295 + Hidden.GetHashCode();
        hashCode = hashCode * -1521134295 + (Metadata?.Count ?? 0);
        return hashCode;
    }
}

public sealed record OpenCliArity(
    int Minimum,
    int? Maximum
) : IEquatable<OpenCliArity>
{
    public bool Equals(OpenCliArity? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Minimum == other.Minimum && Maximum == other.Maximum;
    }

    public override int GetHashCode()
    {
        int hashCode = -1521134295;
        hashCode = hashCode * -1521134295 + Minimum;
        hashCode = hashCode * -1521134295 + (Maximum ?? 0);
        return hashCode;
    }
}

public sealed record OpenCliOption(
    string Name,
    bool Required,
    IReadOnlyList<string>? Aliases,
    IReadOnlyList<OpenCliArgument>? Arguments,
    string? Group,
    string? Description,
    bool Recursive,
    bool Hidden,
    IReadOnlyList<OpenCliMetadata>? Metadata
) : IEquatable<OpenCliOption>
{
    public bool Equals(OpenCliOption? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        
        return Name == other.Name &&
               Required == other.Required &&
               OpenCliSpec.ListEquals(Aliases, other.Aliases) &&
               OpenCliSpec.ListEquals(Arguments, other.Arguments) &&
               Group == other.Group &&
               Description == other.Description &&
               Recursive == other.Recursive &&
               Hidden == other.Hidden &&
               OpenCliSpec.ListEquals(Metadata, other.Metadata);
    }

    public override int GetHashCode()
    {
        int hashCode = -1521134295;
        hashCode = hashCode * -1521134295 + Name.GetHashCode();
        hashCode = hashCode * -1521134295 + Required.GetHashCode();
        hashCode = hashCode * -1521134295 + (Aliases?.Count ?? 0);
        hashCode = hashCode * -1521134295 + (Arguments?.Count ?? 0);
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Group);
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Description);
        hashCode = hashCode * -1521134295 + Recursive.GetHashCode();
        hashCode = hashCode * -1521134295 + Hidden.GetHashCode();
        hashCode = hashCode * -1521134295 + (Metadata?.Count ?? 0);
        return hashCode;
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

    public override int GetHashCode()
    {
        int hashCode = -1521134295;
        hashCode = hashCode * -1521134295 + Code;
        hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Description);
        return hashCode;
    }
}

// public sealed record OpenCliExample(
//     string? Command,
//     string? Description
// ) : IEquatable<OpenCliExample>
// {
//     // This record is kept for backward compatibility if needed, but the spec uses string[] for examples.
//     // The generator will need to adapt.
//     public bool Equals(OpenCliExample? other)
//     {
//         if (ReferenceEquals(this, other)) return true;
//         if (other is null) return false;
        
//         return Command == other.Command && Description == other.Description;
//     }

//     public override int GetHashCode() => HashCode.Combine(Command, Description);
// }