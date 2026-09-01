// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using System.Text.RegularExpressions;
using System.Text.Json.Nodes;

namespace WattsProtocol.Cli;

/// <summary>Severity levels returned by the context-integrity rules engine.</summary>
public enum FindingSeverity
{
    /// <summary>An advisory condition that may merit review.</summary>
    Warning,

    /// <summary>A detected context-integrity risk.</summary>
    Finding,
}

/// <summary>A deterministic rules-engine result.</summary>
public sealed record RuleFinding(FindingSeverity Severity, string Rule, string Message, string Location);

/// <summary>Checks governed context for common v1.2 integrity risks.</summary>
public sealed partial class RulesEngine
{
    private static readonly string[] ActiveCollections =
    [
        "key_facts", "documents_reviewed", "decisions_made", "constraints",
        "open_questions", "risks", "next_actions",
    ];

    /// <summary>
    /// Sections whose items record a settled position rather than an empirical claim. For these,
    /// an explicit <c>authority</c> marker is itself the evidentiary basis, so it satisfies the
    /// evidence-state requirement. A settled decision is grounded by who settled it.
    /// </summary>
    private static readonly string[] AuthorityGroundedSections =
    [
        "decisions_made", "constraints", "next_actions",
    ];

    /// <summary>
    /// Sections exempt from <c>missing-authority-marker</c>. An open question has, by definition,
    /// no source, approval, or verification behind it; requiring one makes the rule unsatisfiable.
    /// </summary>
    private static readonly string[] AuthorityExemptSections =
    [
        "open_questions",
    ];

    /// <summary>
    /// Minimum shared content terms before a restatement is even considered for the
    /// low-confidence-promotion rule.
    /// </summary>
    private const int MinSharedContentTerms = 2;

    /// <summary>
    /// Minimum share of the smaller item's content terms that must overlap. Incidental vocabulary
    /// overlap between two long, unrelated statements scores low here; a genuine restatement of
    /// the same claim scores near 1.0. This ratio, not the raw count, is what separates them.
    /// </summary>
    private const double MinTopicOverlapRatio = 0.6;

    /// <summary>
    /// Overlap ratio required when the only assertive signal is a bare copula (is/are/was/were).
    /// Copulas appear in most well-formed sentences, so they are weak evidence of promotion and
    /// demand near-total topical agreement before a finding is raised.
    /// </summary>
    private const double MinCopulaOverlapRatio = 0.75;

    private static readonly HashSet<string> AuthorityFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "authority", "source", "approved_by", "approval", "verification_status", "evidence_basis",
    };

    /// <summary>Checks a capsule and returns warnings and findings without changing it.</summary>
    public IReadOnlyList<RuleFinding> Check(JsonObject capsule, DateOnly? today = null)
    {
        var results = new List<RuleFinding>();
        var effectiveToday = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var items = EnumerateItems(capsule).ToList();

        foreach (var item in items)
        {
            CheckPerceptualAttribution(item, results);
            CheckSupersession(item, results);
            CheckStaleness(item, effectiveToday, results);
        }

        CheckEvidenceAndAuthority(items, results);
        CheckLowConfidencePromotion(items, results);
        return results;
    }

    /// <summary>Checks unstructured session text by treating each sentence as active, unlabeled state.</summary>
    public IReadOnlyList<RuleFinding> CheckText(string sessionText, DateOnly? today = null)
    {
        var items = new JsonArray();
        foreach (var sentence in SessionSentenceRegex().Split(sessionText))
        {
            var trimmed = sentence.Trim();
            if (trimmed.Length > 0)
            {
                items.Add(trimmed);
            }
        }

        return Check(new JsonObject { ["key_facts"] = items }, today);
    }

    private static IEnumerable<ContextItem> EnumerateItems(JsonObject capsule)
    {
        foreach (var pair in capsule)
        {
            if (pair.Value is JsonArray array)
            {
                for (var index = 0; index < array.Count; index++)
                {
                    yield return ContextItem.Create($"{pair.Key}[{index}]", pair.Key, array[index]);
                }
            }
            else if (pair.Value is not null)
            {
                yield return ContextItem.Create(pair.Key, pair.Key, pair.Value);
            }
        }
    }

    private static void CheckPerceptualAttribution(ContextItem item, ICollection<RuleFinding> results)
    {
        if (FalsePerceptualAttributionRegex().IsMatch(item.Text))
        {
            results.Add(new RuleFinding(
                FindingSeverity.Finding,
                "false-perceptual-attribution",
                "Perceptual language implies access to evidence that may not have been supplied.",
                item.Location));
        }
    }

    private static void CheckSupersession(ContextItem item, ICollection<RuleFinding> results)
    {
        if (ActiveCollections.Contains(item.Section, StringComparer.OrdinalIgnoreCase) &&
            (SupersededRegex().IsMatch(item.Text) || IsStatus(item.Node, "superseded") || IsStatus(item.Node, "obsolete")))
        {
            results.Add(new RuleFinding(
                FindingSeverity.Finding,
                "superseded-item-active",
                "An item marked superseded or obsolete remains in an active capsule section.",
                item.Location));
        }
    }

    private static void CheckStaleness(ContextItem item, DateOnly today, ICollection<RuleFinding> results)
    {
        if (StaleRegex().IsMatch(item.Text) || IsStatus(item.Node, "expired") || IsStatus(item.Node, "stale"))
        {
            results.Add(new RuleFinding(
                FindingSeverity.Finding,
                "stale-or-expired-item",
                "An item is marked stale or expired and should not remain active without review.",
                item.Location));
            return;
        }

        if (item.Node is JsonObject obj &&
            TryReadDate(obj, "expires_on", out var expiresOn) &&
            expiresOn < today)
        {
            results.Add(new RuleFinding(
                FindingSeverity.Finding,
                "stale-or-expired-item",
                $"The item expired on {expiresOn:yyyy-MM-dd}.",
                item.Location));
        }
    }

    private static void CheckEvidenceAndAuthority(
        IEnumerable<ContextItem> items,
        ICollection<RuleFinding> results)
    {
        foreach (var item in items.Where(item =>
                     ActiveCollections.Contains(item.Section, StringComparer.OrdinalIgnoreCase) &&
                     !string.IsNullOrWhiteSpace(item.Text)))
        {
            // In authority-grounded sections a settled decision's evidentiary basis is its
            // authority, so an explicit authority marker satisfies evidence-state on its own.
            var authorityGrounds = AuthorityGroundedSections.Contains(item.Section, StringComparer.OrdinalIgnoreCase) &&
                item.HasAuthorityProperty;

            if (!item.HasEvidenceState && !authorityGrounds)
            {
                results.Add(new RuleFinding(
                    FindingSeverity.Warning,
                    "unlabeled-evidence-state",
                    "Active state has no explicit evidence-state classification.",
                    item.Location));
            }

            // open_questions cannot satisfy this rule by definition, so it is not applied there.
            if (!item.HasAuthorityMarker &&
                !AuthorityExemptSections.Contains(item.Section, StringComparer.OrdinalIgnoreCase))
            {
                results.Add(new RuleFinding(
                    FindingSeverity.Warning,
                    "missing-authority-marker",
                    "Active state has no source, approval, verification, or authority marker.",
                    item.Location));
            }
        }
    }

    private static void CheckLowConfidencePromotion(
        IReadOnlyCollection<ContextItem> items,
        ICollection<RuleFinding> results)
    {
        // Only governed state can be promoted. Scalar header fields such as session_name,
        // project_name, and active_objective state intent, not evidence: an objective that names
        // the same subject as a hypothesis is not a restatement of it. v1.2.0 scanned them and
        // reported active_objective as the promotion site four times on a single capsule.
        var governed = items
            .Where(item => ActiveCollections.Contains(item.Section, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // Match on the claim's prose only. Metadata keys and label values are not the claim.
        var hedged = governed.Where(item => HedgeRegex().IsMatch(item.ClaimText)).ToList();

        // A restatement that carries its own recorded validation is not an unvalidated promotion,
        // which is precisely what this rule reports. Excluding those removes the largest source of
        // false positives on well-labelled capsules.
        var assertive = governed
            .Where(item => !HedgeRegex().IsMatch(item.ClaimText) && !item.HasRecordedValidation)
            .Select(item => (Item: item, Strong: StrongAssertionRegex().IsMatch(item.ClaimText), Copula: CopulaRegex().IsMatch(item.ClaimText)))
            .Where(candidate => candidate.Strong || candidate.Copula)
            .ToList();

        foreach (var tentative in hedged)
        {
            var tentativeTerms = ContentTerms(tentative.ClaimText).ToList();
            if (tentativeTerms.Count == 0)
            {
                continue;
            }

            (ContextItem Item, double Ratio, int Shared)? best = null;

            foreach (var candidate in assertive)
            {
                if (candidate.Item.Location == tentative.Location)
                {
                    continue;
                }

                var candidateTerms = ContentTerms(candidate.Item.ClaimText).ToList();
                if (candidateTerms.Count == 0)
                {
                    continue;
                }

                var shared = tentativeTerms
                    .Intersect(candidateTerms, StringComparer.OrdinalIgnoreCase)
                    .Count();

                if (shared < MinSharedContentTerms)
                {
                    continue;
                }

                // Measure against the smaller item so a short restatement of a long hedged claim
                // is still caught, while two long statements sharing a few words are not.
                var ratio = (double)shared / Math.Min(tentativeTerms.Count, candidateTerms.Count);
                var required = candidate.Strong ? MinTopicOverlapRatio : MinCopulaOverlapRatio;

                if (ratio >= required && (best is null || ratio > best.Value.Ratio))
                {
                    best = (candidate.Item, ratio, shared);
                }
            }

            if (best is not null)
            {
                // Location names the hedged item that is at risk of being promoted; the message
                // names the assertive restatement. The v1.2.0 build had these reversed.
                results.Add(new RuleFinding(
                    FindingSeverity.Finding,
                    "low-confidence-state-promotion",
                    $"Hedged state appears to be restated as fact at {best.Value.Item.Location} without recorded validation.",
                    tentative.Location));
            }
        }
    }

    private static IEnumerable<string> ContentTerms(string text) =>
        WordRegex().Matches(text)
            .Select(match => match.Value.ToLowerInvariant())
            .Where(word => word.Length > 2 && !StopWords.Contains(word))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static bool IsStatus(JsonNode? node, string expected)
    {
        return node is JsonObject obj &&
            obj.TryGetPropertyValue("status", out var status) &&
            string.Equals(status?.GetValue<string>(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadDate(JsonObject obj, string field, out DateOnly date)
    {
        date = default;
        return obj.TryGetPropertyValue(field, out var node) &&
            DateOnly.TryParse(node?.GetValue<string>(), out date);
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "that", "this", "with", "from", "have", "has", "was", "were", "are", "is",
        "for", "not", "may", "might", "could", "would", "should", "appears", "appear", "likely",
        "perhaps", "maybe", "seems", "seem", "been", "being", "into", "than", "then", "they",
    };

    [GeneratedRegex(@"\b(i can see|i see|i heard|i can hear|the document shows|the measurement confirms)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FalsePerceptualAttributionRegex();

    // Both of the following require an actual marking, not a passing mention. "Update the stale
    // v1.1 block" describes staleness; it does not declare the item itself stale. Matching a bare
    // occurrence made these rules fire on any capsule that discussed its own hygiene, which is the
    // same prose-versus-metadata confusion that produced the promotion false positives.
    [GeneratedRegex(
        @"\b(?:marked|flagged|status|state|is|are|was|were|now|considered|deemed)\s+(?:as\s+)?(?:superseded|obsolete)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SupersededRegex();

    [GeneratedRegex(
        @"\b(?:(?:marked|flagged|status|state|is|are|was|were|now|considered|deemed)\s+(?:as\s+)?(?:stale|expired)|expire[sd]?\s+(?:on\s+)?\d{4}-\d{2}-\d{2})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StaleRegex();

    [GeneratedRegex(@"\b(maybe|perhaps|appears?|seems?|likely|unlikely|might|could|possibly|assum(?:e|ed|ption)|hypothes(?:is|ize|ized)|preliminary|tentative)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HedgeRegex();

    // Explicit assertions of settled truth. These are strong evidence that a claim is being
    // presented as established, so they clear a lower topical-overlap bar.
    [GeneratedRegex(@"\b(will be|confirmed|confirms|proven|proves|verified|validated|established|demonstrated|definitely|certainly|conclusively|now known)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StrongAssertionRegex();

    // Bare copulas. Present in most declarative sentences, so on their own they are weak evidence
    // of promotion and are held to a much higher topical-overlap bar.
    [GeneratedRegex(@"\b(is|are|was|were)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CopulaRegex();

    [GeneratedRegex(@"[A-Za-z][A-Za-z0-9_-]*", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"(?<=[.!?])\s+|\r?\n+", RegexOptions.CultureInvariant)]
    private static partial Regex SessionSentenceRegex();

    private sealed partial class ContextItem
    {
        private ContextItem(string location, string section, JsonNode? node, string text)
        {
            Location = location;
            Section = section;
            Node = node;
            Text = text;
        }

        public string Location { get; }

        public string Section { get; }

        public JsonNode? Node { get; }

        public string Text { get; }

        /// <summary>
        /// The claim's own prose, without its metadata. <see cref="Text"/> serialises a structured
        /// item to JSON, so every labelled item shares the tokens "text", "state_type", "source",
        /// and "confidence" with every other one. Comparing those as content terms is what made
        /// the promotion rule fire on unrelated statements in v1.2.0.
        /// </summary>
        public string ClaimText =>
            Node is JsonObject obj &&
            obj.TryGetPropertyValue("text", out var value) &&
            value is JsonValue scalar &&
            scalar.TryGetValue<string>(out var claim)
                ? claim
                : Text;

        public bool HasEvidenceState => HasAnyProperty("state_type", "evidence_state", "confidence", "verification_status", "evidence_basis") ||
            EvidenceMarkerRegex().IsMatch(Text);

        public bool HasAuthorityMarker => HasAnyProperty(AuthorityFields.ToArray()) || AuthorityMarkerRegex().IsMatch(Text);

        /// <summary>Whether the item carries an explicit <c>authority</c> property.</summary>
        public bool HasAuthorityProperty => HasAnyProperty("authority");

        /// <summary>
        /// Whether the item records that its claim was actually validated, as opposed to merely
        /// being labelled. Used to exclude already-validated statements from the
        /// low-confidence-promotion rule.
        /// </summary>
        public bool HasRecordedValidation
        {
            get
            {
                if (Node is not JsonObject obj)
                {
                    return false;
                }

                if (obj.ContainsKey("verification_status") || obj.ContainsKey("evidence_basis"))
                {
                    return true;
                }

                var hasProvenance = AuthorityFields.Any(obj.ContainsKey);
                return hasProvenance &&
                    obj.TryGetPropertyValue("state_type", out var stateType) &&
                    stateType is JsonValue value &&
                    value.TryGetValue<string>(out var text) &&
                    ValidatedStateTypeRegex().IsMatch(text);
            }
        }

        public static ContextItem Create(string location, string section, JsonNode? node) =>
            new(location, section, node, TextOf(node));

        private bool HasAnyProperty(params string[] names)
        {
            return Node is JsonObject obj && names.Any(name => obj.ContainsKey(name));
        }

        private static string TextOf(JsonNode? node)
        {
            return node switch
            {
                null => string.Empty,
                JsonValue value when value.TryGetValue<string>(out var text) => text,
                _ => node.ToJsonString(),
            };
        }

        [GeneratedRegex(@"\b(verified|observed|measured|hypothesis|assumption|inference|expectation|unknown|generated|confidence|evidence)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex EvidenceMarkerRegex();

        [GeneratedRegex(@"\b(source|authority|approved|verified by|evidence basis|owner)\s*[:=]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex AuthorityMarkerRegex();

        [GeneratedRegex(@"^(verified|confirmed|observed|measured|source_document|settled)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ValidatedStateTypeRegex();
    }
}
