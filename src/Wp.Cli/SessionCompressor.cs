// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WattsProtocol.Cli;

/// <summary>Conservatively extracts useful state from verbose session text.</summary>
public static partial class SessionCompressor
{
    /// <summary>Creates a capsule whose extracted claims retain an explicit evidence classification.</summary>
    public static JsonObject Compress(string sessionText, string sessionName)
    {
        var capsule = CapsuleSerializer.CreateTemplate();
        capsule["session_name"] = sessionName;
        var facts = new JsonArray();
        var decisions = new JsonArray();
        var constraints = new JsonArray();
        var questions = new JsonArray();
        var risks = new JsonArray();
        var actions = new JsonArray();

        foreach (var sentence in Sentences(sessionText))
        {
            if (sentence.Length < 4)
            {
                continue;
            }

            if (sentence.EndsWith("?", StringComparison.Ordinal) || QuestionRegex().IsMatch(sentence))
            {
                questions.Add(Label(sentence, "unknown", "unresolved"));
                continue;
            }

            if (ActionRegex().IsMatch(sentence))
            {
                actions.Add(Label(sentence, "next_action", "unassigned"));
                continue;
            }

            if (RiskRegex().IsMatch(sentence))
            {
                risks.Add(Label(sentence, "risk", "unassessed"));
                continue;
            }

            if (ConstraintRegex().IsMatch(sentence))
            {
                constraints.Add(Label(sentence, "constraint", "stated"));
                continue;
            }

            if (DecisionRegex().IsMatch(sentence))
            {
                decisions.Add(Label(sentence, "decision", "unverified"));
                continue;
            }

            facts.Add(Label(sentence, StateType(sentence), Confidence(sentence)));
        }

        capsule["key_facts"] = facts;
        capsule["decisions_made"] = decisions;
        capsule["constraints"] = constraints;
        capsule["open_questions"] = questions;
        capsule["risks"] = risks;
        capsule["next_actions"] = actions;
        capsule["changelog"] = new JsonArray("Compressed from session text; evidence labels were retained or assigned conservatively.");
        return capsule;
    }

    private static IEnumerable<string> Sentences(string text) =>
        SentenceRegex().Split(text).Select(value => value.Trim()).Where(value => value.Length > 0);

    private static JsonObject Label(string text, string stateType, string confidence) =>
        new()
        {
            ["text"] = text,
            ["state_type"] = stateType,
            ["confidence"] = confidence,
            ["source"] = "session-compression",
        };

    private static string StateType(string sentence)
    {
        if (HedgeRegex().IsMatch(sentence))
        {
            return "hypothesis";
        }

        if (ObservationRegex().IsMatch(sentence))
        {
            return "observation";
        }

        if (VerifiedRegex().IsMatch(sentence))
        {
            return "verified_fact";
        }

        return "generated_information";
    }

    private static string Confidence(string sentence) =>
        HedgeRegex().IsMatch(sentence) ? "low" : VerifiedRegex().IsMatch(sentence) ? "stated_verified" : "unverified";

    [GeneratedRegex(@"(?<=[.!?])\s+|\r?\n+", RegexOptions.CultureInvariant)]
    private static partial Regex SentenceRegex();

    [GeneratedRegex(@"\b(maybe|perhaps|appears?|seems?|likely|might|could|assum(?:e|ed|ption)|hypothes(?:is|ize|ized)|preliminary|tentative)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HedgeRegex();

    [GeneratedRegex(@"\b(observe[ds]?|look(?:s|ed)?|visually|heard|noticed)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ObservationRegex();

    [GeneratedRegex(@"\b(verified|confirmed|measured|documented)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VerifiedRegex();

    [GeneratedRegex(@"\b(decided|decision|approved|we will use|chosen)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DecisionRegex();

    [GeneratedRegex(@"\b(must|constraint|cannot|can't|required)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ConstraintRegex();

    [GeneratedRegex(@"\b(risk|danger|may fail|could fail|blocker)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RiskRegex();

    [GeneratedRegex(@"\b(next action|todo|to do|will do|need to|follow up)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ActionRegex();

    [GeneratedRegex(@"\b(question|unknown|unclear|need to know)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex QuestionRegex();
}
