using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OpenCliToMcp.Generator;

/// <summary>
/// A simple JSON parser for OpenCLI specifications.
/// This avoids external dependencies in the source generator.
/// </summary>
internal static class SimpleJsonParser
{
    public static JsonValue Parse(string json)
    {
        JsonReader reader = new(json);
        return ParseValue(reader);
    }

    private static JsonValue ParseValue(JsonReader reader)
    {
        reader.SkipWhitespace();
        
        char c = reader.Peek();
        return c switch
        {
            '{' => ParseObject(reader),
            '[' => ParseArray(reader),
            '"' => new JsonValue { Type = JsonValueType.String, StringValue = ParseString(reader) },
            't' or 'f' => new JsonValue { Type = JsonValueType.Boolean, BooleanValue = ParseBoolean(reader) },
            'n' => ParseNull(reader),
            _ when char.IsDigit(c) || c == '-' => new JsonValue { Type = JsonValueType.Number, NumberValue = ParseNumber(reader) },
            _ => throw new InvalidOperationException($"Unexpected character '{c}' at position {reader.Position}")
        };
    }

    private static JsonValue ParseObject(JsonReader reader)
    {
        Dictionary<string, JsonValue> obj = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
        reader.Read(); // consume '{'
        reader.SkipWhitespace();

        while (reader.Peek() != '}')
        {
            string key = ParseString(reader);
            reader.SkipWhitespace();
            
            if (reader.Read() != ':')
                throw new InvalidOperationException($"Expected ':' after key '{key}'");
            
            reader.SkipWhitespace();
            JsonValue value = ParseValue(reader);
            obj[key] = value;
            
            reader.SkipWhitespace();
            char c = reader.Peek();
            if (c == ',')
            {
                reader.Read();
                reader.SkipWhitespace();
            }
            else if (c != '}')
            {
                throw new InvalidOperationException($"Expected ',' or '}}' but found '{c}'");
            }
        }

        reader.Read(); // consume '}'
        return new JsonValue { Type = JsonValueType.Object, ObjectValue = obj };
    }

    private static JsonValue ParseArray(JsonReader reader)
    {
        List<JsonValue> array = [];
        reader.Read(); // consume '['
        reader.SkipWhitespace();

        while (reader.Peek() != ']')
        {
            array.Add(ParseValue(reader));
            reader.SkipWhitespace();
            
            char c = reader.Peek();
            if (c == ',')
            {
                reader.Read();
                reader.SkipWhitespace();
            }
            else if (c != ']')
            {
                throw new InvalidOperationException($"Expected ',' or ']' but found '{c}'");
            }
        }

        reader.Read(); // consume ']'
        return new JsonValue { Type = JsonValueType.Array, ArrayValue = array };
    }

    private static string ParseString(JsonReader reader)
    {
        if (reader.Read() != '"')
            throw new InvalidOperationException("Expected '\"'");

        // Try to find the end quote to estimate string length
        int startPos = reader.Position;
        int endPos = startPos;
        bool hasEscapes = false;
        
        // Scan ahead to find string length and check for escapes
        while (endPos < reader._json.Length)
        {
            if (reader._json[endPos] == '\\')
            {
                hasEscapes = true;
                endPos++; // Skip the escaped character
                if (endPos >= reader._json.Length)
                    throw new InvalidOperationException("Unterminated string");
            }
            else if (reader._json[endPos] == '"')
            {
                break; // Found unescaped quote
            }
            endPos++;
        }
        
        if (endPos >= reader._json.Length)
            throw new InvalidOperationException("Unterminated string");
        
        // For simple strings without escapes, return substring directly
        if (!hasEscapes)
        {
            string result = reader._json.Substring(startPos, endPos - startPos);
            reader._position = endPos + 1; // Skip past the closing quote
            return result;
        }
        
        // For strings with escapes, use StringBuilder with estimated capacity
        StringBuilder sb = new(endPos - startPos);
        reader._position = startPos; // Reset to start of string content
        
        while (true)
        {
            char c = reader.Read();
            if (c == '"')
                break;
            if (c == '\\')
            {
                char escaped = reader.Read();
                c = escaped switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    '/' => '/',
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'u' => ParseUnicode(reader),
                    _ => throw new InvalidOperationException($"Invalid escape sequence '\\{escaped}'")
                };
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static char ParseUnicode(JsonReader reader)
    {
        string hex = new string([reader.Read(), reader.Read(), reader.Read(), reader.Read()]);
        return (char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static bool ParseBoolean(JsonReader reader)
    {
        char first = reader.Read();
        if (first == 't')
        {
            if (reader.Read() == 'r' && reader.Read() == 'u' && reader.Read() == 'e')
                return true;
        }
        else if (first == 'f')
        {
            if (reader.Read() == 'a' && reader.Read() == 'l' && reader.Read() == 's' && reader.Read() == 'e')
                return false;
        }
        throw new InvalidOperationException("Invalid boolean value");
    }

    private static double ParseNumber(JsonReader reader)
    {
        StringBuilder sb = new();
        char c = reader.Peek();
        
        // Optional minus
        if (c == '-')
        {
            sb.Append(reader.Read());
            c = reader.Peek();
        }

        // Integer part
        if (c == '0')
        {
            sb.Append(reader.Read());
        }
        else if (char.IsDigit(c))
        {
            while (char.IsDigit(reader.Peek()))
            {
                sb.Append(reader.Read());
            }
        }
        else
        {
            throw new InvalidOperationException("Invalid number format");
        }

        // Fractional part
        if (reader.Peek() == '.')
        {
            sb.Append(reader.Read());
            if (!char.IsDigit(reader.Peek()))
                throw new InvalidOperationException("Invalid number format");
            while (char.IsDigit(reader.Peek()))
            {
                sb.Append(reader.Read());
            }
        }

        // Exponent part
        c = reader.Peek();
        if (c == 'e' || c == 'E')
        {
            sb.Append(reader.Read());
            c = reader.Peek();
            if (c == '+' || c == '-')
            {
                sb.Append(reader.Read());
            }
            if (!char.IsDigit(reader.Peek()))
                throw new InvalidOperationException("Invalid number format");
            while (char.IsDigit(reader.Peek()))
            {
                sb.Append(reader.Read());
            }
        }

        return double.Parse(sb.ToString(), CultureInfo.InvariantCulture);
    }

    private static JsonValue ParseNull(JsonReader reader)
    {
        if (reader.Read() == 'n' && reader.Read() == 'u' && reader.Read() == 'l' && reader.Read() == 'l')
            return new JsonValue { Type = JsonValueType.Null };
        throw new InvalidOperationException("Invalid null value");
    }

    private class JsonReader
    {
        internal readonly string _json;
        internal int _position;

        public JsonReader(string json)
        {
            _json = json;
            _position = 0;
        }

        public int Position => _position;

        public char Peek()
        {
            if (_position >= _json.Length)
                throw new InvalidOperationException("Unexpected end of JSON");
            return _json[_position];
        }

        public char Read()
        {
            char c = Peek();
            _position++;
            return c;
        }

        public void SkipWhitespace()
        {
            while (_position < _json.Length && char.IsWhiteSpace(_json[_position]))
            {
                _position++;
            }
        }
    }
}

internal class JsonValue
{
    public JsonValueType Type { get; set; }
    public Dictionary<string, JsonValue>? ObjectValue { get; set; }
    public List<JsonValue>? ArrayValue { get; set; }
    public string? StringValue { get; set; }
    public double NumberValue { get; set; }
    public bool BooleanValue { get; set; }

    public bool IsNull => Type == JsonValueType.Null;

    public string? GetString() => Type == JsonValueType.String ? StringValue : null;
    
    public bool? GetBoolean() => Type == JsonValueType.Boolean ? BooleanValue : null;
    
    public int? GetInt32() => Type == JsonValueType.Number ? (int)NumberValue : null;
    
    public double? GetDouble() => Type == JsonValueType.Number ? NumberValue : null;

    public JsonValue? GetProperty(string name)
    {
        if (Type != JsonValueType.Object || ObjectValue == null)
            return null;
        return ObjectValue.TryGetValue(name, out JsonValue? value) ? value : null;
    }

    public IEnumerable<JsonValue> EnumerateArray()
    {
        if (Type != JsonValueType.Array || ArrayValue == null)
            yield break;
        foreach (JsonValue? item in ArrayValue)
            yield return item;
    }

    public IEnumerable<KeyValuePair<string, JsonValue>> EnumerateObject()
    {
        if (Type != JsonValueType.Object || ObjectValue == null)
            yield break;
        foreach (KeyValuePair<string, JsonValue> kvp in ObjectValue)
            yield return kvp;
    }
}

internal enum JsonValueType
{
    Null,
    Boolean,
    Number,
    String,
    Object,
    Array
}