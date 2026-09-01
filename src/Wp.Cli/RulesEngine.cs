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
            if (!item.HasEvidenceState)
            {
                results.Add(new RuleFinding(
                    FindingSeverity.Warning,
                    "unlabeled-evidence-state",
                    "Active state has no explicit evidence-state classification.",
                    item.Location));
            }

            if (!item.HasAuthorityMarker)
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
        var hedged = items.Where(item => HedgeRegex().IsMatch(item.Text)).ToList();
        var assertive = items.Where(item => !HedgeRegex().IsMatch(item.Text) && AssertiveRegex().IsMatch(item.Text)).ToList();

        foreach (var tentative in hedged)
        {
            var tentativeTerms = ContentTerms(tentative.Text);
            if (!tentativeTerms.Any())
            {
                continue;
            }

            var promotion = assertive.FirstOrDefault(item =>
                item.Location != tentative.Location &&
                tentativeTerms.Intersect(ContentTerms(item.Text), StringComparer.OrdinalIgnoreCase).Count() >= 2);

            if (promotion is not null)
            {
                results.Add(new RuleFinding(
                    FindingSeverity.Finding,
                    "low-confidence-state-promotion",
                    $"Hedged state at {tentative.Location} appears to be restated as fact without recorded validation.",
                    promotion.Location));
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

    [GeneratedRegex(@"\b(superseded|obsolete|replaced)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SupersededRegex();

    [GeneratedRegex(@"\b(stale|expired|expires?\s+(?:on\s+)?\d{4}-\d{2}-\d{2})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StaleRegex();

    [GeneratedRegex(@"\b(maybe|perhaps|appears?|seems?|likely|unlikely|might|could|possibly|assum(?:e|ed|ption)|hypothes(?:is|ize|ized)|preliminary|tentative)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HedgeRegex();

    [GeneratedRegex(@"\b(is|are|was|were|will be|confirmed|proven|definitely)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AssertiveRegex();

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

        public bool HasEvidenceState => HasAnyProperty("state_type", "evidence_state", "confidence", "verification_status", "evidence_basis") ||
            EvidenceMarkerRegex().IsMatch(Text);

        public bool HasAuthorityMarker => HasAnyProperty(AuthorityFields.ToArray()) || AuthorityMarkerRegex().IsMatch(Text);

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
    }
}
