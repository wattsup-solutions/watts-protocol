// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using System.Text.Json.Nodes;

namespace WattsProtocol.Cli;

/// <summary>Validates the flexible, recommended v1.x Memory Capsule structure.</summary>
public sealed class CapsuleValidator
{
    /// <summary>The recommended v1.x fields, all intentionally warning-level when absent.</summary>
    public static readonly string[] RecommendedFields =
    [
        "session_name",
        "project_name",
        "active_objective",
        "key_facts",
        "documents_reviewed",
        "decisions_made",
        "constraints",
        "open_questions",
        "risks",
        "next_actions",
        "changelog",
    ];

    /// <summary>Returns errors and advisory warnings for a capsule.</summary>
    public ValidationResult Validate(JsonObject capsule)
    {
        var result = new ValidationResult();
        foreach (var field in RecommendedFields)
        {
            if (!capsule.TryGetPropertyValue(field, out var value) || value is null)
            {
                result.Warnings.Add($"Recommended field '{field}' is absent.");
                continue;
            }

            if (IsCollectionField(field) && value is not JsonArray)
            {
                result.Errors.Add($"Field '{field}' should be a list when present.");
            }
            else if (!IsCollectionField(field) && value is JsonArray or JsonObject)
            {
                result.Errors.Add($"Field '{field}' should be a scalar value when present.");
            }
        }

        if (capsule.Count == 0)
        {
            result.Errors.Add("The capsule cannot be empty.");
        }

        return result;
    }

    private static bool IsCollectionField(string field) =>
        field is "key_facts" or "documents_reviewed" or "decisions_made" or "constraints" or
            "open_questions" or "risks" or "next_actions" or "changelog";
}

/// <summary>Collects validation diagnostics without forcing a rigid schema.</summary>
public sealed class ValidationResult
{
    /// <summary>Structural errors.</summary>
    public List<string> Errors { get; } = [];

    /// <summary>Useful-state advisory warnings.</summary>
    public List<string> Warnings { get; } = [];

    /// <summary>Whether the capsule has no structural errors.</summary>
    public bool IsValid => Errors.Count == 0;
}
