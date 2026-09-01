// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using System.Text;
using Spectre.Console.Cli;

namespace WattsProtocol.Cli;

/// <summary>Application entry point for the Watts-Protocol delivery system.</summary>
public static class Program
{
    /// <summary>Configures and executes the wp command line application.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>A process exit code.</returns>
    public static int Main(string[] args)
    {
        // The trademark symbol in Watts-Protocol™ renders as a replacement character on
        // consoles that are not already UTF-8 (notably the default Windows code page 437/1252).
        // Setting the output encoding explicitly makes wp render identically everywhere.
        // Guarded because the setter throws IOException when stdout is redirected to a
        // handle that does not support encoding changes.
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // Non-fatal: fall back to the host console's default encoding.
        }

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("wp");
            config.SetApplicationVersion("1.2.1");
            config.AddCommand<InitCommand>("init")
                .WithDescription("Create a new Watts-Protocol Memory Capsule.");
            config.AddCommand<BootstrapCommand>("bootstrap")
                .WithDescription("Print a short protocol and capsule for a fresh AI session.");
            config.AddCommand<InstallCommand>("install")
                .WithDescription("Install bundled Watts-Protocol™ files for this user or system.");
            config.AddCommand<UninstallCommand>("uninstall")
                .WithDescription("Remove an installed Watts-Protocol™ version.");
            config.AddCommand<ListCommand>("list")
                .WithDescription("List installed Watts-Protocol™ versions.");
            config.AddCommand<PathCommand>("path")
                .WithDescription("Print the resolved version-specific install path.");
            config.AddCommand<UpdateCommand>("update")
                .WithDescription("Install the newer version bundled with this wp executable.");
            config.AddCommand<VerifyCommand>("verify")
                .WithDescription("Verify installed files against the manifest hashes.");
            config.AddCommand<CheckCommand>("check")
                .WithDescription("Check a capsule or session file for context-integrity risks.");
            config.AddCommand<CompressCommand>("compress")
                .WithDescription("Compress a session file into a governed Memory Capsule.");
            config.AddCommand<VersionCommand>("version")
                .WithDescription("Print the wp version.");
            config.AddBranch("capsule", capsule =>
            {
                capsule.SetDescription("Create, display, validate, and convert Memory Capsules.");
                capsule.AddCommand<CapsuleNewCommand>("new")
                    .WithDescription("Create a new Memory Capsule.");
                capsule.AddCommand<CapsuleShowCommand>("show")
                    .WithDescription("Display a Memory Capsule.");
                capsule.AddCommand<CapsuleValidateCommand>("validate")
                    .WithDescription("Validate a Memory Capsule's v1.x structure.");
                capsule.AddCommand<CapsuleConvertCommand>("convert")
                    .WithDescription("Convert a capsule between YAML and JSON.");
            });
        });

        return app.Run(args);
    }
}
