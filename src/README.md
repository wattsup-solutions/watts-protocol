# `wp` — Watts-Protocol™ delivery system and rules engine

`wp` is a self-contained, offline command-line delivery system for the Watts-Protocol™ v1.2 Generally Accepted 1.x baseline. It installs embedded training, a short specification, and example Memory Capsules without downloading anything. It also scaffolds, converts, validates, compresses, bootstraps, and checks portable YAML/JSON Memory Capsules.

The tool is built for .NET 9 and uses [Spectre.Console.Cli](https://spectreconsole.net/cli/) with [Spectre.Console](https://spectreconsole.net/) for rich command parsing/output, and [YamlDotNet](https://github.com/aaubry/YamlDotNet) for YAML.

## Build and test

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0). From the repository root:

```bash
dotnet restore
dotnet build
dotnet test
```

`watts-protocol.sln` at the repository root ties `src/Wp.Cli` and `tests/Wp.Cli.Tests` together, so the bare commands above work from a fresh clone with no arguments.

Run from source:

```bash
dotnet run --project src/Wp.Cli/Wp.Cli.csproj -- --help
dotnet run --project src/Wp.Cli/Wp.Cli.csproj -- install
dotnet run --project src/Wp.Cli/Wp.Cli.csproj -- verify
```

## Delivery commands

```text
wp install [--version 1.2] [--system] [--prefix PATH] [--dry-run]
wp uninstall [--version 1.2] [--purge] [--system] [--prefix PATH] [--dry-run]
wp list [--system] [--prefix PATH]
wp path [--version 1.2] [--system] [--prefix PATH]
wp update [--system] [--prefix PATH] [--dry-run]
wp verify [--version 1.2] [--system] [--prefix PATH]
```

`wp install` is idempotent. It writes the following embedded resources to a versioned install directory and records their lowercase SHA-256 hashes in `manifest.json`:

- `Watts_Protocol_Training_v1.2.json`
- `Watts_Protocol_Short_Spec_v1.2.txt`
- `example-capsule.yaml`
- `example-capsule.json`

`wp verify` checks the files against that manifest. `wp update` installs the newer version bundled in the executable alongside existing versions and points `active.json` to it. This v1.2 executable only contains v1.2; an update becomes meaningful when a newer `wp` executable embeds a newer GA bundle.

By default, `wp` uses a user-writable data location:

| Platform | User install |
|---|---|
| Windows | `%LOCALAPPDATA%\OpenProtocolStandards\watts-protocol\v1.2` |
| macOS | `~/Library/Application Support/OpenProtocolStandards/watts-protocol/v1.2` |
| Linux | `${XDG_DATA_HOME:-$HOME/.local/share}/open-protocol-standards/watts-protocol/v1.2` |

Use `--system` only when a machine-wide install is intended. Windows uses `%ProgramData%\OpenProtocolStandards\watts-protocol\v1.2`; macOS/Linux use `/usr/local/share/open-protocol-standards/watts-protocol/v1.2`. The CLI never writes to `System32`. An insufficient-permission error tells you to use user scope, choose a writable `--prefix`, or use permissions appropriate for `--system`.

`WATTS_PROTOCOL_HOME` overrides the base directory. `--prefix PATH` takes precedence over both platform defaults and the environment override. Version folders (such as `v1.2`) are created under that base.

Use `--dry-run` with install, uninstall, or update to print exact planned writes/removals without changing the file system.

## Capsule and rules commands

```text
wp init [--json]
wp bootstrap [CAPSULE] [--minified] [--clipboard-safe]
wp capsule new [FILE] [--json]
wp capsule show FILE [--format json|yaml]
wp capsule validate FILE
wp capsule convert INPUT OUTPUT
wp check FILE [--format table|json]
wp compress INPUT [OUTPUT] [--json]
wp version
```

`wp bootstrap` uses the installed short protocol and example capsule when available; otherwise it falls back to the resources embedded in the executable. `--minified` emits one compact JSON object suitable for transfer to a new session. `--clipboard-safe` preserves plain, no-ANSI output.

The recommended v1.x capsule fields are:

```text
session_name, project_name, active_objective, key_facts, documents_reviewed,
decisions_made, constraints, open_questions, risks, next_actions, changelog
```

Validation keeps the protocol flexible: missing recommended fields are warnings rather than schema errors. `wp capsule convert` preserves additional domain-specific sections as well as standard fields across YAML and JSON.

`wp check` detects unlabeled evidence state, missing authority markers, Low-Confidence State Promotion candidates, False Perceptual Attribution, superseded active state, and stale/expired state. Exit statuses are `0` for clean, `1` for warnings only, and `2` when findings are present, making the command suitable for CI.

`wp compress` produces a cautious capsule from session text: extracted content is tagged with an evidence state and confidence, and it never silently turns hedged material into a verified fact.

## Publish self-contained single-file binaries

The project enables `PublishSingleFile` and `SelfContained` in `Wp.Cli.csproj`. It intentionally leaves `PublishTrimmed=false`: Spectre.Console.Cli discovers command settings through reflection, so avoiding trimming keeps every command reliably available in the single-file deliverable.

Run the following commands from the repository root after exporting the SDK environment above:

```bash
dotnet publish src/Wp.Cli/Wp.Cli.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/wp-win-x64
dotnet publish src/Wp.Cli/Wp.Cli.csproj -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true -o artifacts/wp-win-arm64
dotnet publish src/Wp.Cli/Wp.Cli.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/wp-linux-x64
dotnet publish src/Wp.Cli/Wp.Cli.csproj -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true -o artifacts/wp-linux-arm64
dotnet publish src/Wp.Cli/Wp.Cli.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/wp-osx-x64
dotnet publish src/Wp.Cli/Wp.Cli.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o artifacts/wp-osx-arm64
```

The Windows artifacts are `wp.exe`; macOS/Linux artifacts are `wp`.
