using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CompanyWars.Sim;

namespace CompanyWars.Content;

/// <summary>The one <see cref="JsonSerializerOptions"/> for content, snapshots and results.</summary>
public static class ContentJson
{
    public static JsonSerializerOptions Options { get; } = Create(indented: false);

    public static JsonSerializerOptions Indented { get; } = Create(indented: true);

    private static JsonSerializerOptions Create(bool indented)
    {
        var o = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = indented,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            NumberHandling = JsonNumberHandling.Strict,
        };
        o.Converters.Add(new ValueSpecConverter());
        o.Converters.Add(new OverrideToConverter());
        return o;
    }

    /// <summary>Strips properties whose value is JSON null, recursively, so records with optional fields compare equal to their source.</summary>
    public static JsonNode? StripNulls(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                {
                    var result = new JsonObject();
                    foreach (KeyValuePair<string, JsonNode?> kv in obj)
                    {
                        if (kv.Value is null) continue;
                        result[kv.Key] = StripNulls(kv.Value);
                    }
                    return result;
                }
            case JsonArray arr:
                {
                    var result = new JsonArray();
                    foreach (JsonNode? item in arr) result.Add(StripNulls(item));
                    return result;
                }
            default:
                return node?.DeepClone();
        }
    }
}

/// <summary>Reads a <c>value</c> (CONTENT_SCHEMA.md §3.4) as an integer or one of the two object forms.</summary>
public sealed class ValueSpecConverter : JsonConverter<ValueSpec>
{
    public override ValueSpec Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number) return new ValueSpec(reader.GetInt64(), null, null, null, null);
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("value must be an integer or an object");
        long? @base = null;
        string? perTag = null;
        long? each = null;
        long? permilleOfTargetCap = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            string name = reader.GetString() ?? string.Empty;
            reader.Read();
            switch (name)
            {
                case "base": @base = reader.GetInt64(); break;
                case "perTag": perTag = reader.GetString(); break;
                case "each": each = reader.GetInt64(); break;
                case "permilleOfTargetCap": permilleOfTargetCap = reader.GetInt64(); break;
                default: throw new JsonException($"unknown value field {name}");
            }
        }
        return new ValueSpec(null, @base, perTag, each, permilleOfTargetCap);
    }

    public override void Write(Utf8JsonWriter writer, ValueSpec value, JsonSerializerOptions options)
    {
        if (value.Constant.HasValue)
        {
            writer.WriteNumberValue(value.Constant.Value);
            return;
        }
        writer.WriteStartObject();
        if (value.PermilleOfTargetCap.HasValue)
        {
            writer.WriteNumber("permilleOfTargetCap", value.PermilleOfTargetCap.Value);
        }
        else
        {
            writer.WriteNumber("base", value.Base ?? 0);
            writer.WriteString("perTag", value.PerTag);
            writer.WriteNumber("each", value.Each ?? 0);
        }
        writer.WriteEndObject();
    }
}

/// <summary>Reads an override's <c>to</c> as a selector name or a tier number.</summary>
public sealed class OverrideToConverter : JsonConverter<OverrideTo>
{
    public override OverrideTo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String) return new OverrideTo(reader.GetString(), null);
        if (reader.TokenType == JsonTokenType.Number) return new OverrideTo(null, reader.GetInt64());
        throw new JsonException("to must be a string or an integer");
    }

    public override void Write(Utf8JsonWriter writer, OverrideTo value, JsonSerializerOptions options)
    {
        if (value.Name != null) writer.WriteStringValue(value.Name);
        else writer.WriteNumberValue(value.Tier ?? 0);
    }
}
