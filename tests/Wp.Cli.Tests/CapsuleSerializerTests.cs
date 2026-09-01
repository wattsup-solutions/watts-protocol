// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using System.Text.Json.Nodes;
using WattsProtocol.Cli;

namespace Wp.Cli.Tests;

/// <summary>Regression tests for portable YAML and JSON capsule representations.</summary>
public sealed class CapsuleSerializerTests
{
    /// <summary>YAML can be converted to JSON and back without losing document values.</summary>
    [Fact]
    public void YamlJsonYamlRoundTripPreservesCapsuleValues()
    {
        const string yaml = """
            session_name: handoff
            project_name: test-project
            active_objective: Preserve capsule state
            key_facts:
              - text: "A verified fact"
                state_type: verified_fact
                confidence: high
                source: test
            custom_section:
              nested:
                - alpha
                - beta
            """;
        var serializer = new CapsuleSerializer();
        var original = serializer.LoadText(yaml, ".yaml");
        var directory = CreateTemporaryDirectory();
        var jsonPath = Path.Combine(directory, "capsule.json");
        var yamlPath = Path.Combine(directory, "capsule.yaml");

        serializer.Save(original, jsonPath);
        serializer.Save(serializer.Load(jsonPath), yamlPath);
        var roundTripped = serializer.Load(yamlPath);

        Assert.Equal(original.ToJsonString(), roundTripped.ToJsonString());
        Assert.Equal("handoff", roundTripped["session_name"]!.GetValue<string>());
        Assert.Equal("beta", roundTripped["custom_section"]!["nested"]![1]!.GetValue<string>());
    }

    /// <summary>Unknown capsule fields survive a JSON-to-YAML conversion.</summary>
    [Fact]
    public void ConversionPreservesAdditionalDomainSpecificFields()
    {
        var serializer = new CapsuleSerializer();
        var capsule = new JsonObject
        {
            ["session_name"] = "x",
            ["domain_extension"] = new JsonObject { ["owner"] = "human", ["score"] = 4 },
        };
        var directory = CreateTemporaryDirectory();
        var yamlPath = Path.Combine(directory, "capsule.yaml");

        serializer.Save(capsule, yamlPath);
        var converted = serializer.Load(yamlPath);

        Assert.Equal("human", converted["domain_extension"]!["owner"]!.GetValue<string>());
        Assert.Equal(4, converted["domain_extension"]!["score"]!.GetValue<int>());
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "wp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
