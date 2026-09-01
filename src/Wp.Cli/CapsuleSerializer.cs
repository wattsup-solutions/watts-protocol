// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WattsProtocol.Cli;

/// <summary>Reads and writes the portable YAML and JSON forms of a Memory Capsule.</summary>
public sealed class CapsuleSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly IDeserializer yamlDeserializer = new DeserializerBuilder()
        .WithAttemptingUnquotedStringTypeDeserialization()
        .Build();
    private readonly ISerializer yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    /// <summary>Loads a JSON or YAML document based on its file extension.</summary>
    public JsonObject Load(string path)
    {
        var text = File.ReadAllText(path);
        return LoadText(text, Path.GetExtension(path));
    }

    /// <summary>Loads a JSON or YAML document from text.</summary>
    public JsonObject LoadText(string text, string extension)
    {
        if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
        {
            return JsonNode.Parse(text)?.AsObject()
                ?? throw new InvalidDataException("The JSON document must contain an object at its root.");
        }

        if (!IsYamlExtension(extension))
        {
            throw new InvalidDataException("Supported capsule formats are .yaml, .yml, and .json.");
        }

        var yaml = yamlDeserializer.Deserialize<object>(text);
        return ToJsonNode(yaml).AsObject();
    }

    /// <summary>Writes a capsule using the format implied by its file extension.</summary>
    public void Save(JsonObject capsule, string path)
    {
        var extension = Path.GetExtension(path);
        var text = string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase)
            ? capsule.ToJsonString(JsonOptions) + Environment.NewLine
            : IsYamlExtension(extension)
                ? yamlSerializer.Serialize(ToPlainObject(capsule))
                : throw new InvalidDataException("Supported capsule formats are .yaml, .yml, and .json.");
        File.WriteAllText(path, text);
    }

    /// <summary>Serializes a capsule without formatting for copy-paste transport.</summary>
    public string ToMinifiedJson(JsonObject capsule) =>
        capsule.ToJsonString(new JsonSerializerOptions { WriteIndented = false });

    /// <summary>Creates the v1.x template that is useful while still intentionally sparse.</summary>
    public static JsonObject CreateTemplate() =>
        new()
        {
            ["session_name"] = "new-session",
            ["project_name"] = "new-project",
            ["active_objective"] = "State the current objective.",
            ["key_facts"] = new JsonArray(),
            ["documents_reviewed"] = new JsonArray(),
            ["decisions_made"] = new JsonArray(),
            ["constraints"] = new JsonArray(),
            ["open_questions"] = new JsonArray(),
            ["risks"] = new JsonArray(),
            ["next_actions"] = new JsonArray(),
            ["changelog"] = new JsonArray(),
        };

    private static bool IsYamlExtension(string extension) =>
        string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase);

    private static JsonNode ToJsonNode(object? value)
    {
        return value switch
        {
            null => null!,
            IDictionary dictionary => ToJsonObject(dictionary),
            IEnumerable enumerable when value is not string => ToJsonArray(enumerable),
            bool boolean => JsonValue.Create(boolean)!,
            byte number => JsonValue.Create((int)number)!,
            sbyte number => JsonValue.Create((int)number)!,
            short number => JsonValue.Create((int)number)!,
            ushort number => JsonValue.Create((int)number)!,
            int number => JsonValue.Create(number)!,
            uint number => JsonValue.Create(number)!,
            long number => JsonValue.Create(number)!,
            ulong number => JsonValue.Create(number)!,
            float number => JsonValue.Create(number)!,
            double number => JsonValue.Create(number)!,
            decimal number => JsonValue.Create(number)!,
            DateTime dateTime => JsonValue.Create(dateTime.ToString("O", CultureInfo.InvariantCulture))!,
            DateTimeOffset dateTimeOffset => JsonValue.Create(dateTimeOffset.ToString("O", CultureInfo.InvariantCulture))!,
            _ => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture))!,
        };
    }

    private static JsonObject ToJsonObject(IDictionary dictionary)
    {
        var result = new JsonObject();
        foreach (DictionaryEntry entry in dictionary)
        {
            var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture)
                ?? throw new InvalidDataException("YAML mapping keys must be scalar values.");
            result[key] = ToJsonNode(entry.Value);
        }

        return result;
    }

    private static JsonArray ToJsonArray(IEnumerable values)
    {
        var result = new JsonArray();
        foreach (var value in values)
        {
            result.Add(ToJsonNode(value));
        }

        return result;
    }

    private static object? ToPlainObject(JsonNode? node)
    {
        return node switch
        {
            null => null,
            JsonObject obj => obj.ToDictionary(pair => pair.Key, pair => ToPlainObject(pair.Value)),
            JsonArray array => array.Select(ToPlainObject).ToList(),
            JsonValue value => ToScalar(value),
            _ => node.ToJsonString(),
        };
    }

    private static object? ToScalar(JsonValue value)
    {
        if (value.TryGetValue<bool>(out var boolean))
        {
            return boolean;
        }

        if (value.TryGetValue<long>(out var integer))
        {
            return integer;
        }

        if (value.TryGetValue<decimal>(out var decimalNumber))
        {
            return decimalNumber;
        }

        if (value.TryGetValue<double>(out var number))
        {
            return number;
        }

        if (value.TryGetValue<string>(out var text))
        {
            return text;
        }

        return value.ToJsonString();
    }
}
