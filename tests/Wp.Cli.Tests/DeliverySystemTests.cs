// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using WattsProtocol.Cli;

namespace Wp.Cli.Tests;

/// <summary>Tests cross-platform install resolution and the self-contained bundle lifecycle.</summary>
public sealed class DeliverySystemTests
{
    /// <summary>Windows user and machine locations follow the documented OpenProtocolStandards policy.</summary>
    [Fact]
    public void ResolvesWindowsUserAndSystemPaths()
    {
        var platform = new FakePlatform
        {
            Windows = true,
            LocalAppData = @"C:\Users\Pat\AppData\Local",
            ProgramData = @"C:\ProgramData",
        };
        var resolver = new InstallPathResolver(platform);

        Assert.Equal(
            Path.Combine(platform.LocalAppData, "OpenProtocolStandards", "watts-protocol", "v1.2"),
            resolver.ResolveVersionRoot("1.2"));
        Assert.Equal(
            Path.Combine(platform.ProgramData, "OpenProtocolStandards", "watts-protocol", "v1.2"),
            resolver.ResolveVersionRoot("1.2", systemScope: true));
    }

    /// <summary>macOS and Linux user paths follow their respective user-data conventions.</summary>
    [Fact]
    public void ResolvesMacAndLinuxUserPaths()
    {
        var mac = new FakePlatform { MacOS = true, Home = "/Users/pat" };
        var linux = new FakePlatform { Home = "/home/pat", Environment = { ["XDG_DATA_HOME"] = "/data" } };

        Assert.Equal(
            Path.Combine("/Users/pat", "Library", "Application Support", "OpenProtocolStandards", "watts-protocol", "v1.2"),
            new InstallPathResolver(mac).ResolveVersionRoot("1.2"));
        Assert.Equal(
            Path.Combine("/data", "open-protocol-standards", "watts-protocol", "v1.2"),
            new InstallPathResolver(linux).ResolveVersionRoot("1.2"));
    }

    /// <summary>Explicit prefixes take precedence and receive the version folder only once.</summary>
    [Fact]
    public void PrefixOverridesPlatformRoot()
    {
        var resolver = new InstallPathResolver(new FakePlatform { Home = "/home/pat" });
        var prefix = Path.Combine(Path.GetTempPath(), "wp-prefix");

        Assert.Equal(Path.Combine(Path.GetFullPath(prefix), "v1.2"), resolver.ResolveVersionRoot("1.2", prefix: prefix));
    }

    /// <summary>Installation is idempotent and manifest hashes detect tampering.</summary>
    [Fact]
    public void InstallIsIdempotentAndVerifyChecksHashes()
    {
        var prefix = CreateTemporaryDirectory();
        var installer = new ProtocolInstaller(new InstallPathResolver(new FakePlatform { Home = "/home/pat" }), new ProtocolAssetCatalog());

        var first = installer.Install("1.2", prefix: prefix);
        var second = installer.Install("1.2", prefix: prefix);
        var verification = installer.Verify(prefix: prefix);

        Assert.True(first.Succeeded, first.Error);
        Assert.True(second.Succeeded, second.Error);
        Assert.Contains(second.Actions, action => action.StartsWith("unchanged ", StringComparison.Ordinal));
        Assert.True(verification.Succeeded, verification.Error);
        Assert.True(File.Exists(Path.Combine(prefix, "v1.2", "manifest.json")));

        File.AppendAllText(Path.Combine(prefix, "v1.2", "example-capsule.json"), " ");
        var tampered = installer.Verify(prefix: prefix);
        Assert.False(tampered.Succeeded);
        Assert.Contains("Hash mismatch", tampered.Error!);
    }

    /// <summary>Dry-run installation reports writes without creating a version directory.</summary>
    [Fact]
    public void InstallDryRunDoesNotWriteFiles()
    {
        var prefix = CreateTemporaryDirectory();
        var installer = new ProtocolInstaller(new InstallPathResolver(new FakePlatform { Home = "/home/pat" }), new ProtocolAssetCatalog());

        var result = installer.Install("1.2", prefix: prefix, dryRun: true);

        Assert.True(result.Succeeded, result.Error);
        Assert.Contains(result.Actions, action => action.StartsWith("install ", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(prefix, "v1.2")));
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "wp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class FakePlatform : IPlatformInfo
    {
        public bool Windows { get; init; }

        public bool MacOS { get; init; }

        public string Home { get; init; } = "/home/default";

        public string LocalAppData { get; init; } = "/local";

        public string ProgramData { get; init; } = "/programdata";

        public Dictionary<string, string> Environment { get; } = new(StringComparer.Ordinal);

        public bool IsWindows => Windows;

        public bool IsMacOS => MacOS;

        public string HomeDirectory => Home;

        public string? GetEnvironmentVariable(string name) =>
            Environment.TryGetValue(name, out var value) ? value : null;

        public string GetFolderPath(System.Environment.SpecialFolder folder) => folder switch
        {
            System.Environment.SpecialFolder.LocalApplicationData => LocalAppData,
            System.Environment.SpecialFolder.CommonApplicationData => ProgramData,
            System.Environment.SpecialFolder.UserProfile => Home,
            _ => string.Empty,
        };
    }
}
