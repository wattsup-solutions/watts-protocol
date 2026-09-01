// Licensed under the Apache License, Version 2.0.
// Copyright 2026 WattsUp Solutions, Inc.

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WattsProtocol.Cli;

/// <summary>Abstracts platform values so install location policy is testable.</summary>
public interface IPlatformInfo
{
    /// <summary>Whether the current platform is Windows.</summary>
    bool IsWindows { get; }

    /// <summary>Whether the current platform is macOS.</summary>
    bool IsMacOS { get; }

    /// <summary>User home directory.</summary>
    string HomeDirectory { get; }

    /// <summary>Returns an environment variable if set.</summary>
    string? GetEnvironmentVariable(string name);

    /// <summary>Returns a framework-defined special directory.</summary>
    string GetFolderPath(Environment.SpecialFolder folder);
}

/// <summary>Uses .NET platform and environment APIs without compile-time platform branches.</summary>
public sealed class RuntimePlatformInfo : IPlatformInfo
{
    /// <inheritdoc />
    public bool IsWindows => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
        System.Runtime.InteropServices.OSPlatform.Windows);

    /// <inheritdoc />
    public bool IsMacOS => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
        System.Runtime.InteropServices.OSPlatform.OSX);

    /// <inheritdoc />
    public string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <inheritdoc />
    public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

    /// <inheritdoc />
    public string GetFolderPath(Environment.SpecialFolder folder) => Environment.GetFolderPath(folder);
}

/// <summary>Resolves platform-specific data roots and version directories for wp.</summary>
public sealed class InstallPathResolver
{
    private readonly IPlatformInfo platform;

    /// <summary>Creates a resolver using the current runtime platform.</summary>
    public InstallPathResolver()
        : this(new RuntimePlatformInfo())
    {
    }

    /// <summary>Creates a resolver from a supplied platform abstraction.</summary>
    public InstallPathResolver(IPlatformInfo platform)
    {
        this.platform = platform;
    }

    /// <summary>Gets the base root containing all installed protocol versions.</summary>
    public string ResolveBaseRoot(bool systemScope = false, string? prefix = null)
    {
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            return Path.GetFullPath(prefix);
        }

        var overrideHome = platform.GetEnvironmentVariable("WATTS_PROTOCOL_HOME");
        if (!string.IsNullOrWhiteSpace(overrideHome))
        {
            return Path.GetFullPath(overrideHome);
        }

        if (platform.IsWindows)
        {
            var root = systemScope
                ? platform.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                : platform.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "OpenProtocolStandards", "watts-protocol");
        }

        if (systemScope)
        {
            return Path.Combine("/usr/local/share", "open-protocol-standards", "watts-protocol");
        }

        if (platform.IsMacOS)
        {
            return Path.Combine(platform.HomeDirectory, "Library", "Application Support", "OpenProtocolStandards", "watts-protocol");
        }

        var xdgDataHome = platform.GetEnvironmentVariable("XDG_DATA_HOME");
        var linuxDataHome = string.IsNullOrWhiteSpace(xdgDataHome)
            ? Path.Combine(platform.HomeDirectory, ".local", "share")
            : xdgDataHome;
        return Path.Combine(linuxDataHome, "open-protocol-standards", "watts-protocol");
    }

    /// <summary>Gets a version-specific install directory.</summary>
    public string ResolveVersionRoot(string version, bool systemScope = false, string? prefix = null) =>
        Path.Combine(ResolveBaseRoot(systemScope, prefix), NormalizeVersion(version));

    /// <summary>Normalizes a protocol version into the version-directory convention.</summary>
    public static string NormalizeVersion(string version)
    {
        var trimmed = version.Trim();
        return trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? trimmed : $"v{trimmed}";
    }
}

/// <summary>A bundled file that is copied from the self-contained assembly.</summary>
public sealed record BundledAsset(string RelativePath, byte[] Contents)
{
    /// <summary>SHA-256 over the bundled file contents.</summary>
    public string Sha256 => Convert.ToHexString(SHA256.HashData(Contents)).ToLowerInvariant();
}

/// <summary>Loads the immutable protocol assets embedded in the wp binary.</summary>
public sealed class ProtocolAssetCatalog
{
    /// <summary>The GA version compiled into this distribution.</summary>
    public const string CurrentVersion = "1.2";

    /// <summary>Lists embedded training, specification, and example-capsule files.</summary>
    public IReadOnlyList<BundledAsset> GetAssets(string version)
    {
        if (!string.Equals(version.TrimStart('v'), CurrentVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"This wp build bundles Watts-Protocol™ {CurrentVersion}; version '{version}' is not available offline.");
        }

        return
        [
            ReadAsset("Watts_Protocol_Training_v1.2.json"),
            ReadAsset("Watts_Protocol_Short_Spec_v1.2.txt"),
            ReadAsset("example-capsule.yaml"),
            ReadAsset("example-capsule.json"),
        ];
    }

    /// <summary>Reads the bundled short protocol text.</summary>
    public string GetShortProtocol() => Encoding.UTF8.GetString(
        GetAssets(CurrentVersion).Single(asset => asset.RelativePath == "Watts_Protocol_Short_Spec_v1.2.txt").Contents);

    private static BundledAsset ReadAsset(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"WattsProtocol.Cli.Assets.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return new BundledAsset(name, output.ToArray());
    }
}

/// <summary>Records the integrity state of one file in an installed protocol bundle.</summary>
public sealed class ManifestFile
{
    /// <summary>Path relative to the version directory.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Lowercase SHA-256 hexadecimal digest.</summary>
    public string Sha256 { get; set; } = string.Empty;
}

/// <summary>Machine-readable installation record stored alongside each version.</summary>
public sealed class InstallManifest
{
    /// <summary>Installed Watts-Protocol version.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>UTC timestamp for the initial installation.</summary>
    public DateTimeOffset InstalledAtUtc { get; set; }

    /// <summary>Installed asset files and their digests.</summary>
    public List<ManifestFile> Files { get; set; } = [];
}

/// <summary>Describes changes or errors from a delivery-system operation.</summary>
public sealed class DeliveryResult
{
    /// <summary>Human-readable planned or completed actions.</summary>
    public List<string> Actions { get; } = [];

    /// <summary>Optional actionable failure message.</summary>
    public string? Error { get; set; }

    /// <summary>Whether the operation succeeded.</summary>
    public bool Succeeded => Error is null;
}

/// <summary>Installs, lists, validates, and removes the self-contained protocol bundle.</summary>
public sealed class ProtocolInstaller
{
    private const string ManifestName = "manifest.json";
    private const string ActiveName = "active.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly InstallPathResolver paths;
    private readonly ProtocolAssetCatalog assets;

    /// <summary>Creates an installer for the current platform and embedded assets.</summary>
    public ProtocolInstaller()
        : this(new InstallPathResolver(), new ProtocolAssetCatalog())
    {
    }

    /// <summary>Creates an installer with injectable path and asset dependencies.</summary>
    public ProtocolInstaller(InstallPathResolver paths, ProtocolAssetCatalog assets)
    {
        this.paths = paths;
        this.assets = assets;
    }

    /// <summary>Installs a bundled version and makes it active unless this is a dry run.</summary>
    public DeliveryResult Install(string version, bool systemScope = false, string? prefix = null, bool dryRun = false)
    {
        var result = new DeliveryResult();
        try
        {
            var baseRoot = paths.ResolveBaseRoot(systemScope, prefix);
            var target = paths.ResolveVersionRoot(version, systemScope, prefix);
            var bundle = assets.GetAssets(version);
            var existingManifest = ReadManifest(Path.Combine(target, ManifestName));
            var complete = IsComplete(existingManifest, bundle, target, version);

            if (complete)
            {
                result.Actions.Add($"unchanged {target}");
            }
            else
            {
                result.Actions.Add($"install {target}");
                foreach (var asset in bundle)
                {
                    result.Actions.Add($"write {Path.Combine(target, asset.RelativePath)}");
                }

                result.Actions.Add($"write {Path.Combine(target, ManifestName)}");
            }

            var activePath = Path.Combine(baseRoot, ActiveName);
            var activeVersion = ReadActiveVersion(activePath);
            if (!string.Equals(activeVersion, version.TrimStart('v'), StringComparison.Ordinal))
            {
                result.Actions.Add($"set active {version.TrimStart('v')}");
            }

            if (dryRun || complete && string.Equals(activeVersion, version.TrimStart('v'), StringComparison.Ordinal))
            {
                return result;
            }

            Directory.CreateDirectory(target);
            if (!complete)
            {
                foreach (var asset in bundle)
                {
                    File.WriteAllBytes(Path.Combine(target, asset.RelativePath), asset.Contents);
                }

                var manifest = new InstallManifest
                {
                    Version = version.TrimStart('v'),
                    InstalledAtUtc = DateTimeOffset.UtcNow,
                    Files = bundle.Select(asset => new ManifestFile { Path = asset.RelativePath, Sha256 = asset.Sha256 }).ToList(),
                };
                File.WriteAllText(Path.Combine(target, ManifestName), JsonSerializer.Serialize(manifest, JsonOptions));
            }

            Directory.CreateDirectory(baseRoot);
            File.WriteAllText(activePath, JsonSerializer.Serialize(new { version = version.TrimStart('v') }, JsonOptions));
        }
        catch (UnauthorizedAccessException)
        {
            result.Error = "Permission was denied. Use the default user scope, choose a writable --prefix, or rerun with permissions appropriate for --system.";
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        {
            result.Error = exception.Message;
        }

        return result;
    }

    /// <summary>Removes one installed version or all installed versions with purge.</summary>
    public DeliveryResult Uninstall(
        string version,
        bool purge = false,
        bool systemScope = false,
        string? prefix = null,
        bool dryRun = false)
    {
        var result = new DeliveryResult();
        try
        {
            var baseRoot = paths.ResolveBaseRoot(systemScope, prefix);
            var target = paths.ResolveVersionRoot(version, systemScope, prefix);
            var deleteTarget = purge ? baseRoot : target;
            if (!Directory.Exists(deleteTarget) && !(purge && File.Exists(Path.Combine(baseRoot, ActiveName))))
            {
                result.Actions.Add($"absent {deleteTarget}");
                return result;
            }

            result.Actions.Add($"remove {deleteTarget}");
            if (dryRun)
            {
                return result;
            }

            if (Directory.Exists(deleteTarget))
            {
                Directory.Delete(deleteTarget, recursive: true);
            }

            if (purge)
            {
                return result;
            }

            var activePath = Path.Combine(baseRoot, ActiveName);
            if (string.Equals(ReadActiveVersion(activePath), version.TrimStart('v'), StringComparison.Ordinal))
            {
                var replacement = FindInstalledVersions(baseRoot).OrderByDescending(item => item, StringComparer.Ordinal).FirstOrDefault();
                if (replacement is null)
                {
                    if (File.Exists(activePath))
                    {
                        File.Delete(activePath);
                    }
                }
                else
                {
                    File.WriteAllText(activePath, JsonSerializer.Serialize(new { version = replacement }, JsonOptions));
                    result.Actions.Add($"set active {replacement}");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            result.Error = "Permission was denied. Use the default user scope, choose a writable --prefix, or rerun with permissions appropriate for --system.";
        }
        catch (Exception exception) when (exception is IOException or ArgumentException)
        {
            result.Error = exception.Message;
        }

        return result;
    }

    /// <summary>Lists installed versions and marks the currently active version.</summary>
    public IReadOnlyList<(string Version, bool IsActive)> List(bool systemScope = false, string? prefix = null)
    {
        var baseRoot = paths.ResolveBaseRoot(systemScope, prefix);
        var active = ReadActiveVersion(Path.Combine(baseRoot, ActiveName));
        return FindInstalledVersions(baseRoot)
            .OrderByDescending(version => version, StringComparer.Ordinal)
            .Select(version => (version, string.Equals(version, active, StringComparison.Ordinal)))
            .ToList();
    }

    /// <summary>Returns the resolved active (or requested default) version directory.</summary>
    public string GetPath(string version = ProtocolAssetCatalog.CurrentVersion, bool systemScope = false, string? prefix = null)
    {
        var baseRoot = paths.ResolveBaseRoot(systemScope, prefix);
        var active = ReadActiveVersion(Path.Combine(baseRoot, ActiveName));
        return paths.ResolveVersionRoot(active ?? version, systemScope, prefix);
    }

    /// <summary>Checks every file listed in an installed manifest against its recorded hash.</summary>
    public DeliveryResult Verify(string? version = null, bool systemScope = false, string? prefix = null)
    {
        var result = new DeliveryResult();
        var baseRoot = paths.ResolveBaseRoot(systemScope, prefix);
        var resolvedVersion = version?.TrimStart('v') ?? ReadActiveVersion(Path.Combine(baseRoot, ActiveName)) ?? ProtocolAssetCatalog.CurrentVersion;
        var root = paths.ResolveVersionRoot(resolvedVersion, systemScope, prefix);
        var manifest = ReadManifest(Path.Combine(root, ManifestName));
        if (manifest is null)
        {
            result.Error = $"No install manifest exists at '{Path.Combine(root, ManifestName)}'.";
            return result;
        }

        foreach (var file in manifest.Files)
        {
            var path = Path.Combine(root, file.Path);
            if (!File.Exists(path))
            {
                result.Error = $"Missing installed file: {file.Path}";
                return result;
            }

            var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
            if (!string.Equals(actual, file.Sha256, StringComparison.Ordinal))
            {
                result.Error = $"Hash mismatch: {file.Path}";
                return result;
            }
        }

        result.Actions.Add($"verified {root} ({manifest.Files.Count} files)");
        return result;
    }

    private static bool IsComplete(InstallManifest? manifest, IReadOnlyList<BundledAsset> bundle, string target, string version)
    {
        return manifest is not null &&
            string.Equals(manifest.Version, version.TrimStart('v'), StringComparison.Ordinal) &&
            manifest.Files.Count == bundle.Count &&
            bundle.All(asset =>
            {
                var expected = manifest.Files.SingleOrDefault(file => file.Path == asset.RelativePath);
                var path = Path.Combine(target, asset.RelativePath);
                return expected is not null && expected.Sha256 == asset.Sha256 && File.Exists(path) &&
                    string.Equals(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant(), asset.Sha256, StringComparison.Ordinal);
            });
    }

    private static InstallManifest? ReadManifest(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize<InstallManifest>(File.ReadAllText(path));
    }

    private static string? ReadActiveVersion(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("version", out var version) ? version.GetString() : null;
    }

    private static IEnumerable<string> FindInstalledVersions(string baseRoot)
    {
        if (!Directory.Exists(baseRoot))
        {
            return [];
        }

        return Directory.GetDirectories(baseRoot, "v*")
            .Where(directory => File.Exists(Path.Combine(directory, ManifestName)))
            .Select(directory => Path.GetFileName(directory).TrimStart('v'));
    }
}
