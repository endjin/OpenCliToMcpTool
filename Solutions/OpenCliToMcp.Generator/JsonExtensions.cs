using System.Text.Json;

namespace OpenCliToMcp.Generator;

internal static class JsonExtensions
{
    public static JsonElement? GetPropertyOrNull(this JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out JsonElement value))
        {
            return value;
        }
        return null;
    }

    public static string? GetString(this JsonElement? element)
    {
        if (element.HasValue && element.Value.ValueKind == JsonValueKind.String)
        {
            return element.Value.GetString();
        }
        return null;
    }

    public static bool? GetBoolean(this JsonElement? element)
    {
        if (element.HasValue && (element.Value.ValueKind == JsonValueKind.True || element.Value.ValueKind == JsonValueKind.False))
        {
            return element.Value.GetBoolean();
        }
        return null;
    }

    public static int? GetInt32(this JsonElement? element)
    {
        if (element.HasValue && element.Value.ValueKind == JsonValueKind.Number && element.Value.TryGetInt32(out int value))
        {
            return value;
        }
        return null;
    }
}
