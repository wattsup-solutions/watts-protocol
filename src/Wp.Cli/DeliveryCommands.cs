// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace WattsProtocol.Cli;

/// <summary>Shared options for delivery-system commands.</summary>
public class DeliverySettings : CommandSettings
{
    /// <summary>Uses the machine-level install location.</summary>
    [CommandOption("--system")]
    public bool SystemScope { get; init; }

    /// <summary>Overrides the version-root parent directory.</summary>
    [CommandOption("--prefix <PATH>")]
    public string? Prefix { get; init; }
}

/// <summary>Unpacks the embedded protocol bundle into a user or opted-in machine scope.</summary>
public sealed class InstallCommand : Command<InstallCommand.Settings>
{
    /// <summary>Options for <c>wp install</c>.</summary>
    public sealed class Settings : DeliverySettings
    {
        /// <summary>Embedded version to install.</summary>
        [CommandOption("--version <VERSION>")]
        [DefaultValue(ProtocolAssetCatalog.CurrentVersion)]
        public string Version { get; init; } = ProtocolAssetCatalog.CurrentVersion;

        /// <summary>Shows planned changes without writing files.</summary>
        [CommandOption("--dry-run")]
        public bool DryRun { get; init; }
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings) =>
        DeliveryCommandOutput.Write(new ProtocolInstaller().Install(settings.Version, settings.SystemScope, settings.Prefix, settings.DryRun));
}

/// <summary>Removes an installed version or all installed versions.</summary>
public sealed class UninstallCommand : Command<UninstallCommand.Settings>
{
    /// <summary>Options for <c>wp uninstall</c>.</summary>
    public sealed class Settings : DeliverySettings
    {
        /// <summary>Version to remove.</summary>
        [CommandOption("--version <VERSION>")]
        [DefaultValue(ProtocolAssetCatalog.CurrentVersion)]
        public string Version { get; init; } = ProtocolAssetCatalog.CurrentVersion;

        /// <summary>Removes every installed version under the resolved base root.</summary>
        [CommandOption("--purge")]
        public bool Purge { get; init; }

        /// <summary>Shows planned changes without deleting files.</summary>
        [CommandOption("--dry-run")]
        public bool DryRun { get; init; }
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings) =>
        DeliveryCommandOutput.Write(new ProtocolInstaller().Uninstall(
            settings.Version,
            settings.Purge,
            settings.SystemScope,
            settings.Prefix,
            settings.DryRun));
}

/// <summary>Lists installed protocol versions.</summary>
public sealed class ListCommand : Command<DeliverySettings>
{
    /// <inheritdoc />
    public override int Execute(CommandContext context, DeliverySettings settings)
    {
        try
        {
            var installed = new ProtocolInstaller().List(settings.SystemScope, settings.Prefix);
            if (installed.Count == 0)
            {
                Console.Out.WriteLine("No Watts-Protocol™ versions are installed.");
                return 0;
            }

            foreach (var item in installed)
            {
                Console.Out.WriteLine($"{item.Version}{(item.IsActive ? "  GA/active" : string.Empty)}");
            }

            return 0;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException)
        {
            AnsiConsole.MarkupLine($"[red]List failed:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }
    }
}

/// <summary>Prints a scriptable version-specific installation path.</summary>
public sealed class PathCommand : Command<PathCommand.Settings>
{
    /// <summary>Options for <c>wp path</c>.</summary>
    public sealed class Settings : DeliverySettings
    {
        /// <summary>Version to resolve when no active version is installed.</summary>
        [CommandOption("--version <VERSION>")]
        [DefaultValue(ProtocolAssetCatalog.CurrentVersion)]
        public string Version { get; init; } = ProtocolAssetCatalog.CurrentVersion;
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        Console.Out.WriteLine(new ProtocolInstaller().GetPath(settings.Version, settings.SystemScope, settings.Prefix));
        return 0;
    }
}

/// <summary>Installs the version bundled with this executable and makes it active.</summary>
public sealed class UpdateCommand : Command<UpdateCommand.Settings>
{
    /// <summary>Options for <c>wp update</c>.</summary>
    public sealed class Settings : DeliverySettings
    {
        /// <summary>Shows planned changes without writing files.</summary>
        [CommandOption("--dry-run")]
        public bool DryRun { get; init; }
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings) =>
        DeliveryCommandOutput.Write(new ProtocolInstaller().Install(
            ProtocolAssetCatalog.CurrentVersion,
            settings.SystemScope,
            settings.Prefix,
            settings.DryRun));
}

/// <summary>Verifies installed protocol bundle files against manifest hashes.</summary>
public sealed class VerifyCommand : Command<VerifyCommand.Settings>
{
    /// <summary>Options for <c>wp verify</c>.</summary>
    public sealed class Settings : DeliverySettings
    {
        /// <summary>Specific installed version to verify; default is active.</summary>
        [CommandOption("--version <VERSION>")]
        public string? Version { get; init; }
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings) =>
        DeliveryCommandOutput.Write(new ProtocolInstaller().Verify(settings.Version, settings.SystemScope, settings.Prefix));
}

/// <summary>Renders delivery results consistently without hiding errors.</summary>
internal static class DeliveryCommandOutput
{
    /// <summary>Writes a result and returns a conventional process status.</summary>
    internal static int Write(DeliveryResult result)
    {
        foreach (var action in result.Actions)
        {
            Console.Out.WriteLine(action);
        }

        if (result.Succeeded)
        {
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]Delivery operation failed:[/] {Markup.Escape(result.Error!)}");
        return 1;
    }
}
