# Cake.Grype Build and Release Pipeline — Design

Date: 2026-09-23
Status: Draft for review

## Goal

A repeatable way to build, test, pack and publish `Cake.Grype` to nuget.org, matching the sibling add-in Cake.CycloneDX so both repositories work the same way.

Success criteria:

- Pull requests and pushes to `main` build and test the add-in on Windows, Linux and macOS.
- Every CI build dogfoods the **current source** of Cake.Grype against real Grype on the user's primary use case: generate a CycloneDX SBOM of Cake.Grype, scan it with `GrypeScanSbom`, upload the JSON report as an artifact, and gate the build in C# with `GrypeReadJson`.
- Pushing a `vX.Y.Z` tag publishes exactly that version to nuget.org (NuGet trusted publishing) and creates a GitHub Release with generated notes; a tag containing `-` produces a prerelease.
- Untagged builds get MinVer alpha versions (`X.Y.Z-alpha.0.N`) and are never published.
- The first release is `v0.1.0`.

## Decisions

| Topic | Decision |
|---|---|
| Approach | Mirror Cake.CycloneDX: a Cake Frosting build project in `build/`, a separate Frosting dogfood project, three GitHub workflows, MinVer, NuGet trusted publishing, `gh release create --generate-notes`, the same release policy. |
| Dogfood target | `ProjectReference` to `src/Cake.Grype/Cake.Grype.csproj` — always the commit being built, never a published Cake.Grype. The only package reference is **Cake.CycloneDX 0.0.5**, used to produce the SBOM. |
| Dogfood source | SBOM chain: Cake.CycloneDX `CdxDotNet` on **`Cake.Grype.sln`** (the whole solution: add-in, tests, dogfood project — 53 components when probed) → `GrypeScanSbom` → `GrypeReadJson`. Exercises requirements A (table in the log), B (JSON artifact) and C (C# gate). Decided after probing: the add-in project alone yields an empty SBOM with development dependencies excluded (its only package, `Cake.Core`, is `PrivateAssets=All`) and a single component without. |
| Dogfood gate | Fail on any known-exploited match, or any `High`/`Critical` match with fix state `Fixed`. `Unknown`-severity matches are logged, never fail. Always log counts per severity. |
| Package verification | The dogfood uses a project reference, so the `Pack` task verifies the `.nupkg` instead (a `PackageVerifier` class in `build/`): `lib/net8.0`, `lib/net9.0`, `lib/net10.0` each with `Cake.Grype.dll` and `Cake.Grype.xml`; `icon.png`; `README.md`; nuspec tag `cake-addin`; no `Cake.Core` dependency. |
| Central Package Management | Adopted. `Directory.Packages.props` at the **repo root** (covers `src/`, `tests/` and `build/`). Project files keep version-less `PackageReference`s; `Cake.Core` keeps `PrivateAssets="All"`. |
| Library build settings | `src/Directory.Build.props` (scoped to `src/`): MinVer, `Deterministic`, `ContinuousIntegrationBuild` when `GITHUB_ACTIONS == true`, SourceLink (`PublishRepositoryUrl`, `EmbedUntrackedSources`), `DebugType` embedded, NuGet audit (`all`/`low`), `TreatWarningsAsErrors` in Release. `VersionPrefix`, `IncludeSymbols` and `SymbolPackageFormat` are removed from `Cake.Grype.csproj` (symbols are embedded in the DLL, so no `.snupkg` is produced). |
| Not adopted | StyleCop (would require re-validating every file against a new ruleset; out of scope). |
| Layout | Unchanged: `Cake.Grype.sln`, `src/`, `tests/` at the root. The dogfood project is added to the solution. |
| CI matrix | PR and `main`: `windows-latest`, `ubuntu-latest`, `macos-latest` (Cake.CycloneDX runs PRs on Windows only; Grype path handling is OS-specific and the repository is public). Release: `windows-latest`. |
| Grype on CI | `anchore/scan-action/download-grype` (supports Windows/Linux/macOS; outputs the executable path as `cmd`; `cache-db: true`). The path is passed to the dogfood as `GRYPE_PATH` and used as `ToolPath`; locally Grype is resolved from `PATH`. |
| Cake.Frosting version | **6.3.0** for both Frosting projects. Probed: 6.1.0 (Cake.CycloneDX's version) pulls `NuGet.Packaging`/`NuGet.Protocol` 7.3.0, which carry a low-severity advisory (GHSA-g4vj-cjjj-v7hg); with NuGet audit level `low` and warnings-as-errors the Release build fails with NU1901. 6.2.0 and 6.3.0 build clean. |
| SDK | `global.json` gains `"sdk": { "version": "10.0.100", "rollForward": "latestFeature" }`, keeping `"test": { "runner": "Microsoft.Testing.Platform" }`. |

## Research findings

- **Cake.CycloneDX** (sibling, `C:\Dev\GitHub\mgnslndh\Cake.CycloneDX`, tag `v0.0.5`): `build/Build.csproj` (Cake.Frosting 6.1.0, MinVer 7.0.0 for display via `AssemblyMetadata.Generators`, `RunWorkingDirectory` = repo root), `build/MinVer.props` (`MinVerTagPrefix` `v`, `MinVerDefaultPreReleaseIdentifiers` `alpha.0`), tasks `Default`, `Build`, `Test`, `Pack`, `Dogfood`, `All`, `Publish` (`DotNetNuGetPush` to nuget.org with `NUGET_API_KEY`, `SkipDuplicate`), `Release` (`gh release create <GITHUB_REF_NAME> ./artifacts/*.nupkg --generate-notes`, `--prerelease --latest=false` if the tag contains `-`, otherwise `--verify-tag --fail-on-no-commits`). Workflows: `pr.yml`, `main.yml`, `release.yml` (tag `v*`, `environment: Production`, `permissions: contents: write, id-token: write`, `NuGet/login@v1` with `user: ${{ secrets.NUGET_USER }}` → `NUGET_API_KEY`). `docs/release-policy.md` defines tag/version rules; no `.github/release.yml` exists there yet although the policy recommends one.
- **Cake.CycloneDX on nuget.org:** `0.0.5` is the latest stable. Its `CdxDotNetSettings` has `Framework`, `Output` (`DirectoryPath`), `FileName`, `OutputFormat`, `ComponentName`, `ComponentVersion`, `ComponentType`, `ExcludeDevelopmentDependencies`, `ExcludeTestProjects`, `Recursive`, `IncludeProjectReferences`, `SpecVersion`. It runs the `CycloneDX` dotnet tool, whose latest version is **6.2.0** (6.x requires the .NET 10 SDK).
- **anchore/scan-action** latest `v7.4.2`; sub-action `download-grype` installs via Grype's `install.sh`, names the binary `grype.exe` on Windows, outputs `cmd` (absolute path), accepts `grype-version` and `cache-db`.
- **Probed on a scratch copy (2026-09-23):** with the files this spec describes, `build --target All` passed (Build, Test with 546 tests, Pack with verification, Dogfood with Generate-Sbom/Scan/Gate). Grype found no vulnerabilities in the solution SBOM. The gate's failing path, run as `--target Gate --exclusive` against the committed test fixture, failed with "2 blocking vulnerabilities" (the known-exploited CVE-2023-44487 and the fixed critical CVE-2025-15467). Cake 6's `DotNetTest` needs `PathType = Solution` (emits `--solution`) under the .NET 10 MTP runner. A `const string Configuration` on a `FrostingContext` hides `CakeContextAdapter.Configuration` (CS0108).
- **Cake.Grype on GitHub** (`mgnslndh/Cake.Grype`): public, default branch `main`, no tags, no environments (Cake.CycloneDX has `Production`).

## Section 1 — Repository changes

```
Directory.Packages.props            CPM: every package version
global.json                         + sdk 10.0.100 / latestFeature
build.ps1, build.sh                 dotnet run --project build/Build.csproj -- <args>
cake.config                         [Paths] Tools=./.cake
build/
  Build.csproj                      Cake.Frosting, MinVer (display), AssemblyMetadata.Generators; net10.0
  MinVer.props                      tag prefix "v", pre-release identifiers "alpha.0"
  Program.cs, BuildContext.cs, BuildLifetime.cs
  Tasks/DefaultTask.cs, BuildTask.cs, TestTask.cs, PackTask.cs, DogfoodTask.cs,
        AllTask.cs, PublishTask.cs, ReleaseTask.cs
src/
  Directory.Build.props             library/package build settings (see Decisions); imports ../build/MinVer.props
  Cake.Grype/Cake.Grype.csproj      MinVer reference (PrivateAssets All); VersionPrefix removed; versions via CPM
  Cake.Grype.Dogfooding.Build/      Frosting exe, net10.0
    Cake.Grype.Dogfooding.Build.csproj   ProjectReference ../Cake.Grype; PackageReference Cake.Frosting, Cake.CycloneDX
    Program.cs, BuildContext.cs
    Tasks/GenerateSbomTask.cs, ScanTask.cs, GateTask.cs, DefaultTask.cs
tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj   versions via CPM
.github/workflows/pr.yml, main.yml, release.yml
.github/release.yml                 changelog categories by label
docs/release-policy.md              copied from Cake.CycloneDX, names adjusted
README.md                           + "Building" section (build.ps1 targets, prerequisites)
```

- The `MinVer` package reference lives only in `Cake.Grype.csproj`; `src/Directory.Build.props` imports `build/MinVer.props` (properties only). The dogfood project inherits `src/Directory.Build.props` (so it must also build warning-free in Release) and sets `IsPackable=false`.
- Existing tests (546 across three TFMs) must pass unchanged after the CPM and `Directory.Build.props` changes.

## Section 2 — Build project (`build/`)

All tasks operate on `Cake.Grype.sln` (or `src/Cake.Grype/Cake.Grype.csproj` for `Pack`) in `Release` configuration; the working directory is the repo root.

| Task | Does | Depends on |
|---|---|---|
| `Build` | `DotNetBuild("Cake.Grype.sln")`, Release | – |
| `Test` | `DotNetTest("Cake.Grype.sln")`, Release, `NoBuild`, `PathType = Solution` | Build |
| `Pack` | `DotNetPack("src/Cake.Grype/Cake.Grype.csproj")` → `./artifacts`, `NoBuild`; then verifies the package (below) | Build |
| `Dogfood` | `DotNetRun("src/Cake.Grype.Dogfooding.Build/…csproj")`, Release, `NoBuild`, `NoRestore` | Build |
| `All` | – | Test, Pack, Dogfood |
| `Default` | – | Build |
| `Publish` | Resolves the release package from the tag (below), then `DotNetNuGetPush` it to `https://api.nuget.org/v3/index.json` with `NUGET_API_KEY`, `SkipDuplicate`; throws `CakeException` if the key is missing | – |
| `Release` | `gh release create <GITHUB_REF_NAME> <release package path> --generate-notes` plus `--prerelease --latest=false` if the tag contains `-`, else `--verify-tag --fail-on-no-commits`; the package is passed as an explicit path (no shell expands globs for `StartProcess`) and a non-zero `gh` exit code throws | Publish |

**Release package resolution (`Publish`, `Release`):** `GITHUB_REF_NAME` must be a tag `v<version>`, and `artifacts/Cake.Grype.<version>.nupkg` must exist; otherwise `CakeException` naming the tag and what `artifacts` contains. This guarantees the published package version equals the tag.

**Package verification (`Pack`):** exactly one `Cake.Grype.*.nupkg` in `./artifacts`; the zip contains `lib/net8.0/Cake.Grype.dll`, `lib/net8.0/Cake.Grype.xml` and the same for `net9.0` and `net10.0`, `icon.png`, `README.md`; the nuspec contains the tag `cake-addin` and no `Cake.Core` dependency. Failure throws `CakeException` naming what is missing. `Pack` deletes `./artifacts/*.nupkg` before packing so stale packages cannot be published (it leaves `artifacts/dogfood/` alone).

`BuildLifetime.Setup` logs the MinVer package version.

## Section 3 — Dogfood project (`src/Cake.Grype.Dogfooding.Build`)

Frosting tasks, run in order by its `Default` task; output under `artifacts/dogfood/` (repo-root relative):

1. **`Generate-Sbom`** — `CdxDotNet("Cake.Grype.sln")` with `ComponentName = "Cake.Grype"`, `ComponentType = Library`, `OutputFormat = Json`, `Output = artifacts/dogfood`, `FileName = "Cake.Grype.cdx.json"`. Requires the `CycloneDX` dotnet tool (6.2.0) to be installed.
2. **`Scan`** — `GrypeScanSbom("artifacts/dogfood/Cake.Grype.cdx.json")` with `Outputs = { Table(), Json("artifacts/dogfood/grype.json") }`, `SortBy = Risk`, `ToolPath` = `GRYPE_PATH` when set.
3. **`Gate`** — `GrypeReadJson("artifacts/dogfood/grype.json")`; logs `CountBySeverity()`; logs each blocking match (package, version, vulnerability id, severity, risk); logs the number of `Unknown`-severity matches; throws `CakeException("<n> blocking vulnerabilities in Cake.Grype's dependencies")` when any match is known exploited, or has severity `>= High` with fix state `Fixed`.

`BuildContext` exposes `GrypePath` (from `GRYPE_PATH`, null when unset). An SBOM with no components or a report with no matches is a pass, not an error.

## Section 4 — Workflows and release

| Workflow | Trigger | Runners | Steps |
|---|---|---|---|
| `pr.yml` | `pull_request` | windows, ubuntu, macos (`fail-fast: false`) | checkout (`fetch-depth: 0` for MinVer) → setup-dotnet from `global.json` → `dotnet tool install -g CycloneDX --version 6.2.0` → `anchore/scan-action/download-grype@v7` (`cache-db: true`) → `dotnet run --project build/Build.csproj -- --target All` with `GRYPE_PATH` = the action's `cmd` → upload `artifacts/dogfood/` as `grype-dogfood-<os>` with `if: always()` |
| `main.yml` | push to `main` | same | same |
| `release.yml` | push tag `v*` | job `build`: windows-latest, `permissions: contents: read` — same setup and `--target All` (a tag that fails tests or the dogfood gate never publishes), uploads the package; job `publish` (`needs: build`): windows-latest, `environment: Production`, `permissions: contents: write, id-token: write` — downloads the package, `NuGet/login@v1` (`user: ${{ secrets.NUGET_USER }}`) → `--target Release` with `NUGET_API_KEY` from the login step and `GITHUB_TOKEN`. Every checkout uses `persist-credentials: false`. (Split after the final review so third-party build steps never run with publishing rights.) |

Common env: `DOTNET_SKIP_FIRST_TIME_EXPERIENCE`, `DOTNET_CLI_TELEMETRY_OPTOUT`. Action versions: `actions/checkout@v6`, `actions/setup-dotnet@v5`, `actions/upload-artifact@v4`, `anchore/scan-action/download-grype@v7`, `NuGet/login@v1`.

**`.github/release.yml`:** exclude labels `ignore-for-release`, `chore`, `documentation` and author `dependabot`; categories Breaking Changes (`breaking-change`), Features (`feature`, `enhancement`), Fixes (`bug`, `fix`), Other Changes (`*`).

**`docs/release-policy.md`:** Cake.CycloneDX's policy with the repository name adjusted; the GitHub Release configuration section points to the now-existing `.github/release.yml`.

### Manual prerequisites (owner only)

1. Create the GitHub environment `Production` in `mgnslndh/Cake.Grype`.
2. Add the secret `NUGET_USER` (the nuget.org account name) to that environment or the repository.
3. On nuget.org, add a Trusted Publishing policy for package `Cake.Grype`: owner `mgnslndh`, repository `Cake.Grype`, workflow file `release.yml`, environment `Production`.
4. Create the labels used by `.github/release.yml` (optional; unlabeled PRs land under "Other Changes").

## Error handling

- Missing `NUGET_API_KEY` / `GITHUB_REF_NAME` → `CakeException` with the variable name.
- Missing CycloneDX tool or Grype → Cake's standard "Could not locate executable" error from the respective add-in.
- Package verification failure → `CakeException` listing missing entries.
- Dogfood gate failure → `CakeException` with the count; blocking matches logged before it.

## Testing and verification

- Existing unit tests pass after CPM/`Directory.Build.props` (run through `./build.ps1 --target Test`).
- Local end to end: `./build.ps1 --target All` passes on the developer machine (Windows, Grype from `PATH`, CycloneDX tool installed), producing `artifacts/Cake.Grype.0.0.0-alpha.0.N.nupkg` (MinVer's version before the first tag) and `artifacts/dogfood/{Cake.Grype.cdx.json, grype.json}`, with Grype's table in the log.
- The failing path of the gate is verified once, manually and without code changes: copy the committed fixture `tests/Cake.Grype.Tests/TestData/grype-report.json` over `artifacts/dogfood/grype.json` and run the dogfood with `--target Gate --exclusive`; expect `2 blocking vulnerabilities in Cake.Grype's dependencies`.
- The release-package guard is verified locally without publishing: `Publish` with `GITHUB_REF_NAME=v9.9.9` must fail naming the missing `Cake.Grype.9.9.9.nupkg` before any push; with a matching tag and no `NUGET_API_KEY` it must fail on the missing key.
- Workflows are verified by the first pull request (all three OSes green, artifacts uploaded). `Publish`/`Release` are verified by the first `v0.1.0` tag, which the owner pushes after completing the manual prerequisites. No tag is pushed without the owner's explicit go-ahead.

## Out of scope

- StyleCop.
- Publishing alpha/CI packages anywhere.
- Dependabot/Renovate configuration.
- Signing packages.
- Any change to Cake.Grype's public API or behaviour.
