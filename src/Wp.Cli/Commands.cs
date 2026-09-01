// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace WattsProtocol.Cli;

/// <summary>Shared file option for commands that consume a capsule.</summary>
public abstract class CapsuleFileSettings : CommandSettings
{
    /// <summary>Input capsule path.</summary>
    [CommandArgument(0, "<FILE>")]
    public string File { get; init; } = string.Empty;
}

/// <summary>Creates an initial Memory Capsule in the current directory.</summary>
public sealed class InitCommand : Command<InitCommand.Settings>
{
    /// <summary>Options for <c>wp init</c>.</summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>Creates JSON rather than YAML.</summary>
        [CommandOption("--json")]
        public bool Json { get; init; }
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        var path = Path.Combine(Environment.CurrentDirectory, settings.Json ? "watts-capsule.json" : "watts-capsule.yaml");
        if (File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]Refusing to overwrite[/] {Markup.Escape(path)}.");
            return 1;
        }

        new CapsuleSerializer().Save(CapsuleSerializer.CreateTemplate(), path);
        AnsiConsole.MarkupLine($"Created [green]{Markup.Escape(path)}[/].");
        return 0;
    }
}

/// <summary>Prints the short protocol and an appropriate capsule for session recovery.</summary>
public sealed class BootstrapCommand : Command<BootstrapCommand.Settings>
{
    /// <summary>Options for <c>wp bootstrap</c>.</summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>Optional capsule to include.</summary>
        [CommandArgument(0, "[CAPSULE]")]
        public string? Capsule { get; init; }

        /// <summary>Emits a compact JSON transport object.</summary>
        [CommandOption("--minified")]
        public bool Minified { get; init; }

        /// <summary>Emits plain text with no terminal control characters.</summary>
        [CommandOption("--clipboard-safe")]
        public bool ClipboardSafe { get; init; }

        /// <summary>Uses the machine-scope install path when searching for an installed bundle.</summary>
        [CommandOption("--system")]
        public bool SystemScope { get; init; }

        /// <summary>Overrides the install base while searching for an installed bundle.</summary>
        [CommandOption("--prefix <PATH>")]
        public string? Prefix { get; init; }
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var serializer = new CapsuleSerializer();
            var installer = new ProtocolInstaller();
            var shortProtocolPath = Path.Combine(installer.GetPath(systemScope: settings.SystemScope, prefix: settings.Prefix), "Watts_Protocol_Short_Spec_v1.2.txt");
            var protocol = File.Exists(shortProtocolPath)
                ? File.ReadAllText(shortProtocolPath)
                : new ProtocolAssetCatalog().GetShortProtocol();
            var capsule = ResolveCapsule(settings.Capsule, installer, settings, serializer);

            if (settings.Minified)
            {
                var output = new JsonObject
                {
                    ["protocol"] = protocol,
                    ["capsule"] = capsule,
                };
                Console.Out.WriteLine(output.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
                return 0;
            }

            Console.Out.WriteLine("WATTS-PROTOCOL™ v1.2 — SESSION BOOTSTRAP");
            Console.Out.WriteLine(protocol.Trim());
            Console.Out.WriteLine();
            Console.Out.WriteLine("MEMORY CAPSULE");
            Console.Out.WriteLine(serializer.ToMinifiedJson(capsule));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException)
        {
            AnsiConsole.MarkupLine($"[red]Bootstrap failed:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }
    }

    private static JsonObject ResolveCapsule(string? supplied, ProtocolInstaller installer, Settings settings, CapsuleSerializer serializer)
    {
        if (!string.IsNullOrWhiteSpace(supplied))
        {
            return serializer.Load(supplied);
        }

        var installed = Path.Combine(installer.GetPath(systemScope: settings.SystemScope, prefix: settings.Prefix), "example-capsule.yaml");
        return File.Exists(installed) ? serializer.Load(installed) : CapsuleSerializer.CreateTemplate();
    }
}

/// <summary>Creates a Memory Capsule at a selected path.</summary>
public sealed class CapsuleNewCommand : Command<CapsuleNewCommand.Settings>
{
    /// <summary>Options for <c>wp capsule new</c>.</summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>Output file path.</summary>
        [CommandArgument(0, "[FILE]")]
        public string? File { get; init; }

        /// <summary>Creates JSON rather than YAML when no file path is supplied.</summary>
        [CommandOption("--json")]
        public bool Json { get; init; }
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        var path = settings.File ?? Path.Combine(Environment.CurrentDirectory, settings.Json ? "watts-capsule.json" : "watts-capsule.yaml");
        if (File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]Refusing to overwrite[/] {Markup.Escape(path)}.");
            return 1;
        }

        try
        {
            new CapsuleSerializer().Save(CapsuleSerializer.CreateTemplate(), path);
            AnsiConsole.MarkupLine($"Created [green]{Markup.Escape(path)}[/].");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            AnsiConsole.MarkupLine($"[red]Could not create capsule:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }
    }
}

/// <summary>Displays a capsule in its requested or source format.</summary>
public sealed class CapsuleShowCommand : Command<CapsuleShowCommand.Settings>
{
    /// <summary>Options for <c>wp capsule show</c>.</summary>
    public sealed class Settings : CapsuleFileSettings
    {
        /// <summary>Output format.</summary>
        [CommandOption("--format <FORMAT>")]
        [DefaultValue("json")]
        public string Format { get; init; } = "json";
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var capsule = new CapsuleSerializer().Load(settings.File);
            if (string.Equals(settings.Format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.Out.WriteLine(capsule.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }

            if (string.Equals(settings.Format, "yaml", StringComparison.OrdinalIgnoreCase))
            {
                var temporary = Path.GetTempFileName() + ".yaml";
                try
                {
                    new CapsuleSerializer().Save(capsule, temporary);
                    Console.Out.Write(File.ReadAllText(temporary));
                }
                finally
                {
                    File.Delete(temporary);
                }

                return 0;
            }

            AnsiConsole.MarkupLine("[red]--format must be json or yaml.[/]");
            return 1;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException)
        {
            AnsiConsole.MarkupLine($"[red]Could not display capsule:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }
    }
}

/// <summary>Checks flexible capsule structure without imposing a rigid schema.</summary>
public sealed class CapsuleValidateCommand : Command<CapsuleValidateCommand.Settings>
{
    /// <summary>Options for <c>wp capsule validate</c>.</summary>
    public sealed class Settings : CapsuleFileSettings
    {
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var result = new CapsuleValidator().Validate(new CapsuleSerializer().Load(settings.File));
            foreach (var warning in result.Warnings)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] {Markup.Escape(warning)}");
            }

            foreach (var error in result.Errors)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(error)}");
            }

            if (result.IsValid)
            {
                AnsiConsole.MarkupLine("[green]Capsule structure is valid.[/]");
                return 0;
            }

            return 1;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException)
        {
            AnsiConsole.MarkupLine($"[red]Validation failed:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }
    }
}

/// <summary>Converts a capsule to the destination format inferred from its extension.</summary>
public sealed class CapsuleConvertCommand : Command<CapsuleConvertCommand.Settings>
{
    /// <summary>Options for <c>wp capsule convert</c>.</summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>Source capsule.</summary>
        [CommandArgument(0, "<INPUT>")]
        public string Input { get; init; } = string.Empty;

        /// <summary>Destination capsule.</summary>
        [CommandArgument(1, "<OUTPUT>")]
        public string Output { get; init; } = string.Empty;
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var serializer = new CapsuleSerializer();
            serializer.Save(serializer.Load(settings.Input), settings.Output);
            AnsiConsole.MarkupLine($"Converted to [green]{Markup.Escape(settings.Output)}[/].");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException)
        {
            AnsiConsole.MarkupLine($"[red]Conversion failed:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }
    }
}

/// <summary>Runs deterministic Watts-Protocol context-integrity rules.</summary>
public sealed class CheckCommand : Command<CheckCommand.Settings>
{
    /// <summary>Options for <c>wp check</c>.</summary>
    public sealed class Settings : CapsuleFileSettings
    {
        /// <summary>Output format: table or json.</summary>
        [CommandOption("--format <FORMAT>")]
        [DefaultValue("table")]
        public string Format { get; init; } = "table";

        /// <summary>Lists every affected item instead of one grouped row per rule.</summary>
        [CommandOption("-v|--verbose")]
        public bool Verbose { get; init; }
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var engine = new RulesEngine();
            var extension = Path.GetExtension(settings.File);
            var findings = string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase)
                ? engine.Check(new CapsuleSerializer().Load(settings.File))
                : engine.CheckText(File.ReadAllText(settings.File));
            if (string.Equals(settings.Format, "json", StringComparison.OrdinalIgnoreCase))
            {
                // Emit stable, script-friendly JSON: camelCase keys and string severities so CI
                // consumers are not coupled to enum ordinals.
                var payload = findings.Select(finding => new
                {
                    severity = finding.Severity == FindingSeverity.Finding ? "finding" : "warning",
                    rule = finding.Rule,
                    location = finding.Location,
                    message = finding.Message,
                });
                Console.Out.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            }
            else if (string.Equals(settings.Format, "table", StringComparison.OrdinalIgnoreCase))
            {
                if (findings.Count == 0)
                {
                    AnsiConsole.MarkupLine("[green]No context-integrity risks found.[/]");
                }
                else if (settings.Verbose)
                {
                    WriteItemTable(findings);
                }
                else
                {
                    WriteGroupedTable(findings);
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]--format must be table or json.[/]");
                return 1;
            }

            return findings.Any(finding => finding.Severity == FindingSeverity.Finding)
                ? 2
                : findings.Count > 0 ? 1 : 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException)
        {
            AnsiConsole.MarkupLine($"[red]Check failed:[/] {Markup.Escape(exception.Message)}");
            return 2;
        }
    }

    private static string Severity(FindingSeverity severity) =>
        severity == FindingSeverity.Finding ? "[red]finding[/]" : "[yellow]warning[/]";

    /// <summary>Strips the item index from a location, leaving the capsule section name.</summary>
    private static string SectionOf(string location)
    {
        var bracket = location.IndexOf('[');
        return bracket < 0 ? location : location[..bracket];
    }

    /// <summary>
    /// One row per rule, with how many items tripped it and which sections they live in. A capsule
    /// with three distinct problems reads as three rows rather than one row per affected item.
    /// </summary>
    private static void WriteGroupedTable(IReadOnlyList<RuleFinding> findings)
    {
        var groups = findings
            .GroupBy(finding => (finding.Severity, finding.Rule))
            .OrderByDescending(group => group.Key.Severity == FindingSeverity.Finding)
            .ThenByDescending(group => group.Count())
            .ThenBy(group => group.Key.Rule, StringComparer.Ordinal)
            .ToList();

        // Rule names and counts are never wrapped: a rule identifier split across lines is not
        // greppable and is the first thing a reader scans for.
        var table = new Table()
            .AddColumn(new TableColumn("Severity").NoWrap())
            .AddColumn(new TableColumn("Rule").NoWrap())
            .AddColumn(new TableColumn("Items").RightAligned().NoWrap())
            .AddColumn("Sections")
            .AddColumn("Message");

        foreach (var group in groups)
        {
            var sections = group
                .Select(finding => SectionOf(finding.Location))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(section => section, StringComparer.Ordinal)
                .ToList();

            var rendered = sections.Count > 4
                ? string.Join(", ", sections.Take(4)) + $", +{sections.Count - 4} more"
                : string.Join(", ", sections);

            table.AddRow(
                Severity(group.Key.Severity),
                Markup.Escape(group.Key.Rule),
                group.Count().ToString(CultureInfo.InvariantCulture),
                Markup.Escape(rendered),
                Markup.Escape(group.First().Message));
        }

        AnsiConsole.Write(table);

        var findingCount = findings.Count(finding => finding.Severity == FindingSeverity.Finding);
        var warningCount = findings.Count - findingCount;
        AnsiConsole.MarkupLine(
            $"[grey]{groups.Count} rule(s) tripped across {findings.Count} item(s): " +
            $"{findingCount} finding(s), {warningCount} warning(s). " +
            $"Re-run with --verbose for per-item locations.[/]");
    }

    /// <summary>One row per affected item, for --verbose.</summary>
    private static void WriteItemTable(IReadOnlyList<RuleFinding> findings)
    {
        var table = new Table()
            .AddColumn(new TableColumn("Severity").NoWrap())
            .AddColumn(new TableColumn("Rule").NoWrap())
            .AddColumn(new TableColumn("Location").NoWrap())
            .AddColumn("Message");
        foreach (var finding in findings
                     .OrderByDescending(finding => finding.Severity == FindingSeverity.Finding)
                     .ThenBy(finding => finding.Rule, StringComparer.Ordinal)
                     .ThenBy(finding => finding.Location, StringComparer.Ordinal))
        {
            table.AddRow(
                Severity(finding.Severity),
                Markup.Escape(finding.Rule),
                Markup.Escape(finding.Location),
                Markup.Escape(finding.Message));
        }

        AnsiConsole.Write(table);
    }
}

/// <summary>Compresses session text into a cautious evidence-labelled capsule.</summary>
public sealed class CompressCommand : Command<CompressCommand.Settings>
{
    /// <summary>Options for <c>wp compress</c>.</summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>Session text or capsule input.</summary>
        [CommandArgument(0, "<INPUT>")]
        public string Input { get; init; } = string.Empty;

        /// <summary>Optional capsule destination.</summary>
        [CommandArgument(1, "[OUTPUT]")]
        public string? Output { get; init; }

        /// <summary>Creates JSON when selecting the default output.</summary>
        [CommandOption("--json")]
        public bool Json { get; init; }
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var sourceText = File.ReadAllText(settings.Input);
            var capsule = SessionCompressor.Compress(sourceText, Path.GetFileNameWithoutExtension(settings.Input));
            var output = settings.Output ?? Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(settings.Input))!,
                $"compressed-capsule.{(settings.Json ? "json" : "yaml")}");
            new CapsuleSerializer().Save(capsule, output);
            AnsiConsole.MarkupLine($"Compressed session to [green]{Markup.Escape(output)}[/].");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            AnsiConsole.MarkupLine($"[red]Compression failed:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }
    }
}

/// <summary>Prints wp's compiled version.</summary>
public sealed class VersionCommand : Command<VersionCommand.Settings>
{
    /// <summary>Options for <c>wp version</c>.</summary>
    public sealed class Settings : CommandSettings
    {
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        Console.Out.WriteLine("wp 1.2.0 (Watts-Protocol™ v1.2)");
        return 0;
    }
}
