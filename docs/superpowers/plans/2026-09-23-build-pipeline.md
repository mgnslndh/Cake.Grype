# Cake.Grype Build and Release Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build, test, pack, dogfood and publish Cake.Grype through a Cake Frosting build and GitHub workflows that mirror Cake.CycloneDX.

**Architecture:** Central Package Management and a `src/Directory.Build.props` (MinVer, SourceLink, deterministic, warnings-as-errors) make the library build releasable. A Frosting dogfood project references the Cake.Grype **project** and runs Cake.CycloneDX → `GrypeScanSbom` → `GrypeReadJson` gate. A Frosting build project in `build/` orchestrates Build/Test/Pack(+verify)/Dogfood/Publish/Release, and three workflows run it on pull requests, `main` and `v*` tags.

**Tech Stack:** .NET SDK 10 (`global.json`), Cake.Frosting 6.3.0, MinVer 7.0.0, AssemblyMetadata.Generators 2.1.0, Cake.CycloneDX 0.0.5 + CycloneDX dotnet tool 6.2.0, Grype (winget locally; `anchore/scan-action/download-grype@v7` on CI), GitHub Actions, NuGet trusted publishing, GitHub CLI.

**Spec:** `docs/superpowers/specs/2026-09-23-build-pipeline-design.md`

## Global Constraints

- Branch `feature/build-pipeline` (already created; holds the spec and this plan). Never push, tag, or open a PR — the owner does that.
- The user's untracked `docs/grype-usage.txt` and `etc/` must never be staged, modified or deleted.
- Central Package Management: `Directory.Packages.props` at the **repo root**; every `PackageReference` is version-less. Versions: `AssemblyMetadata.Generators` 2.1.0, `Cake.Core` 6.0.0, `Cake.CycloneDX` 0.0.5, `Cake.Frosting` **6.3.0** (6.1.0 fails the audit: NU1901 via NuGet.Packaging/Protocol 7.3.0, GHSA-g4vj-cjjj-v7hg), `Cake.Testing` 6.0.0, `MinVer` 7.0.0, `NSubstitute` 5.3.0, `xunit.v3` 4.0.1.
- `src/Directory.Build.props` applies only to `src/` (add-in and dogfood project), never to `tests/` or `build/`.
- Release builds must be warning-free (warnings are errors under `src/`; `build/` and `tests/` must also produce zero warnings).
- MinVer: tag prefix `v`, default pre-release identifiers `alpha.0`. The `MinVer` package is referenced by `src/Cake.Grype/Cake.Grype.csproj` (PrivateAssets All) and `build/Build.csproj` (display only).
- Symbols are embedded (`DebugType` embedded): no `.snupkg`; `IncludeSymbols`/`SymbolPackageFormat`/`VersionPrefix` are removed from `Cake.Grype.csproj`.
- The dogfood project uses `ProjectReference` to `src/Cake.Grype/Cake.Grype.csproj` — never a Cake.Grype package.
- Dogfood outputs live in `artifacts/dogfood/` (`Cake.Grype.cdx.json`, `grype.json`); packages in `artifacts/*.nupkg` (`artifacts/` is already git-ignored).
- Dogfood gate: block on `IsKnownExploited`, or `Severity >= High` with `Vulnerability.Fix.State == Fixed`; log counts per severity and the `Unknown` count; never block on `Unknown`.
- Grype on CI is located through the environment variable `GRYPE_PATH` (used as `ToolPath`); locally Grype comes from `PATH`.
- `DotNetTest` must use `PathType = DotNetTestPathType.Solution` (the .NET 10 MTP runner requires `--solution`).
- Do not name a constant `Configuration` on a `FrostingContext` (CS0108); use `BuildConfiguration`.
- Use the Bash tool (Git Bash); `python` is fine, `python3` is not. Commit messages end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Local prerequisites (already present on this machine): .NET SDK 10.0.x, CycloneDX global tool 6.2.0 (`dotnet tool install -g CycloneDX --version 6.2.0`), Grype on `PATH` with a current DB, GitHub CLI.

## Review Focus

1. **Stale package in `artifacts/`** (e.g. a `0.0.0-alpha` package left from a local run) — `Pack` must delete old `*.nupkg` first so exactly one package is verified and published. Pinned in Task 3 Step 5.
2. **Tag that does not match the built package** (tag `v1.2.3` but MinVer produced something else, or `GITHUB_REF_NAME` is a branch) — `Publish` must refuse before pushing, naming the tag and what `artifacts/` contains. Pinned in Task 3 Step 6.
3. **`GRYPE_PATH` unset or empty** — the dogfood must fall back to Grype on `PATH`, not pass an empty `ToolPath`. Pinned in Task 2 Step 4 (local run has no `GRYPE_PATH`) and Task 2 Step 5 (explicit empty value).
4. **A report with blocking findings** — the gate must fail with the count and log each blocking match; `Unknown` must not block. Pinned in Task 2 Step 6 with the committed fixture.
5. **`gh release create` failing** (e.g. tag without commits, auth failure) — `Release` must throw on a non-zero exit code rather than report success. Pinned by code review of Task 3's `ReleaseTask` (cannot be exercised locally without creating a real release); called out in Task 3 Step 7.

## File Structure

```
Directory.Packages.props                         (Task 1) CPM versions
global.json                                      (Task 1) + sdk
src/Directory.Build.props                        (Task 1) library build settings
build/MinVer.props                               (Task 1) MinVer settings (imported by src/Directory.Build.props and build/Build.csproj)
src/Cake.Grype/Cake.Grype.csproj                 (Task 1) MinVer, CPM, remove VersionPrefix/IncludeSymbols/SymbolPackageFormat
tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj   (Task 1) CPM
src/Cake.Grype.Dogfooding.Build/                 (Task 2) Frosting dogfood: csproj, Program.cs, BuildContext.cs,
                                                          Tasks/GenerateSbomTask.cs, ScanTask.cs, GateTask.cs, DefaultTask.cs
Cake.Grype.sln                                   (Task 2) + dogfood project
build/Build.csproj, Program.cs, BuildContext.cs, BuildLifetime.cs, PackageVerifier.cs     (Task 3)
build/Tasks/BuildTask.cs, TestTask.cs, PackTask.cs, DogfoodTask.cs, AllTask.cs, DefaultTask.cs,
            PublishTask.cs, ReleaseTask.cs                                              (Task 3)
build.ps1, build.sh, cake.config                 (Task 3)
.gitignore                                       (Task 3) + .cake/
README.md                                        (Task 3) + "Building" section
.github/workflows/pr.yml, main.yml, release.yml  (Task 4)
.github/release.yml                              (Task 4)
docs/release-policy.md                           (Task 4)
```

Everything in this plan was probed on a scratch copy of the repository on 2026-09-23: `build --target All` passed (546 tests, package verified, dogfood Generate-Sbom/Scan/Gate), the gate failed as expected on the fixture, and `Publish` refused mismatched tags.

---

### Task 1: Central Package Management, MinVer and library build settings

**Files:**
- Create: `Directory.Packages.props`, `src/Directory.Build.props`, `build/MinVer.props`
- Modify: `global.json`, `src/Cake.Grype/Cake.Grype.csproj`, `tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj`

**Interfaces:**
- Produces: CPM versions for every package used by Tasks 2–3; `build/MinVer.props` (imported by Task 3's `build/Build.csproj`); `src/Directory.Build.props` (inherited by Task 2's dogfood project).

- [ ] **Step 1: Record the baseline**

Run: `dotnet build Cake.Grype.sln -c Release 2>&1 | tail -3 && dotnet test Cake.Grype.sln -c Release --no-build 2>&1 | grep -E "total:|failed:"`
Expected: `0 Warning(s)`, `0 Error(s)`, `total: 546`, `failed: 0`.

- [ ] **Step 2: Add the central package versions and MinVer settings**

**File:** `Directory.Packages.props`

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="AssemblyMetadata.Generators" Version="2.1.0" />
    <PackageVersion Include="Cake.Core" Version="6.0.0" />
    <PackageVersion Include="Cake.CycloneDX" Version="0.0.5" />
    <PackageVersion Include="Cake.Frosting" Version="6.3.0" />
    <PackageVersion Include="Cake.Testing" Version="6.0.0" />
    <PackageVersion Include="MinVer" Version="7.0.0" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="xunit.v3" Version="4.0.1" />
  </ItemGroup>

</Project>
```

**File:** `build/MinVer.props`

```xml
<Project>
  <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
    <MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
  </PropertyGroup>
</Project>
```

**File:** `src/Directory.Build.props`

```xml
<Project>

  <Import Project="$(MSBuildThisFileDirectory)..\build\MinVer.props" />

  <PropertyGroup>
    <Deterministic>true</Deterministic>
    <DebugType>embedded</DebugType>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>low</NuGetAuditLevel>
  </PropertyGroup>

  <PropertyGroup Label="Source Link">
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
  </PropertyGroup>

  <PropertyGroup Label="Deterministic Build" Condition="'$(GITHUB_ACTIONS)' == 'true'">
    <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

</Project>
```

**File:** `global.json` (replace)

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

- [ ] **Step 3: Switch the projects to CPM and MinVer**

In `src/Cake.Grype/Cake.Grype.csproj`:
- delete the line `<VersionPrefix>0.1.0</VersionPrefix>`;
- delete the lines `<IncludeSymbols>true</IncludeSymbols>` and `<SymbolPackageFormat>snupkg</SymbolPackageFormat>`;
- replace `<PackageReference Include="Cake.Core" Version="6.0.0" PrivateAssets="All" />` with:

```xml
    <PackageReference Include="Cake.Core" PrivateAssets="All" />
    <PackageReference Include="MinVer" PrivateAssets="All" />
```

In `tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj`, remove the `Version="…"` attribute from the three package references so they read:

```xml
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Cake.Testing" />
    <PackageReference Include="NSubstitute" />
```

- [ ] **Step 4: Build, test and pack**

Run:
```bash
dotnet build Cake.Grype.sln -c Release 2>&1 | tail -3
dotnet test Cake.Grype.sln -c Release --no-build 2>&1 | grep -E "total:|failed:"
rm -rf artifacts && dotnet pack src/Cake.Grype/Cake.Grype.csproj -c Release --no-build -o artifacts 2>&1 | tail -2
ls artifacts && unzip -l artifacts/*.nupkg
```
Expected: `0 Warning(s)`, `0 Error(s)`; `total: 546`, `failed: 0`; exactly one file `artifacts/Cake.Grype.0.0.0-alpha.0.<N>.nupkg` (MinVer's version before the first tag; `<N>` is the commit height), **no** `.snupkg`; the zip lists `lib/net8.0/Cake.Grype.dll`, `lib/net8.0/Cake.Grype.xml`, the same for `net9.0` and `net10.0`, `icon.png`, `README.md`. Then `rm -rf artifacts`.

If the build reports `NU190x` (a vulnerable package) as an error, stop and report it with the package name and advisory — do not lower the audit level.

- [ ] **Step 5: Commit**

```bash
git add Directory.Packages.props global.json src/Directory.Build.props build/MinVer.props src/Cake.Grype/Cake.Grype.csproj tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj
git commit -m "build: adopt central package management, MinVer and release build settings"
```

---

### Task 2: Dogfood project

**Files:**
- Create: `src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj`, `Program.cs`, `BuildContext.cs`, `Tasks/GenerateSbomTask.cs`, `Tasks/ScanTask.cs`, `Tasks/GateTask.cs`, `Tasks/DefaultTask.cs`
- Modify: `Cake.Grype.sln` (add the project)

**Interfaces:**
- Consumes: Task 1's CPM versions (`Cake.Frosting`, `Cake.CycloneDX`) and `src/Directory.Build.props`. Cake.Grype public API: `GrypeScanSbom(FilePath, GrypeScanSettings)`, `GrypeOutput.Table()`, `GrypeOutput.Json(FilePath)`, `GrypeSortBy.Risk`, `GrypeReadJson(FilePath)`, `GrypeReport.CountBySeverity()`, `GrypeMatch.{Severity, Risk, IsKnownExploited, Artifact, Vulnerability}`, `GrypeFixState.Fixed`, `GrypeSeverity`. Cake.CycloneDX 0.0.5: `CdxDotNet(FilePath, CdxDotNetSettings)`, `CdxComponentClassification.Library`, `CdxDotNetOutputFormat.Json` (namespace `Cake.CycloneDX.Tools.CdxDotNet`).
- Produces: `dotnet run --project src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj` (run from the repo root; `RunWorkingDirectory` makes the repo root the working directory) writes `artifacts/dogfood/Cake.Grype.cdx.json` and `artifacts/dogfood/grype.json`, exits non-zero when the gate blocks. Environment variable `GRYPE_PATH` overrides the Grype executable. Task 3's `Dogfood` task runs it with `NoBuild`.

- [ ] **Step 1: Create the project files**

**File:** `src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <RunWorkingDirectory>$(MSBuildProjectDirectory)\..\..</RunWorkingDirectory>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Cake.CycloneDX" />
    <PackageReference Include="Cake.Frosting" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Cake.Grype\Cake.Grype.csproj" />
  </ItemGroup>

</Project>
```

**File:** `src/Cake.Grype.Dogfooding.Build/Program.cs`

```csharp
using Cake.Frosting;

namespace Cake.Grype.Dogfooding.Build
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .Run(args);
        }
    }
}
```

**File:** `src/Cake.Grype.Dogfooding.Build/BuildContext.cs`

```csharp
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Cake.Grype.Dogfooding.Build
{
    public sealed class BuildContext : FrostingContext
    {
        public BuildContext(ICakeContext context)
            : base(context)
        {
            OutputDirectory = context.Environment.WorkingDirectory.Combine("artifacts/dogfood");
            SbomFile = OutputDirectory.CombineWithFilePath("Cake.Grype.cdx.json");
            ReportFile = OutputDirectory.CombineWithFilePath("grype.json");

            var grypePath = context.Environment.GetEnvironmentVariable("GRYPE_PATH");
            GrypePath = string.IsNullOrWhiteSpace(grypePath) ? null : new FilePath(grypePath);
        }

        /// <summary>Gets the directory the SBOM and the Grype report are written to.</summary>
        public DirectoryPath OutputDirectory { get; }

        /// <summary>Gets the CycloneDX SBOM of the solution.</summary>
        public FilePath SbomFile { get; }

        /// <summary>Gets Grype's JSON report.</summary>
        public FilePath ReportFile { get; }

        /// <summary>Gets the Grype executable from GRYPE_PATH, or <c>null</c> to resolve Grype from PATH.</summary>
        public FilePath GrypePath { get; }
    }
}
```

**File:** `src/Cake.Grype.Dogfooding.Build/Tasks/GenerateSbomTask.cs`

```csharp
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxDotNet;
using Cake.Frosting;

namespace Cake.Grype.Dogfooding.Build.Tasks
{
    /// <summary>
    /// Generates a CycloneDX SBOM of the whole solution (add-in, tests, dogfood project) with Cake.CycloneDX.
    /// </summary>
    [TaskName("Generate-Sbom")]
    public sealed class GenerateSbomTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.EnsureDirectoryExists(context.OutputDirectory);
            context.CdxDotNet(new FilePath("Cake.Grype.sln"), new CdxDotNetSettings
            {
                ComponentName = "Cake.Grype",
                ComponentType = CdxComponentClassification.Library,
                OutputFormat = CdxDotNetOutputFormat.Json,
                Output = context.OutputDirectory,
                FileName = context.SbomFile.GetFilename().FullPath,
            });
        }
    }
}
```

**File:** `src/Cake.Grype.Dogfooding.Build/Tasks/ScanTask.cs`

```csharp
using Cake.Frosting;
using Cake.Grype.Scan;

namespace Cake.Grype.Dogfooding.Build.Tasks
{
    /// <summary>
    /// Scans the SBOM with Grype: the table goes to the log, the JSON report to a file (the CI artifact).
    /// </summary>
    [TaskName("Scan")]
    [IsDependentOn(typeof(GenerateSbomTask))]
    public sealed class ScanTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.GrypeScanSbom(context.SbomFile, new GrypeScanSettings
            {
                ToolPath = context.GrypePath,
                Outputs = { GrypeOutput.Table(), GrypeOutput.Json(context.ReportFile) },
                SortBy = GrypeSortBy.Risk,
            });
        }
    }
}
```

**File:** `src/Cake.Grype.Dogfooding.Build/Tasks/GateTask.cs`

```csharp
using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;
using Cake.Grype.Json;

namespace Cake.Grype.Dogfooding.Build.Tasks
{
    /// <summary>
    /// Fails the build on known-exploited findings, or High/Critical findings that have a fix.
    /// Unknown (unassessed) findings are reported but never block.
    /// </summary>
    [TaskName("Gate")]
    [IsDependentOn(typeof(ScanTask))]
    public sealed class GateTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var report = context.GrypeReadJson(context.ReportFile);

            foreach (var count in report.CountBySeverity())
            {
                context.Information("{0}: {1}", count.Key, count.Value);
            }

            context.Information(
                "Unassessed (Unknown severity, not gated): {0}",
                report.Matches.Count(match => match.Severity == GrypeSeverity.Unknown));

            var blocking = report.Matches.Where(IsBlocking).ToList();
            foreach (var match in blocking)
            {
                context.Error(
                    "{0} {1}: {2} ({3}, risk {4:0.0}{5})",
                    match.Artifact?.Name,
                    match.Artifact?.Version,
                    match.Vulnerability?.Id,
                    match.Severity,
                    match.Risk,
                    match.IsKnownExploited ? ", known exploited" : string.Empty);
            }

            if (blocking.Count > 0)
            {
                throw new CakeException($"{blocking.Count} blocking vulnerabilities in Cake.Grype's dependencies");
            }

            context.Information("No blocking vulnerabilities.");
        }

        private static bool IsBlocking(GrypeMatch match)
        {
            return match.IsKnownExploited
                || (match.Severity >= GrypeSeverity.High && match.Vulnerability?.Fix?.State == GrypeFixState.Fixed);
        }
    }
}
```

**File:** `src/Cake.Grype.Dogfooding.Build/Tasks/DefaultTask.cs`

```csharp
using Cake.Frosting;

namespace Cake.Grype.Dogfooding.Build.Tasks
{
    [TaskName("Default")]
    [IsDependentOn(typeof(GateTask))]
    public sealed class DefaultTask : FrostingTask
    {
    }
}
```

Note: Cake's logger does not support alignment specifiers such as `{0,-10}` in `Information(...)` format strings — keep the formats as written.

- [ ] **Step 2: Add the project to the solution**

```bash
dotnet sln Cake.Grype.sln add src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj
```

- [ ] **Step 3: Build in Release**

Run: `dotnet build Cake.Grype.sln -c Release 2>&1 | tail -3`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 4: Run the dogfood (Grype from PATH)**

Run (from the repo root, with `GRYPE_PATH` unset): `dotnet run --project src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj -c Release --no-build`
Expected: tasks `Generate-Sbom`, `Scan`, `Gate`, `Default` all `Succeeded`; the log shows Grype's output (`No vulnerabilities found` when the solution has no known vulnerabilities, otherwise Grype's table), then six severity count lines, `Unassessed (Unknown severity, not gated): <n>` and `No blocking vulnerabilities.`; `artifacts/dogfood/Cake.Grype.cdx.json` (tens of KB, ~50 components) and `artifacts/dogfood/grype.json` exist.

If Grype reports a real blocking vulnerability in a dependency, stop and report it (package, version, CVE) — do not change the gate.

- [ ] **Step 5: `GRYPE_PATH` handling**

Run:
```bash
GRYPE_PATH="" dotnet run --project src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj -c Release --no-build -- --target Scan --exclusive
GRYPE_PATH="$(cygpath -m "$(command -v grype)")" dotnet run --project src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj -c Release --no-build -- --target Scan --exclusive
GRYPE_PATH="C:/does/not/exist/grype.exe" dotnet run --project src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj -c Release --no-build -- --target Scan --exclusive; echo "exit=$?"
```
Expected: the first two succeed (empty value falls back to `PATH`; an explicit path is used); the third fails with `Error: Grype: Could not locate executable.` and a non-zero exit code — proving `GRYPE_PATH` is honoured. (`--exclusive` runs only `Scan`, reusing the SBOM from Step 4.)

- [ ] **Step 6: Gate failing path (manual, no code change)**

Run:
```bash
cp tests/Cake.Grype.Tests/TestData/grype-report.json artifacts/dogfood/grype.json
dotnet run --project src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj -c Release --no-build -- --target Gate --exclusive; echo "exit=$?"
```
Expected: severity counts `High: 1`, `Critical: 1`, `Unassessed (Unknown severity, not gated): 1`; two error lines `varnish 7.6.0: CVE-2023-44487 (High, risk 78.8, known exploited)` and `libssl3 3.0.14-1~deb12u2: CVE-2025-15467 (Critical, risk 45.3)`; `Error: 2 blocking vulnerabilities in Cake.Grype's dependencies`; non-zero exit. The reserved `CVE-2026-53613` (Unknown) and the Low `CVE-2016-2781` must **not** be listed as blocking. Then `rm -rf artifacts`.

- [ ] **Step 7: Run the unit tests**

Run: `dotnet test Cake.Grype.sln -c Release --no-build 2>&1 | grep -E "total:|failed:"`
Expected: `total: 546`, `failed: 0`.

- [ ] **Step 8: Commit**

```bash
git add Cake.Grype.sln src/Cake.Grype.Dogfooding.Build
git commit -m "build: add dogfood project scanning the solution SBOM with Cake.Grype"
```

---

### Task 3: Build project, scripts and README

**Files:**
- Create: `build/Build.csproj`, `build/Program.cs`, `build/BuildContext.cs`, `build/BuildLifetime.cs`, `build/PackageVerifier.cs`, `build/Tasks/BuildTask.cs`, `build/Tasks/TestTask.cs`, `build/Tasks/PackTask.cs`, `build/Tasks/DogfoodTask.cs`, `build/Tasks/AllTask.cs`, `build/Tasks/DefaultTask.cs`, `build/Tasks/PublishTask.cs`, `build/Tasks/ReleaseTask.cs`, `build.ps1`, `build.sh`, `cake.config`
- Modify: `.gitignore` (add `.cake/`), `README.md` (add "Building")

**Interfaces:**
- Consumes: Task 1's `build/MinVer.props` and CPM versions (`Cake.Frosting`, `MinVer`, `AssemblyMetadata.Generators`); Task 2's dogfood project path `src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj`.
- Produces: `dotnet run --project build/Build.csproj -- --target <Default|Build|Test|Pack|Dogfood|All|Publish|Release>` (also `./build.ps1` / `./build.sh`). Environment: `NUGET_API_KEY` (Publish), `GITHUB_REF_NAME` (Publish, Release), `GITHUB_TOKEN` (for `gh`, Release), `GRYPE_PATH` (inherited by the dogfood). Task 4's workflows call `--target All` and `--target Release`.

The `build/` project is **not** added to `Cake.Grype.sln` (as in Cake.CycloneDX): `dotnet build Cake.Grype.sln` must not build the build project.

- [ ] **Step 1: Create the project, entry point and context**

**File:** `build/Build.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Import Project="MinVer.props" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RunWorkingDirectory>$(MSBuildProjectDirectory)\..</RunWorkingDirectory>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AssemblyMetadata.Generators" PrivateAssets="All" />
    <PackageReference Include="Cake.Frosting" />
    <PackageReference Include="MinVer" PrivateAssets="All" />
  </ItemGroup>

  <Target Name="PostMinVer" AfterTargets="MinVer">
    <ItemGroup>
      <AssemblyMetadata Include="PackageVersion" Value="$(PackageVersion)" />
    </ItemGroup>
  </Target>

</Project>
```

**File:** `build/Program.cs`

```csharp
using Cake.Frosting;

namespace Build
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .UseLifetime<BuildLifetime>()
                .Run(args);
        }
    }
}
```

**File:** `build/BuildContext.cs`

```csharp
using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    public sealed class BuildContext : FrostingContext
    {
        public const string BuildConfiguration = "Release";
        public const string Solution = "Cake.Grype.sln";
        public const string LibraryProject = "src/Cake.Grype/Cake.Grype.csproj";
        public const string DogfoodProject = "src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj";

        public BuildContext(ICakeContext context)
            : base(context)
        {
            ArtifactsDirectory = context.Environment.WorkingDirectory.Combine("artifacts");
        }

        /// <summary>Gets the directory packages are written to.</summary>
        public DirectoryPath ArtifactsDirectory { get; }

        /// <summary>Gets the glob matching packages in <see cref="ArtifactsDirectory"/>.</summary>
        public string PackagePattern => ArtifactsDirectory.CombineWithFilePath("*.nupkg").FullPath;

        public string NuGetApiKey => Environment.GetEnvironmentVariable("NUGET_API_KEY");

        public string GitHubRefName => Environment.GetEnvironmentVariable("GITHUB_REF_NAME");

        /// <summary>
        /// Gets the package matching the pushed tag: GITHUB_REF_NAME <c>v1.2.3</c> requires
        /// <c>artifacts/Cake.Grype.1.2.3.nupkg</c>. Guarantees the published version equals the tag.
        /// </summary>
        /// <returns>The package path.</returns>
        public FilePath ResolveReleasePackage()
        {
            var tag = GitHubRefName;
            if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith('v'))
            {
                throw new CakeException($"GITHUB_REF_NAME must be a version tag like v1.2.3 (was '{tag}').");
            }

            var expected = ArtifactsDirectory.CombineWithFilePath($"Cake.Grype.{tag.Substring(1)}.nupkg");
            if (!this.FileExists(expected))
            {
                var found = string.Join(", ", this.GetFiles(PackagePattern).Select(file => file.GetFilename().FullPath));
                throw new CakeException(
                    $"Tag {tag} requires {expected.GetFilename()}, but artifacts contains: {(found.Length == 0 ? "(nothing)" : found)}.");
            }

            this.Information("Release package: {0}", expected.GetFilename());
            return expected;
        }
    }
}
```

**File:** `build/BuildLifetime.cs`

```csharp
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Build
{
    public sealed class BuildLifetime : FrostingLifetime<BuildContext>
    {
        public override void Setup(BuildContext context, ISetupContext info)
        {
            context.Information("Package version: {0}", ThisAssembly.PackageVersion);
        }

        public override void Teardown(BuildContext context, ITeardownContext info)
        {
        }
    }
}
```

**File:** `build/PackageVerifier.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Build
{
    /// <summary>
    /// Checks a packed Cake.Grype .nupkg for the content a Cake add-in package must have.
    /// </summary>
    public static class PackageVerifier
    {
        private static readonly string[] TargetFrameworks = { "net8.0", "net9.0", "net10.0" };

        /// <summary>
        /// Verifies a package.
        /// </summary>
        /// <param name="packagePath">The .nupkg file.</param>
        /// <returns>The problems found; empty when the package is valid.</returns>
        public static IReadOnlyList<string> Verify(string packagePath)
        {
            var problems = new List<string>();

            using var package = ZipFile.OpenRead(packagePath);
            var entries = new HashSet<string>(package.Entries.Select(entry => entry.FullName), StringComparer.OrdinalIgnoreCase);

            foreach (var framework in TargetFrameworks)
            {
                RequireEntry(entries, $"lib/{framework}/Cake.Grype.dll", problems);
                RequireEntry(entries, $"lib/{framework}/Cake.Grype.xml", problems);
            }

            RequireEntry(entries, "icon.png", problems);
            RequireEntry(entries, "README.md", problems);

            var nuspecEntry = package.Entries.SingleOrDefault(
                entry => !entry.FullName.Contains('/') && entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
            if (nuspecEntry == null)
            {
                problems.Add("missing .nuspec");
                return problems;
            }

            using var stream = nuspecEntry.Open();
            var nuspec = XDocument.Load(stream);
            var ns = nuspec.Root.Name.Namespace;

            var tags = (string)nuspec.Root.Element(ns + "metadata")?.Element(ns + "tags") ?? string.Empty;
            if (!tags.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("cake-addin"))
            {
                problems.Add("nuspec tags do not contain 'cake-addin'");
            }

            if (nuspec.Descendants(ns + "dependency").Any(
                dependency => string.Equals((string)dependency.Attribute("id"), "Cake.Core", StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add("nuspec declares a dependency on Cake.Core (it must stay PrivateAssets=All)");
            }

            return problems;
        }

        private static void RequireEntry(HashSet<string> entries, string path, List<string> problems)
        {
            if (!entries.Contains(path))
            {
                problems.Add($"missing {path}");
            }
        }
    }
}
```

- [ ] **Step 2: Create the tasks**

**File:** `build/Tasks/BuildTask.cs`

```csharp
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName("Build")]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.DotNetBuild(BuildContext.Solution, new DotNetBuildSettings
            {
                Configuration = BuildContext.BuildConfiguration,
                Verbosity = DotNetVerbosity.Minimal,
            });
        }
    }
}
```

**File:** `build/Tasks/TestTask.cs`

```csharp
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Test;
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName("Test")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class TestTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.DotNetTest(BuildContext.Solution, new DotNetTestSettings
            {
                Configuration = BuildContext.BuildConfiguration,
                PathType = DotNetTestPathType.Solution,
                NoBuild = true,
                Verbosity = DotNetVerbosity.Minimal,
            });
        }
    }
}
```

**File:** `build/Tasks/PackTask.cs`

```csharp
using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks
{
    /// <summary>
    /// Packs Cake.Grype into ./artifacts (deleting older packages first) and verifies the package content.
    /// </summary>
    [TaskName("Pack")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PackTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.EnsureDirectoryExists(context.ArtifactsDirectory);
            context.DeleteFiles(context.PackagePattern);

            context.DotNetPack(BuildContext.LibraryProject, new DotNetPackSettings
            {
                Configuration = BuildContext.BuildConfiguration,
                NoBuild = true,
                OutputDirectory = context.ArtifactsDirectory,
                Verbosity = DotNetVerbosity.Minimal,
            });

            var packages = context.GetFiles(context.PackagePattern).ToList();
            if (packages.Count != 1)
            {
                throw new CakeException($"Expected exactly one package in {context.ArtifactsDirectory}, found {packages.Count}.");
            }

            var problems = PackageVerifier.Verify(packages[0].FullPath);
            if (problems.Count > 0)
            {
                throw new CakeException($"Package {packages[0].GetFilename()} is invalid: {string.Join("; ", problems)}");
            }

            context.Information("Verified {0}", packages[0].GetFilename());
        }
    }
}
```

**File:** `build/Tasks/DogfoodTask.cs`

```csharp
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Run;
using Cake.Frosting;

namespace Build.Tasks
{
    /// <summary>
    /// Runs the dogfood project: Cake.CycloneDX SBOM of the solution, Grype scan with the current Cake.Grype, C# gate.
    /// </summary>
    [TaskName("Dogfood")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class DogfoodTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.DotNetRun(BuildContext.DogfoodProject, new DotNetRunSettings
            {
                Configuration = BuildContext.BuildConfiguration,
                NoBuild = true,
                NoRestore = true,
            });
        }
    }
}
```

**File:** `build/Tasks/AllTask.cs`

```csharp
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName("All")]
    [IsDependentOn(typeof(TestTask))]
    [IsDependentOn(typeof(PackTask))]
    [IsDependentOn(typeof(DogfoodTask))]
    public sealed class AllTask : FrostingTask
    {
    }
}
```

**File:** `build/Tasks/DefaultTask.cs`

```csharp
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName("Default")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class DefaultTask : FrostingTask
    {
    }
}
```

**File:** `build/Tasks/PublishTask.cs`

```csharp
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks
{
    /// <summary>
    /// Pushes the package that matches the pushed tag to nuget.org. Requires NUGET_API_KEY and GITHUB_REF_NAME,
    /// and a package produced by a previous Pack in the same checkout.
    /// </summary>
    [TaskName("Publish")]
    public sealed class PublishTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var package = context.ResolveReleasePackage();
            if (string.IsNullOrWhiteSpace(context.NuGetApiKey))
            {
                throw new CakeException("NUGET_API_KEY environment variable is not set.");
            }

            context.DotNetNuGetPush(package, new DotNetNuGetPushSettings
            {
                ApiKey = context.NuGetApiKey,
                Source = "https://api.nuget.org/v3/index.json",
                SkipDuplicate = true,
            });
        }
    }
}
```

**File:** `build/Tasks/ReleaseTask.cs`

```csharp
using Cake.Common;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    /// <summary>
    /// Creates the GitHub Release for the pushed tag with generated notes and the package attached.
    /// A tag containing '-' (e.g. v1.2.0-preview.1) becomes a prerelease that is not marked latest.
    /// </summary>
    [TaskName("Release")]
    [IsDependentOn(typeof(PublishTask))]
    public sealed class ReleaseTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var package = context.ResolveReleasePackage();
            var tag = context.GitHubRefName;

            var arguments = new ProcessArgumentBuilder()
                .Append("release")
                .Append("create")
                .Append(tag)
                .AppendQuoted(package.FullPath)
                .Append("--generate-notes");

            if (tag.Contains('-'))
            {
                arguments.Append("--prerelease").Append("--latest=false");
            }
            else
            {
                arguments.Append("--verify-tag").Append("--fail-on-no-commits");
            }

            // StartProcess does not go through a shell, so the package is passed as an explicit path (no globs).
            var exitCode = context.StartProcess("gh", new ProcessSettings { Arguments = arguments });
            if (exitCode != 0)
            {
                throw new CakeException($"gh release create failed (exit code {exitCode}).");
            }
        }
    }
}
```

- [ ] **Step 3: Scripts, cake.config, .gitignore**

**File:** `build.ps1`

```powershell
dotnet run --project build/Build.csproj -- $args
exit $LASTEXITCODE
```

**File:** `build.sh`

```bash
#!/usr/bin/env bash
set -euo pipefail
dotnet run --project ./build/Build.csproj -- "$@"
```

**File:** `cake.config`

```ini
[Paths]
Tools=./.cake
```

Make `build.sh` executable in git and ignore Cake's tools folder:

```bash
git add build.sh && git update-index --chmod=+x build.sh
printf '\n# Cake tools\n.cake/\n' >> .gitignore
```

- [ ] **Step 4: Run the whole build**

Run: `./build.sh --target All`
Expected: `Package version: 0.0.0-alpha.0.<N>` in Setup; tasks `Build`, `Test` (546 tests passed), `Pack` (`Verified Cake.Grype.0.0.0-alpha.0.<N>.nupkg`), `Dogfood` (its `Generate-Sbom`, `Scan`, `Gate` succeed) all `Succeeded`; exit code 0. `dotnet build build/Build.csproj 2>&1 | tail -3` reports `0 Warning(s)`.

- [ ] **Step 5: Stale packages are removed (Review Focus 1)**

Run:
```bash
cp artifacts/Cake.Grype.*.nupkg artifacts/Cake.Grype.0.0.0-stale.nupkg
./build.sh --target Pack
ls artifacts/*.nupkg
```
Expected: `Pack` succeeds and only the freshly built `Cake.Grype.0.0.0-alpha.0.<N>.nupkg` remains (the stale copy was deleted before packing).

- [ ] **Step 6: The release guard refuses mismatched tags without publishing (Review Focus 2)**

Run:
```bash
GITHUB_REF_NAME=v9.9.9 NUGET_API_KEY=dummy ./build.sh --target Publish; echo "exit=$?"
GITHUB_REF_NAME=main NUGET_API_KEY=dummy ./build.sh --target Publish; echo "exit=$?"
V=$(ls artifacts/*.nupkg | sed 's/.*Cake\.Grype\.\(.*\)\.nupkg/\1/'); GITHUB_REF_NAME="v$V" NUGET_API_KEY= ./build.sh --target Publish; echo "exit=$?"
```
Expected (each with a non-zero exit and **no** push attempted):
1. `Error: Tag v9.9.9 requires Cake.Grype.9.9.9.nupkg, but artifacts contains: Cake.Grype.0.0.0-alpha.0.<N>.nupkg.`
2. `Error: GITHUB_REF_NAME must be a version tag like v1.2.3 (was 'main').`
3. `Release package: Cake.Grype.0.0.0-alpha.0.<N>.nupkg` then `Error: NUGET_API_KEY environment variable is not set.`

Never run `Publish` or `Release` with a real API key or token.

- [ ] **Step 7: Review `ReleaseTask` (Review Focus 5)**

`Release` cannot be exercised locally without creating a real GitHub Release. Confirm by reading the code that a non-zero `gh` exit code throws, that the package is passed as an explicit path, and that prerelease tags get `--prerelease --latest=false`. The first `v0.1.0` tag (pushed by the owner) verifies it end to end.

- [ ] **Step 8: README "Building" section**

Insert before `## License` in `README.md`:

````markdown
## Building

The build is a [Cake Frosting](https://cakebuild.net/docs/running-builds/runners/cake-frosting) project in `build/`:

```powershell
./build.ps1 --target All      # Windows
./build.sh --target All       # Linux/macOS
```

| Target | Does |
|---|---|
| `Default` / `Build` | Builds `Cake.Grype.sln` in Release |
| `Test` | Runs the unit tests |
| `Pack` | Packs `artifacts/Cake.Grype.<version>.nupkg` and verifies its content |
| `Dogfood` | Generates a CycloneDX SBOM of the solution with Cake.CycloneDX, scans it with this build of Cake.Grype, and fails on known-exploited or fixable High/Critical findings (report in `artifacts/dogfood/`) |
| `All` | Test, Pack and Dogfood |

Prerequisites: the .NET 10 SDK, Grype on `PATH` (or `GRYPE_PATH` set to the executable) and the CycloneDX tool (`dotnet tool install -g CycloneDX --version 6.2.0`). Versions come from git tags via MinVer; see [docs/release-policy.md](docs/release-policy.md) for how releases are made.

````

- [ ] **Step 9: Commit**

```bash
rm -rf artifacts
git add build build.ps1 build.sh cake.config .gitignore README.md
git commit -m "build: add Cake Frosting build with pack verification, publish and release"
```

---

### Task 4: GitHub workflows and release policy

**Files:**
- Create: `.github/workflows/pr.yml`, `.github/workflows/main.yml`, `.github/workflows/release.yml`, `.github/release.yml`, `docs/release-policy.md`

**Interfaces:**
- Consumes: Task 3's targets `All` and `Release`, and its environment variables (`GRYPE_PATH`, `NUGET_API_KEY`, `GITHUB_REF_NAME` — set by GitHub on tag pushes — and `GITHUB_TOKEN`).
- Produces: CI on pull requests and `main`; release on `v*` tags.

- [ ] **Step 1: Pull request and main workflows**

**File:** `.github/workflows/pr.yml`

```yaml
# yaml-language-server: $schema=https://json.schemastore.org/github-workflow.json
name: Pull Request
on: pull_request

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: [windows-latest, ubuntu-latest, macos-latest]

    steps:
      - name: Checkout
        uses: actions/checkout@v6
        with:
          fetch-depth: 0
      - name: Install .NET SDK
        uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - name: Install CycloneDX tool
        run: dotnet tool install -g CycloneDX --version 6.2.0
      - name: Install Grype
        id: grype
        uses: anchore/scan-action/download-grype@v7
        with:
          cache-db: true
      - name: Build, test, pack and dogfood
        env:
          GRYPE_PATH: ${{ steps.grype.outputs.cmd }}
        run: dotnet run --project build/Build.csproj -- --target All
      - name: Upload dogfood report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: grype-dogfood-${{ matrix.os }}
          path: artifacts/dogfood/
          if-no-files-found: ignore
```

**File:** `.github/workflows/main.yml` — identical to `pr.yml` except the first lines:

```yaml
# yaml-language-server: $schema=https://json.schemastore.org/github-workflow.json
name: CI
on:
  push:
    branches: [main]
```

(write the complete file: the `env:` and `jobs:` blocks are exactly those of `pr.yml`).

- [ ] **Step 2: Release workflow**

**File:** `.github/workflows/release.yml`

```yaml
# yaml-language-server: $schema=https://json.schemastore.org/github-workflow.json
name: Release
on:
  push:
    tags: ['v*']

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  publish:
    runs-on: windows-latest
    environment: Production
    permissions:
      contents: write
      id-token: write

    steps:
      - name: Checkout
        uses: actions/checkout@v6
        with:
          fetch-depth: 0
      - name: Install .NET SDK
        uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - name: Install CycloneDX tool
        run: dotnet tool install -g CycloneDX --version 6.2.0
      - name: Install Grype
        id: grype
        uses: anchore/scan-action/download-grype@v7
        with:
          cache-db: true
      - name: Build, test, pack and dogfood
        env:
          GRYPE_PATH: ${{ steps.grype.outputs.cmd }}
        run: dotnet run --project build/Build.csproj -- --target All
      - name: Upload dogfood report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: grype-dogfood-release
          path: artifacts/dogfood/
          if-no-files-found: ignore
      - name: NuGet login
        uses: NuGet/login@v1
        id: nuget-login
        with:
          user: ${{ secrets.NUGET_USER }}
      - name: Publish and create GitHub Release
        env:
          NUGET_API_KEY: ${{ steps.nuget-login.outputs.NUGET_API_KEY }}
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: dotnet run --project build/Build.csproj -- --target Release
```

- [ ] **Step 3: Release notes configuration**

**File:** `.github/release.yml`

```yaml
changelog:
  exclude:
    labels:
      - ignore-for-release
      - chore
      - documentation
    authors:
      - dependabot
  categories:
    - title: Breaking Changes
      labels:
        - breaking-change
    - title: Features
      labels:
        - feature
        - enhancement
    - title: Fixes
      labels:
        - bug
        - fix
    - title: Other Changes
      labels:
        - "*"
```

- [ ] **Step 4: Release policy**

Copy Cake.CycloneDX's policy and adapt it:

```bash
cp /c/Dev/GitHub/mgnslndh/Cake.CycloneDX/docs/release-policy.md docs/release-policy.md
```

Then edit `docs/release-policy.md`:
- Under `## GitHub Release Configuration`, replace the sentence `Add a \`.github/release.yml\` file to group pull requests into useful sections and exclude noise.` with `[.github/release.yml](../.github/release.yml) groups pull requests into sections and excludes noise:` and keep the example block.
- Append this section at the end:

```markdown
## Automation In This Repository

- Pull requests and pushes to `main` run `build --target All` (build, test, pack with package verification, dogfood scan) on Windows, Linux and macOS.
- Pushing a tag `vX.Y.Z` (or `vX.Y.Z-preview.N`) runs `.github/workflows/release.yml`: the same `All` build, then `Publish` (pushes `artifacts/Cake.Grype.X.Y.Z.nupkg` to nuget.org — it refuses if the package version does not equal the tag) and `Release` (creates the GitHub Release with generated notes; tags containing `-` become prereleases).

## Publishing Setup (one-time, repository owner)

1. Create the GitHub environment `Production` in `mgnslndh/Cake.Grype`.
2. Add the secret `NUGET_USER` (the nuget.org account name) to that environment.
3. On nuget.org, add a Trusted Publishing policy for `Cake.Grype`: owner `mgnslndh`, repository `Cake.Grype`, workflow file `release.yml`, environment `Production`.
4. Optionally create the labels used by `.github/release.yml` (`breaking-change`, `feature`, `enhancement`, `bug`, `fix`, `documentation`, `chore`, `ignore-for-release`).
```

Confirm nothing in the copied file still names Cake.CycloneDX: `grep -n "CycloneDX" docs/release-policy.md` → no output.

- [ ] **Step 5: Validate the YAML locally**

Run:
```bash
python - <<'EOF'
import yaml
for path in [".github/workflows/pr.yml", ".github/workflows/main.yml", ".github/workflows/release.yml", ".github/release.yml"]:
    data = yaml.safe_load(open(path, encoding="utf-8"))
    print(path, "ok", sorted(k for k in data if isinstance(k, str)))
pr = yaml.safe_load(open(".github/workflows/pr.yml", encoding="utf-8"))
main = yaml.safe_load(open(".github/workflows/main.yml", encoding="utf-8"))
assert pr["jobs"] == main["jobs"] and pr["env"] == main["env"], "pr.yml and main.yml jobs/env differ"
print("pr/main jobs identical")
EOF
```
Expected: four `ok` lines (PyYAML reads the key `on` as boolean `True`, so it is not in the printed list — that is expected) and `pr/main jobs identical`. The workflows themselves are verified by the first pull request on GitHub (all three OS jobs green, `grype-dogfood-<os>` artifacts uploaded) — the owner opens it; do not push.

- [ ] **Step 6: Commit**

```bash
git add .github docs/release-policy.md
git commit -m "ci: add pull request, main and release workflows with release policy"
```
