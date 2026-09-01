// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using System.Text;
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

    /// <summary>
    /// In decision-type sections an authority marker is the evidentiary basis, so it satisfies
    /// evidence-state on its own. v1.2.0 raised 17 warnings on a capsule whose decisions,
    /// constraints, and next actions were all properly attributed.
    /// </summary>
    [Theory]
    [InlineData("decisions_made")]
    [InlineData("constraints")]
    [InlineData("next_actions")]
    public void AuthorityGroundsEvidenceStateInDecisionSections(string section)
    {
        var capsule = new JsonObject
        {
            [section] = new JsonArray(new JsonObject
            {
                ["text"] = "Use YAML or JSON for portable Memory Capsules.",
                ["authority"] = "settled_decision",
            }),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 9, 1));

        Assert.DoesNotContain(findings, finding => finding.Rule == "unlabeled-evidence-state");
    }

    /// <summary>
    /// An open question has no source, approval, or verification by definition, so
    /// missing-authority-marker is unsatisfiable there and must not be applied.
    /// </summary>
    [Fact]
    public void OpenQuestionsAreExemptFromTheAuthorityMarkerRule()
    {
        var capsule = new JsonObject
        {
            ["open_questions"] = new JsonArray(new JsonObject
            {
                ["text"] = "Does the release ship before or after the article?",
                ["state_type"] = "open_question",
            }),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 9, 1));

        Assert.DoesNotContain(findings, finding => finding.Rule == "missing-authority-marker");
    }

    /// <summary>
    /// Two long, unrelated statements that happen to share a couple of words are not a promotion.
    /// This is the false positive that made the rule untrustworthy in v1.2.0.
    /// </summary>
    [Fact]
    public void DoesNotFlagIncidentalVocabularyOverlapAsPromotion()
    {
        var capsule = new JsonObject
        {
            ["key_facts"] = new JsonArray(
                "Paige appears as a recurring recipient on project email alongside Ben and Craig.",
                "The consulting agreement with Ben covers association selection work and renews monthly."),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 9, 1));

        Assert.DoesNotContain(findings, finding => finding.Rule == "low-confidence-state-promotion");
    }

    /// <summary>
    /// The finding locates the hedged item at risk of promotion and names the restatement in the
    /// message. v1.2.0 reported these the other way round, which read backwards.
    /// </summary>
    [Fact]
    public void PromotionFindingLocatesTheHedgedItem()
    {
        var capsule = new JsonObject
        {
            ["key_facts"] = new JsonArray(
                "The components appear similar.",
                "The components are similar."),
        };

        var promotion = Assert.Single(
            new RulesEngine().Check(capsule, new DateOnly(2026, 9, 1)),
            finding => finding.Rule == "low-confidence-state-promotion");

        Assert.Equal("key_facts[0]", promotion.Location);
        Assert.Contains("key_facts[1]", promotion.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A restatement that records its own validation is not an unvalidated promotion, which is
    /// exactly what the rule's message asserts.
    /// </summary>
    [Fact]
    public void DoesNotFlagPromotionWhenTheRestatementRecordsValidation()
    {
        var capsule = new JsonObject
        {
            ["key_facts"] = new JsonArray(
                new JsonObject
                {
                    ["text"] = "The components appear similar.",
                    ["state_type"] = "hypothesis",
                    ["source"] = "analysis",
                },
                new JsonObject
                {
                    ["text"] = "The components are similar.",
                    ["state_type"] = "verified_fact",
                    ["source"] = "bench-measurement-2026-09-01",
                }),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 9, 1));

        Assert.DoesNotContain(findings, finding => finding.Rule == "low-confidence-state-promotion");
    }

    /// <summary>
    /// Scalar header fields state intent, not evidence. v1.2.0 named active_objective as the
    /// promotion site four times on a single capsule.
    /// </summary>
    [Fact]
    public void IgnoresScalarHeaderFieldsWhenDetectingPromotion()
    {
        var capsule = new JsonObject
        {
            ["active_objective"] = "Determine whether the components are similar.",
            ["key_facts"] = new JsonArray("The components appear similar."),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 9, 1));

        Assert.DoesNotContain(findings, finding => finding.Rule == "low-confidence-state-promotion");
    }

    /// <summary>
    /// The capsule shipped inside the binary must pass the checker shipped inside the same binary.
    /// A reference example that fails its own tool undermines the whole rules engine.
    /// </summary>
    [Fact]
    public void BundledExampleCapsulePassesItsOwnCheck()
    {
        var serializer = new CapsuleSerializer();

        foreach (var name in new[] { "example-capsule.yaml", "example-capsule.json" })
        {
            var asset = new ProtocolAssetCatalog()
                .GetAssets(ProtocolAssetCatalog.CurrentVersion)
                .Single(candidate => candidate.RelativePath == name);

            var capsule = serializer.LoadText(
                Encoding.UTF8.GetString(asset.Contents),
                Path.GetExtension(name));

            var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 9, 1));

            Assert.True(
                findings.Count == 0,
                $"{name} produced {findings.Count} finding(s): " +
                string.Join("; ", findings.Select(finding => $"{finding.Rule} at {finding.Location}")));
        }
    }

    /// <summary>
    /// The capsule that reproduced the v1.2.0 output defects on a real machine, kept here so the
    /// regression tests below run against a structurally valid capsule that genuinely provokes
    /// them rather than against a minimal hand-built stub.
    ///
    /// Against v1.2.0 this capsule produced 29 findings for 3 distinct problems:
    ///   17 unlabeled-evidence-state — next_actions(7), decisions_made(5), constraints(5),
    ///      every one of which carries an authority marker;
    ///    7 missing-authority-marker — all in open_questions, where the rule is unsatisfiable;
    ///    5 low-confidence-state-promotion — all false positives, four of them near-identical
    ///      rows naming the same location.
    ///
    /// Against v1.2.1 it produces none. Every claim in it is fictional release housekeeping.
    /// </summary>
    private const string ReproductionCapsuleYaml =
        """
        session_name: protocol-release-preparation
        project_name: watts-protocol
        active_objective: Prepare the public release, verify the archive hashes, and decide whether the rules engine is ready for continuous integration use.
        key_facts:
          - text: "The v1.2 baseline is the Generally Accepted 1.x line."
            state_type: verified_fact
            confidence: high
            source: bundled-training
          - text: "The release archive contains forty-nine files."
            state_type: verified_fact
            confidence: high
            source: archive-listing
          - text: "The test suite covers the delivery system, the serializer, and the rules engine."
            state_type: verified_fact
            confidence: high
            source: test-run
          - text: "The published archive hashes have been recomputed from the downloaded asset."
            state_type: verified_fact
            confidence: high
            source: asset-download
          - text: "Two maintainers hold publication rights for the repository."
            state_type: verified_fact
            confidence: medium
            source: repository-settings
          - text: "The delivery system performs no network calls at runtime."
            state_type: verified_fact
            confidence: high
            source: build-notes
          - text: "The single-file build keeps trimming disabled because command discovery uses reflection."
            state_type: verified_fact
            confidence: high
            source: build-notes
          - text: "Six runtime identifiers are documented for publication."
            state_type: verified_fact
            confidence: high
            source: source-readme
          - text: "The short specification ships as an embedded resource."
            state_type: verified_fact
            confidence: high
            source: asset-catalog
          - text: "Downstream forks must carry the notice file forward."
            state_type: verified_fact
            confidence: high
            source: license-terms
          - text: "The trademark is not licensed under the code license."
            state_type: verified_fact
            confidence: high
            source: license-terms
          - text: "Adoption of the capsule format outside the repository has not been measured."
            state_type: unknown
            confidence: low
            source: maintainer-note
          - text: "Reviewers will likely ask for a continuous integration mode before adopting the checker."
            state_type: expectation
            confidence: medium
            source: maintainer-note
          - text: "The archive layout mirrors the repository tree at the tagged commit."
            state_type: assumption
            confidence: medium
            source: maintainer-note
          - text: "Documentation prose and code carry different license terms."
            state_type: hypothesis
            confidence: medium
            source: maintainer-note
        documents_reviewed:
          - text: "Overview and definition document."
            state_type: source_document
            verification_status: reviewed
          - text: "Release checklist."
            state_type: source_document
            verification_status: reviewed
          - text: "Build notes for the delivery system."
            state_type: source_document
            verification_status: reviewed
          - text: "Attribution and provenance page."
            state_type: source_document
            verification_status: reviewed
        decisions_made:
          - text: "Build every release from the tag rather than from the default branch."
            authority: settled_decision
            source: maintainer
          - text: "Ship portable capsules in YAML and JSON."
            authority: settled_decision
            source: specification
          - text: "Keep the delivery system offline by design."
            authority: settled_decision
            source: build-notes
          - text: "Dual-license code and documentation prose separately."
            authority: settled_decision
            source: license-terms
          - text: "Publish a citable archive alongside each tagged release."
            authority: settled_decision
            source: release-checklist
        constraints:
          - text: "Do not promote contextual state beyond its evidentiary basis."
            authority: core-principle
          - text: "Published archives are immutable once their hashes are recorded."
            authority: hard-boundary
          - text: "Exploratory material must never be presented as a released line."
            authority: hard-boundary
          - text: "The trademark and name are reserved."
            authority: legal-requirement
          - text: "Every tagged release regenerates its provenance record."
            authority: operational-requirement
        open_questions:
          - text: "Should the checker gain a quiet mode for automated pipelines?"
            state_type: open_question
          - text: "Does a patch release warrant a fresh archive deposit?"
            state_type: open_question
          - text: "Which runtime identifiers deserve prebuilt binaries?"
            state_type: open_question
          - text: "Should the rules engine expose per-rule severity overrides?"
            state_type: open_question
          - text: "How should downstream forks signal protocol conformance?"
            state_type: open_question
          - text: "What cadence suits the specification prose?"
            state_type: open_question
          - text: "Who reviews changes to the bundled training file?"
            state_type: open_question
        risks:
          - text: "A first-time reader could abandon the project if the first command fails."
            state_type: risk
            confidence: high
            source: maintainer-note
          - text: "A checker that reports one row per item might be ignored in automated pipelines."
            state_type: risk
            confidence: medium
            source: maintainer-note
          - text: "Rewriting a published archive would break every recorded hash."
            state_type: risk
            confidence: high
            source: license-terms
          - text: "Specification prose and implementation can drift apart between releases."
            state_type: risk
            confidence: medium
            source: maintainer-note
          - text: "Reflection-based command discovery breaks under aggressive trimming."
            state_type: risk
            confidence: medium
            source: build-notes
        next_actions:
          - text: "Regenerate the provenance record for the new tag."
            authority: operational-guidance
          - text: "Recompute the archive checksums."
            authority: operational-guidance
          - text: "Attach the built binaries to the release."
            authority: operational-guidance
          - text: "Update the citation metadata."
            authority: operational-guidance
          - text: "Run the full test suite from a clean clone."
            authority: operational-guidance
          - text: "Verify the bundled example passes the checker."
            authority: operational-guidance
          - text: "Announce the release once the archive is live."
            authority: operational-guidance
        changelog:
          - "v1.2: Initial public release."
        """;

    private static JsonObject LoadReproductionCapsule() =>
        new CapsuleSerializer().LoadText(ReproductionCapsuleYaml, ".yaml");

    /// <summary>The reproduction capsule is a well-formed capsule, not a malformed one.</summary>
    [Fact]
    public void ReproductionCapsuleIsAValidCapsule()
    {
        var result = new CapsuleValidator().Validate(LoadReproductionCapsule());

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Warnings);
    }

    /// <summary>
    /// The whole point of the v1.2.1 rules work: a properly labelled capsule reports clean.
    /// This is the single assertion that would have caught all three output defects at once.
    /// </summary>
    [Fact]
    public void ReproductionCapsuleReportsCleanUnderTheFixedRules()
    {
        var findings = new RulesEngine().Check(LoadReproductionCapsule(), new DateOnly(2026, 9, 1));

        Assert.True(
            findings.Count == 0,
            $"expected 0 findings, got {findings.Count}: " +
            string.Join("; ", findings.Select(finding => $"{finding.Rule} at {finding.Location}")));
    }

    /// <summary>
    /// Guards the 17 warnings individually: in the reproduction capsule every decision,
    /// constraint, and next action is authority-grounded, and none may be flagged.
    /// </summary>
    [Fact]
    public void ReproductionCapsuleRaisesNoEvidenceStateWarningsOnAuthorityGroundedSections()
    {
        var findings = new RulesEngine().Check(LoadReproductionCapsule(), new DateOnly(2026, 9, 1));

        Assert.DoesNotContain(
            findings,
            finding => finding.Rule == "unlabeled-evidence-state" &&
                (finding.Location.StartsWith("decisions_made", StringComparison.Ordinal) ||
                 finding.Location.StartsWith("constraints", StringComparison.Ordinal) ||
                 finding.Location.StartsWith("next_actions", StringComparison.Ordinal)));
    }

    /// <summary>Guards the 7 warnings: the capsule's open questions stay exempt.</summary>
    [Fact]
    public void ReproductionCapsuleRaisesNoAuthorityWarningsOnOpenQuestions()
    {
        var findings = new RulesEngine().Check(LoadReproductionCapsule(), new DateOnly(2026, 9, 1));

        Assert.DoesNotContain(
            findings,
            finding => finding.Location.StartsWith("open_questions", StringComparison.Ordinal));
    }

    /// <summary>
    /// Guards the 5 false positives. Their real cause was that a structured item's text is its
    /// JSON serialisation, so every labelled item shared the tokens "text", "state_type",
    /// "source", and "confidence" with every other one and cleared the overlap bar on metadata
    /// alone. The capsule below has 15 key facts and 5 risks and must yield no promotions.
    /// </summary>
    [Fact]
    public void ReproductionCapsuleRaisesNoPromotionFalsePositives()
    {
        var findings = new RulesEngine().Check(LoadReproductionCapsule(), new DateOnly(2026, 9, 1));

        Assert.DoesNotContain(findings, finding => finding.Rule == "low-confidence-state-promotion");
    }

    /// <summary>
    /// Item metadata is not claim content. Two unrelated claims that share only their labels must
    /// not be treated as a restatement of one another.
    /// </summary>
    [Fact]
    public void SharedItemMetadataIsNotTreatedAsSharedClaimContent()
    {
        var capsule = new JsonObject
        {
            ["key_facts"] = new JsonArray(
                new JsonObject
                {
                    ["text"] = "The deposit may land tomorrow.",
                    ["state_type"] = "assumption",
                    ["confidence"] = "low",
                    ["source"] = "maintainer-note",
                },
                new JsonObject
                {
                    ["text"] = "Six runtime identifiers are documented.",
                    ["state_type"] = "verified_fact",
                    ["confidence"] = "high",
                    ["source"] = "source-readme",
                }),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 9, 1));

        Assert.DoesNotContain(findings, finding => finding.Rule == "low-confidence-state-promotion");
    }

    /// <summary>
    /// Describing staleness is not being stale. A capsule that plans to replace an outdated
    /// artifact was flagged as outdated itself, which is the prose-versus-metadata confusion that
    /// also produced the promotion false positives.
    /// </summary>
    [Theory]
    [InlineData("Update the stale v1.1 schema block in the project instructions.")]
    [InlineData("Replace the expired certificate reference in the deployment guide.")]
    [InlineData("Document why the superseded workflow was retired.")]
    [InlineData("Remove the obsolete build script.")]
    public void DoesNotFlagItemsThatMerelyDiscussStaleOrSupersededMaterial(string claim)
    {
        var capsule = new JsonObject
        {
            ["next_actions"] = new JsonArray(new JsonObject
            {
                ["text"] = claim,
                ["authority"] = "operational-guidance",
            }),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 9, 1));

        Assert.DoesNotContain(findings, finding => finding.Rule == "stale-or-expired-item");
        Assert.DoesNotContain(findings, finding => finding.Rule == "superseded-item-active");
    }

    /// <summary>An item that actually declares itself stale or superseded in prose still trips.</summary>
    [Theory]
    [InlineData("This constraint is stale and needs review.", "stale-or-expired-item")]
    [InlineData("The credential expired on 2026-08-01.", "stale-or-expired-item")]
    [InlineData("This approach is now superseded.", "superseded-item-active")]
    [InlineData("The prior decision was marked obsolete.", "superseded-item-active")]
    public void StillFlagsItemsThatDeclareThemselvesStaleOrSuperseded(string claim, string rule)
    {
        var capsule = new JsonObject
        {
            ["constraints"] = new JsonArray(new JsonObject
            {
                ["text"] = claim,
                ["authority"] = "operational-requirement",
            }),
        };

        var findings = new RulesEngine().Check(capsule, new DateOnly(2026, 9, 1));

        Assert.Contains(findings, finding => finding.Rule == rule);
    }
}
