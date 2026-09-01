// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using System.Text.Json.Nodes;
using WattsProtocol.Cli;

namespace Wp.Cli.Tests;

/// <summary>Tests key deterministic context-integrity rules.</summary>
public sealed class RulesEngineTests
{
    /// <summary>Hedged state restated as a fact is identified as a promotion risk.</summary>
    [Fact]
    public void DetectsLowConfidenceStatePromotion()
    {
        var capsule = new JsonObject
        {
            ["key_facts"] = new JsonArray(
                "The components appear similar.",
                "The components are similar."),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 8, 21));

        Assert.Contains(findings, finding => finding.Rule == "low-confidence-state-promotion" &&
            finding.Severity == FindingSeverity.Finding);
    }

    /// <summary>Perceptual claims are surfaced because evidence possession may be implied.</summary>
    [Fact]
    public void DetectsFalsePerceptualAttribution()
    {
        var capsule = new JsonObject
        {
            ["next_actions"] = new JsonArray("I can see the difference, so proceed."),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 8, 21));

        Assert.Contains(findings, finding => finding.Rule == "false-perceptual-attribution" &&
            finding.Severity == FindingSeverity.Finding);
    }

    /// <summary>Superseded state and stale metadata remain findings when left in active sections.</summary>
    [Fact]
    public void DetectsSupersededAndExpiredActiveState()
    {
        var capsule = new JsonObject
        {
            ["decisions_made"] = new JsonArray(new JsonObject
            {
                ["text"] = "Use the original workflow.",
                ["status"] = "superseded",
                ["state_type"] = "decision",
                ["authority"] = "settled",
            }),
            ["constraints"] = new JsonArray(new JsonObject
            {
                ["text"] = "Old constraint",
                ["expires_on"] = "2026-08-20",
                ["state_type"] = "constraint",
                ["authority"] = "human",
            }),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 8, 21));

        Assert.Contains(findings, finding => finding.Rule == "superseded-item-active");
        Assert.Contains(findings, finding => finding.Rule == "stale-or-expired-item");
    }

    /// <summary>Active free text missing state and authority labels receives warning-level results.</summary>
    [Fact]
    public void ReportsMissingEvidenceAndAuthorityMarkersAsWarnings()
    {
        var capsule = new JsonObject
        {
            ["key_facts"] = new JsonArray("A plain active claim."),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 8, 21));

        Assert.Contains(findings, finding => finding.Rule == "unlabeled-evidence-state" &&
            finding.Severity == FindingSeverity.Warning);
        Assert.Contains(findings, finding => finding.Rule == "missing-authority-marker" &&
            finding.Severity == FindingSeverity.Warning);
    }

    /// <summary>Plain session text is checkable without pretending it is already a capsule.</summary>
    [Fact]
    public void ChecksUnstructuredSessionText()
    {
        var findings = new RulesEngine().CheckText(
            "The component appears similar. The component is similar. I can see the difference.",
            new DateOnly(2026, 8, 21));

        Assert.Contains(findings, finding => finding.Rule == "low-confidence-state-promotion");
        Assert.Contains(findings, finding => finding.Rule == "false-perceptual-attribution");
    }
}
