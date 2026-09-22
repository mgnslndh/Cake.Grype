# Cake.Grype Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A Cake add-in that runs Grype (scan, `db update/status/check/import/delete`, `version`) and reads Grype's JSON report into a typed model for gating builds in C#.

**Architecture:** An abstract `GrypeTool<TSettings> : Tool<TSettings>` (Cake.Core) resolves `grype`/`grype.exe`, builds Grype's global flags and offers a "run and parse JSON from stdout" helper. Each Grype command has its own runner and settings class; aliases live in `GrypeAliases.*.cs` partial files. `Cake.Grype.Json` holds the immutable report model and a streaming `System.Text.Json` reader and depends on nothing but the root namespace.

**Tech Stack:** C# / .NET (`net8.0;net9.0;net10.0`), `Cake.Core` 6.0.0, `System.Text.Json`, xUnit v3 (`xunit.v3` 4.0.1) on Microsoft Testing Platform, `Cake.Testing` 6.0.0, `NSubstitute` 5.3.0 (alias tests only).

**Spec:** `docs/superpowers/specs/2026-09-22-cake-grype-design.md`

## Global Constraints

- Package id `Cake.Grype`; one assembly; namespaces `Cake.Grype` (base, global settings, `GrypeSource`, `GrypeSeverity`, version command, all aliases), `Cake.Grype.Scan`, `Cake.Grype.Db`, `Cake.Grype.Json`.
- `Cake.Grype.Json` must never reference `Cake.Grype.Scan` or `Cake.Grype.Db` (architecture test, Task 7).
- Baseline `Cake.Core` **6.0.0** with `PrivateAssets="All"`. No reference to `Cake.Common`.
- Target frameworks `net8.0;net9.0;net10.0`. No `<Nullable>`, no `<ImplicitUsings>` in `src` (explicit `using`s, like Cake.DotNetOutdated). C# language version is the SDK default per TFM (C# 12 on net8.0): `init`, raw string literals are fine; the `field` keyword is **not**.
- Package tag `cake-addin`; embedded `PackageIcon` (`icon.png`) and `PackageReadmeFile`; XML documentation generated and shipped; every alias has `[CakeMethodAlias]`, `[CakeAliasCategory("Grype")]` and `[CakeNamespaceImport]` for each namespace its signature uses.
- Alias names are tool-first: `GrypeScan`, `GrypeScanSbom`, `GrypeScanDirectory`, `GrypeScanFile`, `GrypeScanImage`, `GrypeScanRegistry`, `GrypeReadJson`, `GrypeDbUpdate`, `GrypeDbStatus`, `GrypeDbCheck`, `GrypeDbImport`, `GrypeDbDelete`, `GrypeVersion`.
- Tool name `Grype` (error messages read `Grype: Process returned an error (exit code N).`); executable names `grype.exe`, `grype`.
- Argument order: command words (e.g. `db status`), global flags (`-c`, `--profile`, `-q`, `-v`/`-vv`), command flags, positional argument last.
- Commands whose output is parsed (`version`, `db status`, `db check`) always pass `-q` (once) and end with `-o json`, redirect stdout, and read stdout **before** invoking the user's `PostAction` — Cake's `ProcessWrapper.GetStandardOutput()` dequeues, so it can only be read once. Passing a post action to `Tool.Run` replaces `settings.PostAction` (`postAction ?? settings.PostAction`), so runners that pass one must invoke `settings.PostAction` themselves.
- Exit codes are not remapped, except: `db check` accepts `100` (update available); `db status` accepts `1` only when stdout parses as status JSON with `valid: false`. Everything else non-zero throws `CakeException` unless the user's `HandleExitCode` accepts it.
- All relative paths passed to Grype (sources, output files, `--file`, `-t`, `-c`, `--vex`, `db import` archive) are made absolute against `settings.WorkingDirectory` (itself made absolute) or the Cake working directory.
- Report model: unmapped fields are ignored; missing or `null` arrays become empty lists; unrecognised severity/fix-state strings map to `Unknown`; `GrypeSeverity` order `Unknown < Negligible < Low < Medium < High < Critical` (identical to Grype's).
- Tests: xUnit v3 + `Cake.Testing` `ToolFixture` (fake Unix environment, working directory `/Working`, tool at `/Working/tools/grype`). `global.json` selects the Microsoft Testing Platform runner.
- Fast test command for one class: `dotnet test --project tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj -f net10.0 --filter-class "<Namespace.Class>"`. Full run: `dotnet test Cake.Grype.sln`.
- Test files have `ImplicitUsings` (so `System.IO`); in a file that also has `using Cake.Core.IO;`, write `System.IO.Path` — `Cake.Core.IO.Path` is public and makes bare `Path` ambiguous (CS0104). `Cake.Core.IO.File`/`Directory` are internal, so `File`/`Directory` are fine.
- Shell: use the Bash tool (Git Bash). `python` works; `python3` is a Windows Store stub — don't use it.
- Commit messages end with the `Co-Authored-By` trailer from the session's attribution instructions.

## File Structure

```
global.json
Cake.Grype.sln
README.md                                  (rewritten in Task 7)
src/Cake.Grype/
  Cake.Grype.csproj
  icon.png                                 Cake Contrib icon (downloaded, Task 1)
  GrypeVerbosity.cs                        Default / Info / Debug
  GrypeSettings.cs                         global flags
  GrypeTool.cs                             base runner: names, global args, JSON-from-stdout, exit-code hook
  GrypeVersion.cs                          `version -o json` result
  GrypeVersionSettings.cs
  GrypeVersionReader.cs
  GrypeSeverity.cs                         (Task 2)
  GrypeSource.cs                           (Task 2)
  GrypeAliases.Version.cs                  GrypeVersion(...)
  GrypeAliases.Scan.cs                     GrypeScan*(...)            (Task 3)
  GrypeAliases.ReadJson.cs                 GrypeReadJson(...)         (Task 4)
  GrypeAliases.Db.cs                       GrypeDb*(...)              (Tasks 5, 6)
  Scan/GrypeOutputFormat.cs, GrypeOutput.cs, GrypeSortBy.cs, GrypeScope.cs, GrypeFixStates.cs,
       GrypeScanSettings.cs, GrypeScanner.cs                           (Task 3)
  Json/GrypeReport.cs, GrypeMatch.cs, GrypeArtifact.cs, GrypeVulnerability.cs,
       GrypeVulnerabilitySignals.cs, GrypeJsonConverters.cs, GrypeReportReader.cs   (Task 4)
  Db/GrypeDbUpdateSettings.cs, GrypeDbUpdater.cs, GrypeDbImportSettings.cs, GrypeDbImporter.cs,
     GrypeDbDeleteSettings.cs, GrypeDbDeleter.cs                       (Task 5)
  Db/GrypeDbStatus.cs, GrypeDbStatusSettings.cs, GrypeDbStatusReader.cs,
     GrypeDbCheckResult.cs, GrypeDbCheckSettings.cs, GrypeDbChecker.cs (Task 6)
tests/Cake.Grype.Tests/
  Cake.Grype.Tests.csproj
  TestData/grype-report.json               ALREADY COMMITTED — trimmed real Grype 0.119.0 output
  Assertions.cs
  Fixtures/GrypeFixture.cs                 base ToolFixture ("grype") + GivenStandardOutput
  Fixtures/AliasContext.cs                 NSubstitute ICakeContext + recording process runner
  Fixtures/VersionFixture.cs, ScanFixture.cs, DbFixtures.cs
  GrypeToolTests.cs, GrypeVersionReaderTests.cs, GrypeSeverityTests.cs, GrypeSourceTests.cs,
  GrypeScannerTests.cs, GrypeScanAliasesTests.cs, GrypeReportReaderTests.cs, GrypeMatchTests.cs,
  GrypeDbCommandTests.cs, GrypeDbStatusReaderTests.cs, GrypeDbCheckerTests.cs, ArchitectureTests.cs
```

### Test data (already in the repository)

`tests/Cake.Grype.Tests/TestData/grype-report.json` is real Grype 0.119.0 output for `etc/sample.cdx.json`, trimmed to four matches and one ignored match (URLs and descriptions shortened; unmapped fields such as `matchDetails`, `locations`, `descriptor.configuration` kept on purpose so the reader proves it ignores them). Facts the tests rely on:

| # | `vulnerability.id` | artifact (`name` `version`) | severity | fix state / versions | risk | EPSS score (percentile) | CVSS base scores (primary \| related) | other |
|---|---|---|---|---|---|---|---|---|
| 0 | CVE-2023-44487 | varnish 7.6.0 | High | wont-fix / – | 78.75 | 0.99999 (0.99999) | 7.5 \| 7.5, 7.5 | KEV entry: `dueDate` `2023-10-31`, `vendorProject` `IETF`, `cwes` `["CWE-400"]` |
| 1 | CVE-2025-15467 | libssl3 3.0.14-1~deb12u2 | Critical | fixed / `3.0.18-1~deb12u2` | 45.31834 | 0.48211 (0.98841) | 9.8 \| 9.8, 8.8 | advisory `DSA-6113-1`; purl `pkg:deb/debian/libssl3@3.0.14-1~deb12u2?arch=amd64&upstream=openssl&distro=debian-12` |
| 2 | CVE-2016-2781 | coreutils 9.1-1 | Low | wont-fix / – | 0.1311 | 0.00437 (0.37346) | **none** \| 6.5, 2.1, 4.6 | CVSS only on the related NVD record |
| 3 | CVE-2026-53613 | bsdextrautils 2.38.1-5+deb12u1 | Unknown | not-fixed / – | 0 | **none** | **none** \| none | reserved CVE: no `description`, no `epss`, no `knownExploited` keys |

Each match has exactly one related vulnerability (same id, namespace `nvd:cpe`). Ignored match: `CVE-2004-0230` on `linux-libc-dev`, one applied rule `{ "namespace": "", "package": { "name": "linux-libc-dev", "language": "", "type": "deb", "upstream-name": "linux" }, "match-type": "exact-indirect-match" }`. `source`: `type` `image`, `target.userInput` `docker.io/library/varnish`. `distro`: `debian` `12`, `idLike` `["debian"]`. `descriptor`: `grype` `0.119.0`, `timestamp` `2026-09-22T20:15:25.2703298+02:00`.

---

### Task 1: Scaffolding, base tool, global settings and the `version` command

**Files:**
- Create: `global.json`, `Cake.Grype.sln`, `src/Cake.Grype/Cake.Grype.csproj`, `src/Cake.Grype/icon.png`
- Create: `src/Cake.Grype/GrypeVerbosity.cs`, `GrypeSettings.cs`, `GrypeTool.cs`, `GrypeVersion.cs`, `GrypeVersionSettings.cs`, `GrypeVersionReader.cs`, `GrypeAliases.Version.cs`
- Create: `tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj`, `Assertions.cs`, `Fixtures/GrypeFixture.cs`, `Fixtures/AliasContext.cs`, `Fixtures/VersionFixture.cs`, `GrypeToolTests.cs`, `GrypeVersionReaderTests.cs`

**Interfaces:**
- Produces:
  - `enum GrypeVerbosity { Default, Info, Debug }`
  - `class GrypeSettings : ToolSettings { ICollection<FilePath> ConfigFiles; ICollection<string> Profiles; bool Quiet; GrypeVerbosity Verbosity; }`
  - `abstract class GrypeTool<TSettings> : Tool<TSettings> where TSettings : GrypeSettings` with protected members:
    - `ProcessArgumentBuilder CreateArgumentBuilder(TSettings settings, params string[] command)`
    - `DirectoryPath ResolveWorkingDirectory(TSettings settings)`
    - `FilePath MakeAbsolute(FilePath path, TSettings settings)`
    - `T RunAndReadJson<T>(TSettings settings, params string[] command) where T : class`
    - `virtual bool AcceptsExitCode(int exitCode, string standardOutput)` (default `false`)
    - `static bool TryParseJson<T>(string json, out T value) where T : class`
  - `GrypeVersion` (result), `GrypeVersionSettings : GrypeSettings`, `GrypeVersionReader.Read(GrypeVersionSettings) : GrypeVersion`
  - Alias `GrypeVersion(this ICakeContext[, GrypeVersionSettings])`
  - Test helpers: `Assertions.IsCakeException(Exception, string)`, `Assertions.IsArgumentNullException(Exception, string)`, `Assertions.IsArgumentException(Exception, string)`; `GrypeFixture<TSettings>` with `GivenStandardOutput(string)`; `AliasContext` (`Context`, `FileSystem`, `Environment`, `ProcessRunner`), `RecordingProcessRunner` (`Process`, `Arguments`).

- [ ] **Step 1: Scaffold the solution and projects**

The branch `feature/cake-grype` already exists and holds the spec, this plan and the test data; work on it.

**File:** `global.json`

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

**File:** `src/Cake.Grype/Cake.Grype.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <AssemblyName>Cake.Grype</AssemblyName>
    <RootNamespace>Cake.Grype</RootNamespace>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <VersionPrefix>0.1.0</VersionPrefix>
  </PropertyGroup>

  <PropertyGroup Label="Package">
    <PackageId>Cake.Grype</PackageId>
    <Authors>Magnus Lindhe</Authors>
    <Description>Cake add-in for Anchore Grype: scan SBOMs, directories and images for vulnerabilities, manage the vulnerability database, and read the JSON report to gate builds on severity, KEV, EPSS, CVSS or risk.</Description>
    <PackageTags>cake;cake-addin;cake-build;grype;anchore;sbom;cyclonedx;vulnerability;security;cve</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/mgnslndh/Cake.Grype</PackageProjectUrl>
    <RepositoryUrl>https://github.com/mgnslndh/Cake.Grype.git</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageIcon>icon.png</PackageIcon>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Cake.Core" Version="6.0.0" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <None Include="icon.png" Pack="true" PackagePath="\" />
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>

</Project>
```

**File:** `tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="4.0.1" />
    <PackageReference Include="Cake.Testing" Version="6.0.0" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Cake.Grype\Cake.Grype.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="TestData\**" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

```bash
dotnet new sln --name Cake.Grype --format sln
dotnet sln add src/Cake.Grype/Cake.Grype.csproj tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj
curl -sSL -o src/Cake.Grype/icon.png https://raw.githubusercontent.com/cake-contrib/graphics/master/png/cake-contrib-medium.png
file src/Cake.Grype/icon.png
```

Expected: the sln lists both projects; `file` reports "PNG image data".

- [ ] **Step 2: Write the test helpers**

**File:** `tests/Cake.Grype.Tests/Assertions.cs`

```csharp
using Cake.Core;

namespace Cake.Grype.Tests;

internal static class Assertions
{
    public static void IsCakeException(Exception exception, string expectedMessage)
    {
        var cakeException = Assert.IsType<CakeException>(exception);
        Assert.Equal(expectedMessage, cakeException.Message);
    }

    public static void IsArgumentNullException(Exception exception, string expectedParameterName)
    {
        var argumentNullException = Assert.IsType<ArgumentNullException>(exception);
        Assert.Equal(expectedParameterName, argumentNullException.ParamName);
    }

    public static void IsArgumentException(Exception exception, string expectedParameterName)
    {
        var argumentException = Assert.IsType<ArgumentException>(exception);
        Assert.Equal(expectedParameterName, argumentException.ParamName);
    }
}
```

**File:** `tests/Cake.Grype.Tests/Fixtures/GrypeFixture.cs`

```csharp
using Cake.Core.Tooling;
using Cake.Testing.Fixtures;

namespace Cake.Grype.Tests.Fixtures;

internal abstract class GrypeFixture<TSettings> : ToolFixture<TSettings>
    where TSettings : ToolSettings, new()
{
    protected GrypeFixture()
        : base("grype")
    {
        ProcessRunner.Process.SetStandardOutput(Array.Empty<string>());
    }

    public void GivenStandardOutput(string output)
    {
        ProcessRunner.Process.SetStandardOutput(output.Replace("\r\n", "\n").Split('\n'));
    }
}
```

**File:** `tests/Cake.Grype.Tests/Fixtures/AliasContext.cs`

```csharp
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Tooling;
using Cake.Testing;
using NSubstitute;

namespace Cake.Grype.Tests.Fixtures;

/// <summary>
/// A substitute <see cref="ICakeContext"/> backed by Cake's fakes, for calling aliases directly.
/// </summary>
internal sealed class AliasContext
{
    public AliasContext()
    {
        Environment = FakeEnvironment.CreateUnixEnvironment();
        FileSystem = new FakeFileSystem(Environment);
        FileSystem.CreateFile("/Working/tools/grype");
        ProcessRunner = new RecordingProcessRunner();

        var globber = new Globber(FileSystem, Environment);
        var tools = new ToolLocator(
            Environment,
            new ToolRepository(Environment),
            new ToolResolutionStrategy(FileSystem, Environment, globber, new FakeConfiguration(), new NullLog()));

        Context = Substitute.For<ICakeContext>();
        Context.FileSystem.Returns(FileSystem);
        Context.Environment.Returns(Environment);
        Context.ProcessRunner.Returns(ProcessRunner);
        Context.Tools.Returns(tools);
    }

    public ICakeContext Context { get; }

    public FakeEnvironment Environment { get; }

    public FakeFileSystem FileSystem { get; }

    public RecordingProcessRunner ProcessRunner { get; }
}

internal sealed class RecordingProcessRunner : IProcessRunner
{
    public FakeProcess Process { get; } = new FakeProcess();

    public List<string> Arguments { get; } = new List<string>();

    public IProcess Start(FilePath filePath, ProcessSettings settings)
    {
        Arguments.Add(settings.Arguments.Render());
        return Process;
    }
}
```

**File:** `tests/Cake.Grype.Tests/Fixtures/VersionFixture.cs`

```csharp
namespace Cake.Grype.Tests.Fixtures;

internal sealed class VersionFixture : GrypeFixture<GrypeVersionSettings>
{
    public VersionFixture()
    {
        GivenStandardOutput("{}");
    }

    public GrypeVersion Result { get; private set; }

    protected override void RunTool()
    {
        Result = new GrypeVersionReader(FileSystem, Environment, ProcessRunner, Tools).Read(Settings);
    }
}
```

- [ ] **Step 3: Write the failing tests**

`GrypeToolTests` exercises the base class (global flags, tool resolution, JSON-from-stdout) through `GrypeVersionReader`, the simplest concrete runner.

**File:** `tests/Cake.Grype.Tests/GrypeToolTests.cs`

```csharp
using Cake.Core.IO;
using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeToolTests
{
    private static string Args(Action<GrypeVersionSettings> configure)
    {
        var fixture = new VersionFixture();
        configure(fixture.Settings);
        return fixture.Run().Args;
    }

    [Fact]
    public void Should_Find_The_Grype_Executable()
    {
        var result = new VersionFixture().Run();

        Assert.Equal("/Working/tools/grype", result.Path.FullPath);
    }

    [Fact]
    public void Should_Throw_If_Grype_Cannot_Be_Found()
    {
        var fixture = new VersionFixture();
        fixture.GivenDefaultToolDoNotExist();

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Could not locate executable.");
    }

    [Fact]
    public void Should_Use_The_Tool_Path_From_Settings()
    {
        var fixture = new VersionFixture();
        fixture.Settings.ToolPath = "/opt/grype/grype";
        fixture.GivenSettingsToolPathExist();

        var result = fixture.Run();

        Assert.Equal("/opt/grype/grype", result.Path.FullPath);
    }

    [Fact]
    public void Should_Add_Config_Files_As_Absolute_Paths()
    {
        var args = Args(s =>
        {
            s.ConfigFiles.Add("grype.yaml");
            s.ConfigFiles.Add("/etc/grype/base.yaml");
        });

        Assert.Equal("version -c \"/Working/grype.yaml\" -c \"/etc/grype/base.yaml\" -q -o json", args);
    }

    [Fact]
    public void Should_Resolve_Config_Files_Against_The_Settings_Working_Directory()
    {
        var args = Args(s =>
        {
            s.WorkingDirectory = "/Other";
            s.ConfigFiles.Add("grype.yaml");
        });

        Assert.Equal("version -c \"/Other/grype.yaml\" -q -o json", args);
    }

    [Fact]
    public void Should_Resolve_A_Relative_Settings_Working_Directory_Against_The_Cake_Working_Directory()
    {
        var args = Args(s =>
        {
            s.WorkingDirectory = "sub";
            s.ConfigFiles.Add("grype.yaml");
        });

        Assert.Equal("version -c \"/Working/sub/grype.yaml\" -q -o json", args);
    }

    [Fact]
    public void Should_Add_Profiles_And_Skip_Blank_Ones()
    {
        var args = Args(s =>
        {
            s.Profiles.Add("ci");
            s.Profiles.Add(" ");
            s.Profiles.Add("strict");
        });

        Assert.Equal("version --profile \"ci\" --profile \"strict\" -q -o json", args);
    }

    [Fact]
    public void Should_Not_Duplicate_Quiet_For_Json_Commands()
    {
        Assert.Equal("version -q -o json", Args(s => s.Quiet = true));
    }

    [Theory]
    [InlineData(GrypeVerbosity.Default, "version -q -o json")]
    [InlineData(GrypeVerbosity.Info, "version -q -v -o json")]
    [InlineData(GrypeVerbosity.Debug, "version -q -vv -o json")]
    public void Should_Add_Verbosity(GrypeVerbosity verbosity, string expected)
    {
        Assert.Equal(expected, Args(s => s.Verbosity = verbosity));
    }

    [Fact]
    public void Should_Tolerate_Null_Collections()
    {
        var args = Args(s =>
        {
            s.ConfigFiles = null;
            s.Profiles = null;
        });

        Assert.Equal("version -q -o json", args);
    }
}
```

**File:** `tests/Cake.Grype.Tests/GrypeVersionReaderTests.cs`

```csharp
using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeVersionReaderTests
{
    // Captured from `grype version -o json` (Grype 0.119.0 on Windows).
    private const string VersionJson = """
        {
         "application": "grype",
         "buildDate": "2026-09-17T16:17:07Z",
         "compiler": "gc",
         "gitCommit": "b6f5194537747ee7f705f4113069ac9eb269919f",
         "gitDescription": "v0.119.0",
         "goVersion": "go1.26.3",
         "platform": "windows/amd64",
         "supportedDbSchema": 6,
         "syftVersion": "v1.52.0",
         "version": "0.119.0"
        }
        """;

    [Fact]
    public void Should_Throw_If_Settings_Are_Null()
    {
        var fixture = new VersionFixture { Settings = null };

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsArgumentNullException(result, "settings");
    }

    [Fact]
    public void Should_Request_Quiet_Json_Output()
    {
        Assert.Equal("version -q -o json", new VersionFixture().Run().Args);
    }

    [Fact]
    public void Should_Redirect_Standard_Output()
    {
        var result = new VersionFixture().Run();

        Assert.True(result.Process.RedirectStandardOutput);
    }

    [Fact]
    public void Should_Parse_The_Version()
    {
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput(VersionJson);

        fixture.Run();

        Assert.Equal("grype", fixture.Result.Application);
        Assert.Equal("0.119.0", fixture.Result.Version);
        Assert.Equal("2026-09-17T16:17:07Z", fixture.Result.BuildDate);
        Assert.Equal("b6f5194537747ee7f705f4113069ac9eb269919f", fixture.Result.GitCommit);
        Assert.Equal("v0.119.0", fixture.Result.GitDescription);
        Assert.Equal("windows/amd64", fixture.Result.Platform);
        Assert.Equal("go1.26.3", fixture.Result.GoVersion);
        Assert.Equal("gc", fixture.Result.Compiler);
        Assert.Equal("v1.52.0", fixture.Result.SyftVersion);
        Assert.Equal(6, fixture.Result.SupportedDbSchema);
    }

    [Fact]
    public void Should_Throw_If_The_Output_Is_Not_Json()
    {
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput("Application: grype");

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: The output is not valid JSON: Application: grype");
    }

    [Fact]
    public void Should_Throw_If_There_Is_No_Output()
    {
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput(string.Empty);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: The output is not valid JSON: (no output)");
    }

    [Fact]
    public void Should_Truncate_Long_Invalid_Output_In_The_Error()
    {
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput(new string('x', 600));

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: The output is not valid JSON: " + new string('x', 500) + "...");
    }

    [Fact]
    public void Should_Throw_On_A_Non_Zero_Exit_Code_Before_Parsing()
    {
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput(string.Empty);
        fixture.GivenProcessExitsWithCode(1);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 1).");
    }

    [Fact]
    public void Should_Invoke_The_Users_Post_Action_Once_After_Reading_The_Output()
    {
        var invocations = 0;
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput(VersionJson);
        fixture.Settings.PostAction = _ => invocations++;

        fixture.Run();

        Assert.Equal(1, invocations);
        Assert.Equal("0.119.0", fixture.Result.Version);
    }

    [Fact]
    public void Alias_Should_Run_Version()
    {
        var alias = new AliasContext();
        alias.ProcessRunner.Process.SetStandardOutput(VersionJson.Split('\n'));

        var version = alias.Context.GrypeVersion();

        Assert.Equal("0.119.0", version.Version);
        Assert.Equal(new[] { "version -q -o json" }, alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Alias_Should_Throw_If_Context_Is_Null()
    {
        var result = Record.Exception(() => GrypeAliases.GrypeVersion(null));

        Assertions.IsArgumentNullException(result, "context");
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test Cake.Grype.sln`
Expected: build FAILS — `GrypeVersionSettings`, `GrypeVersionReader`, `GrypeVersion`, `GrypeVerbosity`, `GrypeAliases` do not exist.

- [ ] **Step 5: Implement global settings and the base tool**

**File:** `src/Cake.Grype/GrypeVerbosity.cs`

```csharp
namespace Cake.Grype
{
    /// <summary>
    /// Grype's log verbosity (<c>-v</c>, <c>-vv</c>). Logs are written to standard error.
    /// </summary>
    public enum GrypeVerbosity
    {
        /// <summary>Grype's default verbosity; no flag is passed.</summary>
        Default,

        /// <summary>Informational logging (<c>-v</c>).</summary>
        Info,

        /// <summary>Debug logging (<c>-vv</c>).</summary>
        Debug,
    }
}
```

**File:** `src/Cake.Grype/GrypeSettings.cs`

```csharp
using System.Collections.Generic;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype
{
    /// <summary>
    /// Contains the settings shared by all Grype commands (Grype's global flags).
    /// </summary>
    /// <remarks>
    /// Grype settings that have no command-line flag, such as <c>GRYPE_DB_AUTO_UPDATE</c>,
    /// <c>GRYPE_DB_CACHE_DIR</c> or <c>GRYPE_CHECK_FOR_APP_UPDATE</c>, can be set through
    /// <see cref="ToolSettings.EnvironmentVariables"/>.
    /// </remarks>
    public class GrypeSettings : ToolSettings
    {
        /// <summary>
        /// Gets or sets the Grype configuration files to use (<c>-c</c>, repeatable).
        /// Relative paths are resolved against the working directory.
        /// </summary>
        public ICollection<FilePath> ConfigFiles { get; set; } = new List<FilePath>();

        /// <summary>
        /// Gets or sets the configuration profiles to use (<c>--profile</c>, repeatable).
        /// </summary>
        public ICollection<string> Profiles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether all logging output is suppressed (<c>-q</c>).
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Gets or sets the log verbosity (<c>-v</c> or <c>-vv</c>).
        /// </summary>
        public GrypeVerbosity Verbosity { get; set; }
    }
}
```

**File:** `src/Cake.Grype/GrypeTool.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype
{
    /// <summary>
    /// Base class for the Grype commands.
    /// </summary>
    /// <typeparam name="TSettings">The settings type.</typeparam>
    public abstract class GrypeTool<TSettings> : Tool<TSettings>
        where TSettings : GrypeSettings
    {
        private const int OutputExcerptLength = 500;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly ICakeEnvironment _environment;
        private string _standardOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeTool{TSettings}" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        protected GrypeTool(
            IFileSystem fileSystem,
            ICakeEnvironment environment,
            IProcessRunner processRunner,
            IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
            _environment = environment;
        }

        /// <summary>
        /// Gets the name of the tool.
        /// </summary>
        /// <returns>The name of the tool.</returns>
        protected override string GetToolName()
        {
            return "Grype";
        }

        /// <summary>
        /// Gets the possible names of the tool executable.
        /// </summary>
        /// <returns>The tool executable names.</returns>
        protected override IEnumerable<string> GetToolExecutableNames()
        {
            return new[] { "grype.exe", "grype" };
        }

        /// <summary>
        /// Creates a <see cref="ProcessArgumentBuilder"/> containing the command words followed by the global flags.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="command">The command words, for example <c>db</c>, <c>update</c>; none for a scan.</param>
        /// <returns>The argument builder.</returns>
        protected ProcessArgumentBuilder CreateArgumentBuilder(TSettings settings, params string[] command)
        {
            return CreateArgumentBuilder(settings, false, command);
        }

        /// <summary>
        /// Gets the absolute directory that relative paths are resolved against: the settings' working directory
        /// if set, otherwise the Cake working directory.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The absolute working directory.</returns>
        protected DirectoryPath ResolveWorkingDirectory(TSettings settings)
        {
            return GetWorkingDirectory(settings).MakeAbsolute(_environment);
        }

        /// <summary>
        /// Makes a path absolute against <see cref="ResolveWorkingDirectory"/>.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The absolute path.</returns>
        protected FilePath MakeAbsolute(FilePath path, TSettings settings)
        {
            return path.MakeAbsolute(ResolveWorkingDirectory(settings));
        }

        /// <summary>
        /// Runs a command with <c>-q</c> and <c>-o json</c>, and parses its standard output.
        /// </summary>
        /// <remarks>
        /// Standard output is read before <see cref="ToolSettings.PostAction"/> runs, because redirected output can only
        /// be read once. The exit code is checked (see <see cref="AcceptsExitCode"/>) before the output is parsed.
        /// </remarks>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="settings">The settings.</param>
        /// <param name="command">The command words.</param>
        /// <returns>The parsed result.</returns>
        protected T RunAndReadJson<T>(TSettings settings, params string[] command)
            where T : class
        {
            var arguments = CreateArgumentBuilder(settings, true, command);
            arguments.Append("-o");
            arguments.Append("json");

            _standardOutput = null;
            Run(settings, arguments, new ProcessSettings { RedirectStandardOutput = true }, process =>
            {
                _standardOutput = string.Join("\n", process.GetStandardOutput() ?? Enumerable.Empty<string>());
                settings.PostAction?.Invoke(process);
            });

            if (TryParseJson(_standardOutput, out T result))
            {
                return result;
            }

            throw new CakeException($"{GetToolName()}: The output is not valid JSON: {Excerpt(_standardOutput)}");
        }

        /// <summary>
        /// Determines whether a non-zero exit code is expected for this command and must not throw.
        /// </summary>
        /// <param name="exitCode">The non-zero exit code.</param>
        /// <param name="standardOutput">The captured standard output, or <c>null</c> if it was not redirected.</param>
        /// <returns><c>true</c> to accept the exit code.</returns>
        protected virtual bool AcceptsExitCode(int exitCode, string standardOutput)
        {
            return false;
        }

        /// <inheritdoc />
        protected sealed override void ProcessExitCode(int exitCode)
        {
            if (exitCode != 0 && AcceptsExitCode(exitCode, _standardOutput))
            {
                return;
            }

            base.ProcessExitCode(exitCode);
        }

        /// <summary>
        /// Parses JSON without throwing.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="json">The JSON text.</param>
        /// <param name="value">The parsed value, or <c>null</c>.</param>
        /// <returns><c>true</c> if the text is a JSON value of the requested shape.</returns>
        protected static bool TryParseJson<T>(string json, out T value)
            where T : class
        {
            value = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return value != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private ProcessArgumentBuilder CreateArgumentBuilder(TSettings settings, bool quiet, string[] command)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var builder = new ProcessArgumentBuilder();
            foreach (var word in command)
            {
                builder.Append(word);
            }

            foreach (var configFile in settings.ConfigFiles ?? Enumerable.Empty<FilePath>())
            {
                if (configFile == null)
                {
                    continue;
                }

                builder.Append("-c");
                builder.AppendQuoted(MakeAbsolute(configFile, settings).FullPath);
            }

            foreach (var profile in settings.Profiles ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(profile))
                {
                    continue;
                }

                builder.Append("--profile");
                builder.AppendQuoted(profile);
            }

            if (settings.Quiet || quiet)
            {
                builder.Append("-q");
            }

            switch (settings.Verbosity)
            {
                case GrypeVerbosity.Info:
                    builder.Append("-v");
                    break;
                case GrypeVerbosity.Debug:
                    builder.Append("-vv");
                    break;
            }

            return builder;
        }

        private static string Excerpt(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return "(no output)";
            }

            return output.Length <= OutputExcerptLength ? output : output.Substring(0, OutputExcerptLength) + "...";
        }
    }
}
```

- [ ] **Step 6: Implement the version command and alias**

**File:** `src/Cake.Grype/GrypeVersion.cs`

```csharp
namespace Cake.Grype
{
    /// <summary>
    /// Version information reported by <c>grype version -o json</c>.
    /// </summary>
    public sealed class GrypeVersion
    {
        /// <summary>Gets the application name (<c>grype</c>).</summary>
        public string Application { get; init; }

        /// <summary>Gets the Grype version, for example <c>0.119.0</c>.</summary>
        public string Version { get; init; }

        /// <summary>Gets the build date as reported by Grype (RFC 3339 for release builds).</summary>
        public string BuildDate { get; init; }

        /// <summary>Gets the git commit Grype was built from.</summary>
        public string GitCommit { get; init; }

        /// <summary>Gets the git description, for example <c>v0.119.0</c>.</summary>
        public string GitDescription { get; init; }

        /// <summary>Gets the platform, for example <c>windows/amd64</c>.</summary>
        public string Platform { get; init; }

        /// <summary>Gets the Go version Grype was built with.</summary>
        public string GoVersion { get; init; }

        /// <summary>Gets the Go compiler.</summary>
        public string Compiler { get; init; }

        /// <summary>Gets the version of the embedded Syft library.</summary>
        public string SyftVersion { get; init; }

        /// <summary>Gets the vulnerability database schema version this Grype supports.</summary>
        public int? SupportedDbSchema { get; init; }
    }
}
```

**File:** `src/Cake.Grype/GrypeVersionSettings.cs`

```csharp
namespace Cake.Grype
{
    /// <summary>
    /// Contains the settings for <c>grype version</c>.
    /// </summary>
    public sealed class GrypeVersionSettings : GrypeSettings
    {
    }
}
```

**File:** `src/Cake.Grype/GrypeVersionReader.cs`

```csharp
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype
{
    /// <summary>
    /// Runs <c>grype version</c> and returns the parsed version information.
    /// </summary>
    public sealed class GrypeVersionReader : GrypeTool<GrypeVersionSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeVersionReader" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeVersionReader(
            IFileSystem fileSystem,
            ICakeEnvironment environment,
            IProcessRunner processRunner,
            IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Reads Grype's version information.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The version information.</returns>
        public GrypeVersion Read(GrypeVersionSettings settings)
        {
            return RunAndReadJson<GrypeVersion>(settings, "version");
        }
    }
}
```

**File:** `src/Cake.Grype/GrypeAliases.Version.cs`

```csharp
using System;
using Cake.Core;
using Cake.Core.Annotations;

namespace Cake.Grype
{
    /// <summary>
    /// Contains functionality for running Anchore Grype, a vulnerability scanner for SBOMs, container images and
    /// file systems. Grype must be installed (for example <c>winget install Anchore.Grype</c>) or
    /// <see cref="Cake.Core.Tooling.ToolSettings.ToolPath"/> must be set.
    /// </summary>
    [CakeAliasCategory("Grype")]
    public static partial class GrypeAliases
    {
        /// <summary>
        /// Gets Grype's version information.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The version information.</returns>
        /// <example>
        /// <code>
        /// var version = GrypeVersion();
        /// Information("Grype {0} (DB schema {1})", version.Version, version.SupportedDbSchema);
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        public static GrypeVersion GrypeVersion(this ICakeContext context)
        {
            return context.GrypeVersion(null);
        }

        /// <summary>
        /// Gets Grype's version information using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The version information.</returns>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        public static GrypeVersion GrypeVersion(this ICakeContext context, GrypeVersionSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeVersionSettings();
            return new GrypeVersionReader(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools)
                .Read(settings);
        }
    }
}
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test Cake.Grype.sln`
Expected: PASS on `net8.0`, `net9.0`, `net10.0`; build has no warnings about missing XML comments.

- [ ] **Step 8: Commit**

```bash
git add global.json Cake.Grype.sln src tests
git commit -m "feat: scaffold Cake.Grype with base tool, global settings and GrypeVersion"
```

---

### Task 2: `GrypeSeverity` and `GrypeSource`

**Files:**
- Create: `src/Cake.Grype/GrypeSeverity.cs`, `src/Cake.Grype/GrypeSource.cs`
- Test: `tests/Cake.Grype.Tests/GrypeSeverityTests.cs`, `tests/Cake.Grype.Tests/GrypeSourceTests.cs`

**Interfaces:**
- Produces:
  - `enum GrypeSeverity { Unknown = 0, Negligible = 1, Low = 2, Medium = 3, High = 4, Critical = 5 }`
  - `sealed class GrypeSource` with static factories `Sbom(FilePath)`, `Directory(DirectoryPath)`, `File(FilePath)`, `Image(string)`, `Docker(string)`, `Podman(string)`, `Registry(string)`, `DockerArchive(FilePath)`, `OciArchive(FilePath)`, `OciDirectory(DirectoryPath)`, `Singularity(FilePath)`, `PurlFile(FilePath)`, `Purl(string)`, `CpeFile(FilePath)`, `Cpe(string)`, `Zarf(FilePath)`, `Parse(string)`; `implicit operator GrypeSource(string)`; `string ToArgument(DirectoryPath workingDirectory)`; `ToString()`.

- [ ] **Step 1: Write the failing tests**

**File:** `tests/Cake.Grype.Tests/GrypeSeverityTests.cs`

```csharp
namespace Cake.Grype.Tests;

public sealed class GrypeSeverityTests
{
    [Fact]
    public void Should_Be_Ordered_Like_Grype()
    {
        // grype/vulnerability/severity.go: UnknownSeverity = iota, Negligible, Low, Medium, High, Critical
        Assert.Equal(0, (int)GrypeSeverity.Unknown);
        Assert.Equal(1, (int)GrypeSeverity.Negligible);
        Assert.Equal(2, (int)GrypeSeverity.Low);
        Assert.Equal(3, (int)GrypeSeverity.Medium);
        Assert.Equal(4, (int)GrypeSeverity.High);
        Assert.Equal(5, (int)GrypeSeverity.Critical);
    }

    [Fact]
    public void Should_Rank_Unknown_Below_Every_Assessed_Severity()
    {
        Assert.True(GrypeSeverity.Unknown < GrypeSeverity.Negligible);
        Assert.True(GrypeSeverity.Critical >= GrypeSeverity.High);
    }
}
```

**File:** `tests/Cake.Grype.Tests/GrypeSourceTests.cs`

```csharp
using Cake.Core.IO;

namespace Cake.Grype.Tests;

public sealed class GrypeSourceTests
{
    private static readonly DirectoryPath Working = new DirectoryPath("/Working");

    public static TheoryData<GrypeSource, string> RelativePathSources => new TheoryData<GrypeSource, string>
    {
        { GrypeSource.Sbom("artifacts/bom.cdx.json"), "sbom:/Working/artifacts/bom.cdx.json" },
        { GrypeSource.Directory("src"), "dir:/Working/src" },
        { GrypeSource.File("bin/app.jar"), "file:/Working/bin/app.jar" },
        { GrypeSource.DockerArchive("image.tar"), "docker-archive:/Working/image.tar" },
        { GrypeSource.OciArchive("image.oci.tar"), "oci-archive:/Working/image.oci.tar" },
        { GrypeSource.OciDirectory("oci"), "oci-dir:/Working/oci" },
        { GrypeSource.Singularity("image.sif"), "singularity:/Working/image.sif" },
        { GrypeSource.PurlFile("purls.txt"), "purl:/Working/purls.txt" },
        { GrypeSource.CpeFile("cpes.txt"), "cpes:/Working/cpes.txt" },
        { GrypeSource.Zarf("package.tar.zst"), "zarf:/Working/package.tar.zst" },
    };

    public static TheoryData<GrypeSource, string> ValueSources => new TheoryData<GrypeSource, string>
    {
        { GrypeSource.Image("alpine:3.20"), "alpine:3.20" },
        { GrypeSource.Docker("myorg/api:1.2.3"), "docker:myorg/api:1.2.3" },
        { GrypeSource.Podman("myorg/api:1.2.3"), "podman:myorg/api:1.2.3" },
        { GrypeSource.Registry("myorg/api:1.2.3"), "registry:myorg/api:1.2.3" },
        { GrypeSource.Purl("pkg:apk/openssl@3.2.1?distro=alpine-3.20.3"), "pkg:apk/openssl@3.2.1?distro=alpine-3.20.3" },
        { GrypeSource.Cpe("cpe:2.3:a:openssl:openssl:3.0.14:*:*:*:*:*:*:*"), "cpe:2.3:a:openssl:openssl:3.0.14:*:*:*:*:*:*:*" },
        { GrypeSource.Parse("registry:alpine:3.20"), "registry:alpine:3.20" },
    };

    [Theory]
    [MemberData(nameof(RelativePathSources))]
    public void Should_Make_Paths_Absolute(GrypeSource source, string expected)
    {
        Assert.Equal(expected, source.ToArgument(Working));
    }

    [Theory]
    [MemberData(nameof(ValueSources))]
    public void Should_Pass_Values_Through(GrypeSource source, string expected)
    {
        Assert.Equal(expected, source.ToArgument(Working));
    }

    [Fact]
    public void Should_Keep_Absolute_Paths()
    {
        Assert.Equal("sbom:/data/bom.json", GrypeSource.Sbom("/data/bom.json").ToArgument(Working));
    }

    [Fact]
    public void Should_Resolve_Against_The_Given_Directory()
    {
        Assert.Equal("dir:/Other/src", GrypeSource.Directory("src").ToArgument(new DirectoryPath("/Other")));
    }

    [Fact]
    public void Should_Convert_Implicitly_From_String()
    {
        GrypeSource source = "sbom:./bom.json";

        Assert.Equal("sbom:./bom.json", source.ToArgument(Working));
    }

    [Fact]
    public void Should_Render_The_Unresolved_Form_In_ToString()
    {
        Assert.Equal("sbom:artifacts/bom.cdx.json", GrypeSource.Sbom("artifacts/bom.cdx.json").ToString());
        Assert.Equal("registry:alpine:3.20", GrypeSource.Registry("alpine:3.20").ToString());
    }

    [Fact]
    public void Should_Throw_If_A_Path_Is_Null()
    {
        var result = Record.Exception(() => GrypeSource.Sbom(null));

        Assertions.IsArgumentNullException(result, "file");
    }

    [Fact]
    public void Should_Throw_If_A_Directory_Is_Null()
    {
        var result = Record.Exception(() => GrypeSource.Directory(null));

        Assertions.IsArgumentNullException(result, "directory");
    }

    [Fact]
    public void Should_Throw_If_A_Value_Is_Null()
    {
        var result = Record.Exception(() => GrypeSource.Registry(null));

        Assertions.IsArgumentNullException(result, "reference");
    }

    [Fact]
    public void Should_Throw_If_A_Value_Is_Blank()
    {
        var result = Record.Exception(() => GrypeSource.Image(" "));

        Assertions.IsArgumentException(result, "reference");
    }

    [Fact]
    public void Should_Throw_If_Parsed_Text_Is_Null()
    {
        var result = Record.Exception(() => GrypeSource.Parse(null));

        Assertions.IsArgumentNullException(result, "source");
    }

    [Fact]
    public void Should_Throw_If_The_Working_Directory_Is_Null()
    {
        var result = Record.Exception(() => GrypeSource.Sbom("bom.json").ToArgument(null));

        Assertions.IsArgumentNullException(result, "workingDirectory");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test --project tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj -f net10.0 --filter-class "Cake.Grype.Tests.GrypeSourceTests"`
Expected: build FAILS — `GrypeSeverity` and `GrypeSource` do not exist.

- [ ] **Step 3: Implement**

**File:** `src/Cake.Grype/GrypeSeverity.cs`

```csharp
namespace Cake.Grype
{
    /// <summary>
    /// A vulnerability severity as Grype reports it. Values are ordered, so <c>severity &gt;= GrypeSeverity.High</c> works.
    /// </summary>
    /// <remarks>
    /// <see cref="Unknown"/> means "not assessed yet" (for example a reserved CVE without analysis), not "low".
    /// It ranks below every assessed severity, so severity thresholds, including Grype's own <c>--fail-on</c>,
    /// never match it. Check for it explicitly if unassessed findings should block a build.
    /// </remarks>
    public enum GrypeSeverity
    {
        /// <summary>No severity has been assessed.</summary>
        Unknown = 0,

        /// <summary>Negligible.</summary>
        Negligible = 1,

        /// <summary>Low.</summary>
        Low = 2,

        /// <summary>Medium.</summary>
        Medium = 3,

        /// <summary>High.</summary>
        High = 4,

        /// <summary>Critical.</summary>
        Critical = 5,
    }
}
```

**File:** `src/Cake.Grype/GrypeSource.cs`

```csharp
using System;
using Cake.Core.IO;

namespace Cake.Grype
{
    /// <summary>
    /// What Grype scans: an SBOM, a directory, a file, a container image, package URLs or CPEs.
    /// Relative paths are made absolute when the scan runs. A <see cref="string"/> converts implicitly and is
    /// passed to Grype unchanged, for example <c>"registry:alpine:3.20"</c>.
    /// </summary>
    public sealed class GrypeSource
    {
        private readonly string _scheme;
        private readonly string _value;
        private readonly FilePath _file;
        private readonly DirectoryPath _directory;

        private GrypeSource(string scheme, string value, FilePath file, DirectoryPath directory)
        {
            _scheme = scheme;
            _value = value;
            _file = file;
            _directory = directory;
        }

        /// <summary>An SBOM file: Syft JSON, CycloneDX or SPDX (<c>sbom:</c>).</summary>
        /// <param name="file">The SBOM file.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Sbom(FilePath file) => FromFile("sbom", file);

        /// <summary>A directory on disk (<c>dir:</c>).</summary>
        /// <param name="directory">The directory.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Directory(DirectoryPath directory) => FromDirectory("dir", directory);

        /// <summary>A single file on disk (<c>file:</c>).</summary>
        /// <param name="file">The file.</param>
        /// <returns>The source.</returns>
        public static GrypeSource File(FilePath file) => FromFile("file", file);

        /// <summary>A container image reference, resolved by Grype's default lookup (a Docker daemon first).</summary>
        /// <param name="reference">The image reference, for example <c>myorg/api:1.2.3</c>.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Image(string reference) => FromValue(null, reference);

        /// <summary>An image from the Docker daemon (<c>docker:</c>).</summary>
        /// <param name="reference">The image reference.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Docker(string reference) => FromValue("docker", reference);

        /// <summary>An image from the Podman daemon (<c>podman:</c>).</summary>
        /// <param name="reference">The image reference.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Podman(string reference) => FromValue("podman", reference);

        /// <summary>An image pulled directly from a registry, no container runtime required (<c>registry:</c>).</summary>
        /// <param name="reference">The image reference.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Registry(string reference) => FromValue("registry", reference);

        /// <summary>A tarball created by <c>docker save</c> (<c>docker-archive:</c>).</summary>
        /// <param name="file">The archive.</param>
        /// <returns>The source.</returns>
        public static GrypeSource DockerArchive(FilePath file) => FromFile("docker-archive", file);

        /// <summary>An OCI archive (<c>oci-archive:</c>).</summary>
        /// <param name="file">The archive.</param>
        /// <returns>The source.</returns>
        public static GrypeSource OciArchive(FilePath file) => FromFile("oci-archive", file);

        /// <summary>An OCI layout directory (<c>oci-dir:</c>).</summary>
        /// <param name="directory">The directory.</param>
        /// <returns>The source.</returns>
        public static GrypeSource OciDirectory(DirectoryPath directory) => FromDirectory("oci-dir", directory);

        /// <summary>A Singularity Image Format container (<c>singularity:</c>).</summary>
        /// <param name="file">The SIF file.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Singularity(FilePath file) => FromFile("singularity", file);

        /// <summary>A file with one package URL per line (<c>purl:</c>).</summary>
        /// <param name="file">The file.</param>
        /// <returns>The source.</returns>
        public static GrypeSource PurlFile(FilePath file) => FromFile("purl", file);

        /// <summary>A single package URL, for example <c>pkg:apk/openssl@3.2.1?distro=alpine-3.20.3</c>.</summary>
        /// <param name="reference">The package URL.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Purl(string reference) => FromValue(null, reference);

        /// <summary>A file with one CPE per line (<c>cpes:</c>).</summary>
        /// <param name="file">The file.</param>
        /// <returns>The source.</returns>
        public static GrypeSource CpeFile(FilePath file) => FromFile("cpes", file);

        /// <summary>A single CPE, for example <c>cpe:2.3:a:openssl:openssl:3.0.14:*:*:*:*:*:*:*</c>.</summary>
        /// <param name="reference">The CPE.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Cpe(string reference) => FromValue(null, reference);

        /// <summary>All SBOMs within a Zarf package archive (<c>zarf:</c>).</summary>
        /// <param name="file">The package archive.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Zarf(FilePath file) => FromFile("zarf", file);

        /// <summary>
        /// A source exactly as Grype expects it on the command line; relative paths are not resolved.
        /// </summary>
        /// <param name="source">The source text, for example <c>registry:alpine:3.20</c>.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Parse(string source)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentException.ThrowIfNullOrWhiteSpace(source);
            return new GrypeSource(null, source, null, null);
        }

        /// <summary>
        /// Converts source text to a <see cref="GrypeSource"/> (see <see cref="Parse"/>).
        /// </summary>
        /// <param name="source">The source text.</param>
        public static implicit operator GrypeSource(string source) => Parse(source);

        /// <summary>
        /// Renders the command-line argument, making relative paths absolute.
        /// </summary>
        /// <param name="workingDirectory">The absolute directory relative paths are resolved against.</param>
        /// <returns>The argument.</returns>
        public string ToArgument(DirectoryPath workingDirectory)
        {
            ArgumentNullException.ThrowIfNull(workingDirectory);

            var value = _file != null
                ? _file.MakeAbsolute(workingDirectory).FullPath
                : _directory != null
                    ? _directory.MakeAbsolute(workingDirectory).FullPath
                    : _value;

            return Render(value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Render(_file?.FullPath ?? _directory?.FullPath ?? _value);
        }

        private static GrypeSource FromFile(string scheme, FilePath file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return new GrypeSource(scheme, null, file, null);
        }

        private static GrypeSource FromDirectory(string scheme, DirectoryPath directory)
        {
            ArgumentNullException.ThrowIfNull(directory);
            return new GrypeSource(scheme, null, null, directory);
        }

        private static GrypeSource FromValue(string scheme, string reference)
        {
            ArgumentNullException.ThrowIfNull(reference);
            ArgumentException.ThrowIfNullOrWhiteSpace(reference);
            return new GrypeSource(scheme, reference, null, null);
        }

        private string Render(string value)
        {
            return _scheme == null ? value : _scheme + ":" + value;
        }
    }
}
```

Note: `ArgumentNullException.ThrowIfNull(x)` and `ArgumentException.ThrowIfNullOrWhiteSpace(x)` take the parameter name from `[CallerArgumentExpression]`, so the names asserted by the tests (`file`, `directory`, `reference`, `source`, `workingDirectory`) are the parameter names above. In `FromFile`/`FromDirectory`/`FromValue` the parameter is `file`/`directory`/`reference`, which is also what the public factories are called with.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test --project tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj -f net10.0 --filter-class "Cake.Grype.Tests.GrypeSourceTests"` and again with `--filter-class "Cake.Grype.Tests.GrypeSeverityTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add GrypeSeverity and typed GrypeSource"
```

---

### Task 3: Scan command (`GrypeScanner`, settings, outputs) and scan aliases

**Files:**
- Create: `src/Cake.Grype/Scan/GrypeOutputFormat.cs`, `GrypeOutput.cs`, `GrypeSortBy.cs`, `GrypeScope.cs`, `GrypeFixStates.cs`, `GrypeScanSettings.cs`, `GrypeScanner.cs`
- Create: `src/Cake.Grype/GrypeAliases.Scan.cs`
- Create: `tests/Cake.Grype.Tests/Fixtures/ScanFixture.cs`
- Test: `tests/Cake.Grype.Tests/GrypeScannerTests.cs`, `tests/Cake.Grype.Tests/GrypeScanAliasesTests.cs`

**Interfaces:**
- Consumes: `GrypeTool<TSettings>` (`CreateArgumentBuilder`, `ResolveWorkingDirectory`, `MakeAbsolute`), `GrypeSettings`, `GrypeSeverity`, `GrypeSource.ToArgument(DirectoryPath)`.
- Produces (namespace `Cake.Grype.Scan`):
  - `enum GrypeOutputFormat { Table, Json, CycloneDx, CycloneDxJson, Sarif, Template }`
  - `sealed class GrypeOutput { GrypeOutputFormat Format; FilePath File; static Table/Json/CycloneDx/CycloneDxJson/Sarif/Template(FilePath file = null) }`
  - `enum GrypeSortBy { Package, Severity, Epss, Risk, Kev, Vulnerability }`
  - `enum GrypeScope { Squashed, AllLayers, DeepSquashed }`
  - `[Flags] enum GrypeFixStates { None = 0, Fixed = 1, NotFixed = 2, Unknown = 4, WontFix = 8 }`
  - `sealed class GrypeScanSettings : GrypeSettings` (properties listed in the spec's Section 2 table)
  - `sealed class GrypeScanner : GrypeTool<GrypeScanSettings> { void Scan(GrypeSource source, GrypeScanSettings settings); }`
  - Aliases (namespace `Cake.Grype`): `GrypeScan(GrypeSource[, GrypeScanSettings])`, `GrypeScanSbom(FilePath[, …])`, `GrypeScanDirectory(DirectoryPath[, …])`, `GrypeScanFile(FilePath[, …])`, `GrypeScanImage(string[, …])`, `GrypeScanRegistry(string[, …])`.

- [ ] **Step 1: Write the fixture and the failing tests**

**File:** `tests/Cake.Grype.Tests/Fixtures/ScanFixture.cs`

```csharp
using Cake.Grype.Scan;

namespace Cake.Grype.Tests.Fixtures;

internal sealed class ScanFixture : GrypeFixture<GrypeScanSettings>
{
    public GrypeSource Source { get; set; } = GrypeSource.Sbom("bom.cdx.json");

    protected override void RunTool()
    {
        new GrypeScanner(FileSystem, Environment, ProcessRunner, Tools).Scan(Source, Settings);
    }
}
```

**File:** `tests/Cake.Grype.Tests/GrypeScannerTests.cs`

```csharp
using Cake.Core.IO;
using Cake.Grype.Scan;
using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeScannerTests
{
    private const string Source = "\"sbom:/Working/bom.cdx.json\"";

    private static string Args(Action<GrypeScanSettings> configure)
    {
        var fixture = new ScanFixture();
        configure(fixture.Settings);
        return fixture.Run().Args;
    }

    [Fact]
    public void Should_Throw_If_Settings_Are_Null()
    {
        var fixture = new ScanFixture { Settings = null };

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsArgumentNullException(result, "settings");
    }

    [Fact]
    public void Should_Throw_If_Source_Is_Null()
    {
        var fixture = new ScanFixture { Source = null };

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsArgumentNullException(result, "source");
    }

    [Fact]
    public void Should_Pass_Only_The_Source_By_Default()
    {
        Assert.Equal(Source, new ScanFixture().Run().Args);
    }

    [Fact]
    public void Should_Not_Redirect_Standard_Output()
    {
        Assert.False(new ScanFixture().Run().Process.RedirectStandardOutput);
    }

    [Fact]
    public void Should_Resolve_The_Source_Against_The_Settings_Working_Directory()
    {
        var args = Args(s => s.WorkingDirectory = "/Other");

        Assert.Equal("\"sbom:/Other/bom.cdx.json\"", args);
    }

    [Fact]
    public void Should_Emit_Global_Flags_Before_Scan_Flags()
    {
        var args = Args(s =>
        {
            s.Quiet = true;
            s.OnlyFixed = true;
        });

        Assert.Equal("-q --only-fixed " + Source, args);
    }

    [Theory]
    [InlineData(GrypeOutputFormat.Table, "table")]
    [InlineData(GrypeOutputFormat.Json, "json")]
    [InlineData(GrypeOutputFormat.CycloneDx, "cyclonedx")]
    [InlineData(GrypeOutputFormat.CycloneDxJson, "cyclonedx-json")]
    [InlineData(GrypeOutputFormat.Sarif, "sarif")]
    [InlineData(GrypeOutputFormat.Template, "template")]
    public void Should_Render_Output_Formats(GrypeOutputFormat format, string expected)
    {
        var output = format switch
        {
            GrypeOutputFormat.Table => GrypeOutput.Table(),
            GrypeOutputFormat.Json => GrypeOutput.Json(),
            GrypeOutputFormat.CycloneDx => GrypeOutput.CycloneDx(),
            GrypeOutputFormat.CycloneDxJson => GrypeOutput.CycloneDxJson(),
            GrypeOutputFormat.Sarif => GrypeOutput.Sarif(),
            _ => GrypeOutput.Template(),
        };

        Assert.Equal(format, output.Format);
        Assert.Equal($"-o {expected} {Source}", Args(s => s.Outputs.Add(output)));
    }

    [Fact]
    public void Should_Write_Several_Outputs_With_Absolute_File_Paths()
    {
        var args = Args(s =>
        {
            s.Outputs.Add(GrypeOutput.Table());
            s.Outputs.Add(GrypeOutput.Json("out/grype.json"));
            s.Outputs.Add(GrypeOutput.Sarif("/reports/grype.sarif"));
        });

        Assert.Equal(
            "-o table -o \"json=/Working/out/grype.json\" -o \"sarif=/reports/grype.sarif\" " + Source,
            args);
    }

    [Fact]
    public void Should_Skip_Null_Outputs()
    {
        Assert.Equal("-o table " + Source, Args(s =>
        {
            s.Outputs.Add(null);
            s.Outputs.Add(GrypeOutput.Table());
        }));
    }

    [Fact]
    public void Should_Add_The_Default_Output_File()
    {
        Assert.Equal("--file \"/Working/out/report.txt\" " + Source, Args(s => s.OutputFile = "out/report.txt"));
    }

    [Theory]
    [InlineData(GrypeSeverity.Negligible, "negligible")]
    [InlineData(GrypeSeverity.Low, "low")]
    [InlineData(GrypeSeverity.Medium, "medium")]
    [InlineData(GrypeSeverity.High, "high")]
    [InlineData(GrypeSeverity.Critical, "critical")]
    public void Should_Add_Fail_On(GrypeSeverity severity, string expected)
    {
        Assert.Equal($"-f {expected} {Source}", Args(s => s.FailOn = severity));
    }

    [Fact]
    public void Should_Reject_Fail_On_Unknown()
    {
        var fixture = new ScanFixture();
        fixture.Settings.FailOn = GrypeSeverity.Unknown;

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsArgumentException(result, "settings");
    }

    [Fact]
    public void Should_Add_The_Template_As_An_Absolute_Path()
    {
        Assert.Equal("-t \"/Working/tmpl/report.tmpl\" " + Source, Args(s => s.Template = "tmpl/report.tmpl"));
    }

    [Theory]
    [InlineData(GrypeSortBy.Package, "package")]
    [InlineData(GrypeSortBy.Severity, "severity")]
    [InlineData(GrypeSortBy.Epss, "epss")]
    [InlineData(GrypeSortBy.Risk, "risk")]
    [InlineData(GrypeSortBy.Kev, "kev")]
    [InlineData(GrypeSortBy.Vulnerability, "vulnerability")]
    public void Should_Add_Sort_By(GrypeSortBy sortBy, string expected)
    {
        Assert.Equal($"--sort-by {expected} {Source}", Args(s => s.SortBy = sortBy));
    }

    [Theory]
    [InlineData(GrypeFixStates.Fixed, "fixed")]
    [InlineData(GrypeFixStates.WontFix | GrypeFixStates.NotFixed, "not-fixed,wont-fix")]
    [InlineData(GrypeFixStates.Fixed | GrypeFixStates.NotFixed | GrypeFixStates.Unknown | GrypeFixStates.WontFix, "fixed,not-fixed,unknown,wont-fix")]
    public void Should_Add_Ignore_States(GrypeFixStates states, string expected)
    {
        Assert.Equal($"--ignore-states {expected} {Source}", Args(s => s.IgnoreStates = states));
    }

    [Theory]
    [InlineData(GrypeScope.Squashed, "squashed")]
    [InlineData(GrypeScope.AllLayers, "all-layers")]
    [InlineData(GrypeScope.DeepSquashed, "deep-squashed")]
    public void Should_Add_Scope(GrypeScope scope, string expected)
    {
        Assert.Equal($"-s {expected} {Source}", Args(s => s.Scope = scope));
    }

    [Fact]
    public void Should_Add_Switches()
    {
        Assert.Equal("--only-fixed " + Source, Args(s => s.OnlyFixed = true));
        Assert.Equal("--only-notfixed " + Source, Args(s => s.OnlyNotFixed = true));
        Assert.Equal("--by-cve " + Source, Args(s => s.ByCve = true));
        Assert.Equal("--add-cpes-if-none " + Source, Args(s => s.AddCpesIfNone = true));
        Assert.Equal("--show-suppressed " + Source, Args(s => s.ShowSuppressed = true));
    }

    [Fact]
    public void Should_Add_Values()
    {
        Assert.Equal("--distro \"debian:12\" " + Source, Args(s => s.Distro = "debian:12"));
        Assert.Equal("--platform \"linux/arm64\" " + Source, Args(s => s.Platform = "linux/arm64"));
        Assert.Equal("--name \"my-app\" " + Source, Args(s => s.Name = "my-app"));
    }

    [Fact]
    public void Should_Add_Repeatable_Options_And_Skip_Blank_Ones()
    {
        var args = Args(s =>
        {
            s.Exclude.Add("./tmp/**");
            s.Exclude.Add("");
            s.Exclude.Add("**/*.log");
            s.From.Add("registry");
            s.Vex.Add("vex/app.openvex.json");
            s.Vex.Add(null);
        });

        Assert.Equal(
            "--exclude \"./tmp/**\" --exclude \"**/*.log\" --from \"registry\" --vex \"/Working/vex/app.openvex.json\" " + Source,
            args);
    }

    [Fact]
    public void Should_Emit_All_Options_In_Order()
    {
        var args = Args(s =>
        {
            s.Quiet = true;
            s.Outputs.Add(GrypeOutput.Table());
            s.Outputs.Add(GrypeOutput.Json("out/grype.json"));
            s.OutputFile = "out/default.txt";
            s.FailOn = GrypeSeverity.High;
            s.Template = "tmpl/report.tmpl";
            s.SortBy = GrypeSortBy.Risk;
            s.OnlyFixed = true;
            s.OnlyNotFixed = true;
            s.IgnoreStates = GrypeFixStates.WontFix;
            s.ByCve = true;
            s.AddCpesIfNone = true;
            s.Distro = "debian:12";
            s.Platform = "linux/arm64";
            s.Scope = GrypeScope.AllLayers;
            s.Exclude.Add("./tmp/**");
            s.From.Add("registry");
            s.Name = "my-app";
            s.Vex.Add("vex/app.openvex.json");
            s.ShowSuppressed = true;
        });

        Assert.Equal(
            "-q -o table -o \"json=/Working/out/grype.json\" --file \"/Working/out/default.txt\" -f high " +
            "-t \"/Working/tmpl/report.tmpl\" --sort-by risk --only-fixed --only-notfixed --ignore-states wont-fix " +
            "--by-cve --add-cpes-if-none --distro \"debian:12\" --platform \"linux/arm64\" -s all-layers " +
            "--exclude \"./tmp/**\" --from \"registry\" --name \"my-app\" --vex \"/Working/vex/app.openvex.json\" " +
            "--show-suppressed " + Source,
            args);
    }

    [Fact]
    public void Should_Delete_Stale_Output_Files_Before_Running()
    {
        var fixture = new ScanFixture();
        fixture.Settings.Outputs.Add(GrypeOutput.Json("out/grype.json"));
        fixture.Settings.OutputFile = "out/report.txt";
        fixture.FileSystem.CreateFile("/Working/out/grype.json").SetContent("stale");
        fixture.FileSystem.CreateFile("/Working/out/report.txt").SetContent("stale");

        fixture.Run();

        Assert.False(fixture.FileSystem.Exist(new FilePath("/Working/out/grype.json")));
        Assert.False(fixture.FileSystem.Exist(new FilePath("/Working/out/report.txt")));
    }

    [Fact]
    public void Should_Create_Output_Directories()
    {
        var fixture = new ScanFixture();
        fixture.Settings.Outputs.Add(GrypeOutput.Json("out/json/grype.json"));
        fixture.Settings.OutputFile = "out/text/report.txt";

        fixture.Run();

        Assert.True(fixture.FileSystem.Exist(new DirectoryPath("/Working/out/json")));
        Assert.True(fixture.FileSystem.Exist(new DirectoryPath("/Working/out/text")));
    }

    [Fact]
    public void Should_Not_Touch_Files_When_Validation_Fails()
    {
        var fixture = new ScanFixture();
        fixture.Settings.Outputs.Add(GrypeOutput.Json("grype.json"));
        fixture.Settings.FailOn = GrypeSeverity.Unknown;
        fixture.FileSystem.CreateFile("/Working/grype.json").SetContent("previous");

        Record.Exception(() => fixture.Run());

        Assert.True(fixture.FileSystem.Exist(new FilePath("/Working/grype.json")));
    }

    [Fact]
    public void Should_Throw_On_Exit_Code_2_From_Fail_On()
    {
        var fixture = new ScanFixture();
        fixture.Settings.FailOn = GrypeSeverity.Critical;
        fixture.GivenProcessExitsWithCode(2);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 2).");
    }

    [Fact]
    public void Should_Not_Throw_On_Exit_Code_2_When_It_Is_Handled()
    {
        var fixture = new ScanFixture();
        fixture.Settings.FailOn = GrypeSeverity.Critical;
        fixture.Settings.HandleExitCode = code => code is 0 or 2;
        fixture.GivenProcessExitsWithCode(2);

        Assert.Null(Record.Exception(() => fixture.Run()));
    }

    [Fact]
    public void Should_Invoke_The_Users_Post_Action_Once()
    {
        var invocations = 0;
        var fixture = new ScanFixture();
        fixture.Settings.PostAction = _ => invocations++;

        fixture.Run();

        Assert.Equal(1, invocations);
    }
}
```

**File:** `tests/Cake.Grype.Tests/GrypeScanAliasesTests.cs`

```csharp
using Cake.Grype.Scan;
using Cake.Grype.Tests.Fixtures;

namespace Cake.Grype.Tests;

public sealed class GrypeScanAliasesTests
{
    [Fact]
    public void GrypeScan_Should_Accept_A_Source_String()
    {
        var alias = new AliasContext();

        alias.Context.GrypeScan("registry:alpine:3.20");

        Assert.Equal(new[] { "\"registry:alpine:3.20\"" }, alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void GrypeScan_Should_Use_Settings()
    {
        var alias = new AliasContext();

        alias.Context.GrypeScan(GrypeSource.Sbom("bom.json"), new GrypeScanSettings { Outputs = { GrypeOutput.Table() } });

        Assert.Equal(new[] { "-o table \"sbom:/Working/bom.json\"" }, alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void GrypeScan_Should_Scan_A_List_Of_Sources()
    {
        var alias = new AliasContext();
        var sources = new List<GrypeSource> { GrypeSource.Sbom("api.cdx.json"), GrypeSource.Registry("myorg/api:1.2.3") };

        foreach (var source in sources)
        {
            alias.Context.GrypeScan(source);
        }

        Assert.Equal(
            new[] { "\"sbom:/Working/api.cdx.json\"", "\"registry:myorg/api:1.2.3\"" },
            alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Shortcut_Aliases_Should_Use_Their_Scheme()
    {
        var alias = new AliasContext();

        alias.Context.GrypeScanSbom("bom.json");
        alias.Context.GrypeScanDirectory("src");
        alias.Context.GrypeScanFile("app.jar");
        alias.Context.GrypeScanImage("myorg/api:1.2.3");
        alias.Context.GrypeScanRegistry("myorg/api:1.2.3");

        Assert.Equal(
            new[]
            {
                "\"sbom:/Working/bom.json\"",
                "\"dir:/Working/src\"",
                "\"file:/Working/app.jar\"",
                "\"myorg/api:1.2.3\"",
                "\"registry:myorg/api:1.2.3\"",
            },
            alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Shortcut_Aliases_Should_Pass_Settings()
    {
        var alias = new AliasContext();
        var settings = new GrypeScanSettings { OnlyFixed = true };

        alias.Context.GrypeScanSbom("bom.json", settings);
        alias.Context.GrypeScanDirectory("src", settings);
        alias.Context.GrypeScanFile("app.jar", settings);
        alias.Context.GrypeScanImage("img", settings);
        alias.Context.GrypeScanRegistry("img", settings);

        Assert.All(alias.ProcessRunner.Arguments, args => Assert.StartsWith("--only-fixed ", args));
    }

    [Fact]
    public void GrypeScan_Should_Throw_If_Context_Is_Null()
    {
        var result = Record.Exception(() => GrypeAliases.GrypeScan(null, GrypeSource.Sbom("bom.json")));

        Assertions.IsArgumentNullException(result, "context");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test --project tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj -f net10.0 --filter-class "Cake.Grype.Tests.GrypeScannerTests"`
Expected: build FAILS — `Cake.Grype.Scan` types do not exist.

- [ ] **Step 3: Implement the scan types**

**File:** `src/Cake.Grype/Scan/GrypeOutputFormat.cs`

```csharp
namespace Cake.Grype.Scan
{
    /// <summary>
    /// A Grype report format (<c>-o</c>).
    /// </summary>
    public enum GrypeOutputFormat
    {
        /// <summary>The human-readable table (<c>table</c>), Grype's default.</summary>
        Table,

        /// <summary>Grype's native JSON (<c>json</c>); readable with <c>GrypeReadJson</c>.</summary>
        Json,

        /// <summary>CycloneDX XML with vulnerabilities (<c>cyclonedx</c>).</summary>
        CycloneDx,

        /// <summary>CycloneDX JSON with vulnerabilities (<c>cyclonedx-json</c>).</summary>
        CycloneDxJson,

        /// <summary>SARIF (<c>sarif</c>), for code-scanning dashboards.</summary>
        Sarif,

        /// <summary>A Go template (<c>template</c>); requires <see cref="GrypeScanSettings.Template"/>.</summary>
        Template,
    }
}
```

**File:** `src/Cake.Grype/Scan/GrypeOutput.cs`

```csharp
using Cake.Core.IO;

namespace Cake.Grype.Scan
{
    /// <summary>
    /// One Grype report output (<c>-o format</c> or <c>-o format=file</c>). Several outputs can be written by one scan,
    /// for example the table to the terminal and JSON to a file.
    /// </summary>
    public sealed class GrypeOutput
    {
        private GrypeOutput(GrypeOutputFormat format, FilePath file)
        {
            Format = format;
            File = file;
        }

        /// <summary>Gets the report format.</summary>
        public GrypeOutputFormat Format { get; }

        /// <summary>Gets the file the report is written to, or <c>null</c> for standard output.</summary>
        public FilePath File { get; }

        /// <summary>The table format.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput Table(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.Table, file);

        /// <summary>Grype's native JSON format.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput Json(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.Json, file);

        /// <summary>The CycloneDX XML format.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput CycloneDx(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.CycloneDx, file);

        /// <summary>The CycloneDX JSON format.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput CycloneDxJson(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.CycloneDxJson, file);

        /// <summary>The SARIF format.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput Sarif(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.Sarif, file);

        /// <summary>The Go template format; set <see cref="GrypeScanSettings.Template"/> too.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput Template(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.Template, file);
    }
}
```

**File:** `src/Cake.Grype/Scan/GrypeSortBy.cs`

```csharp
namespace Cake.Grype.Scan
{
    /// <summary>
    /// How Grype sorts matches (<c>--sort-by</c>).
    /// </summary>
    public enum GrypeSortBy
    {
        /// <summary>By package (<c>package</c>).</summary>
        Package,

        /// <summary>By severity (<c>severity</c>).</summary>
        Severity,

        /// <summary>By EPSS score (<c>epss</c>).</summary>
        Epss,

        /// <summary>By Grype's risk score (<c>risk</c>), Grype's default.</summary>
        Risk,

        /// <summary>Known exploited vulnerabilities first (<c>kev</c>).</summary>
        Kev,

        /// <summary>By vulnerability id (<c>vulnerability</c>).</summary>
        Vulnerability,
    }
}
```

**File:** `src/Cake.Grype/Scan/GrypeScope.cs`

```csharp
namespace Cake.Grype.Scan
{
    /// <summary>
    /// Which container image layers Grype analyzes (<c>-s</c>).
    /// </summary>
    public enum GrypeScope
    {
        /// <summary>The squashed final image (<c>squashed</c>), Grype's default.</summary>
        Squashed,

        /// <summary>All layers (<c>all-layers</c>).</summary>
        AllLayers,

        /// <summary>Deep squashed (<c>deep-squashed</c>).</summary>
        DeepSquashed,
    }
}
```

**File:** `src/Cake.Grype/Scan/GrypeFixStates.cs`

```csharp
using System;

namespace Cake.Grype.Scan
{
    /// <summary>
    /// Fix states whose matches Grype ignores (<c>--ignore-states</c>).
    /// </summary>
    [Flags]
    public enum GrypeFixStates
    {
        /// <summary>No fix states are ignored.</summary>
        None = 0,

        /// <summary>A fix is available (<c>fixed</c>).</summary>
        Fixed = 1,

        /// <summary>No fix is available yet (<c>not-fixed</c>).</summary>
        NotFixed = 2,

        /// <summary>The fix state is unknown (<c>unknown</c>).</summary>
        Unknown = 4,

        /// <summary>The vendor will not fix it (<c>wont-fix</c>).</summary>
        WontFix = 8,
    }
}
```

**File:** `src/Cake.Grype/Scan/GrypeScanSettings.cs`

```csharp
using System.Collections.Generic;
using Cake.Core.IO;

namespace Cake.Grype.Scan
{
    /// <summary>
    /// Contains the settings for a Grype scan.
    /// </summary>
    public sealed class GrypeScanSettings : GrypeSettings
    {
        /// <summary>
        /// Gets or sets the report outputs (<c>-o</c>, repeatable). Grype prints the table when none are set.
        /// </summary>
        public ICollection<GrypeOutput> Outputs { get; set; } = new List<GrypeOutput>();

        /// <summary>
        /// Gets or sets the file the default report is written to instead of standard output (<c>--file</c>).
        /// </summary>
        public FilePath OutputFile { get; set; }

        /// <summary>
        /// Gets or sets the severity at or above which Grype exits with code 2 (<c>-f</c>). The exit code throws a
        /// <see cref="Cake.Core.CakeException"/> after the outputs are written; accept it with
        /// <see cref="Cake.Core.Tooling.ToolSettings.HandleExitCode"/> to continue. <see cref="GrypeSeverity.Unknown"/>
        /// is not allowed.
        /// </summary>
        public GrypeSeverity? FailOn { get; set; }

        /// <summary>
        /// Gets or sets the Go template file for the <see cref="GrypeOutputFormat.Template"/> output (<c>-t</c>).
        /// </summary>
        public FilePath Template { get; set; }

        /// <summary>
        /// Gets or sets how matches are sorted (<c>--sort-by</c>).
        /// </summary>
        public GrypeSortBy? SortBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether vulnerabilities without a fix are ignored (<c>--only-fixed</c>).
        /// </summary>
        public bool OnlyFixed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether vulnerabilities with a fix are ignored (<c>--only-notfixed</c>).
        /// </summary>
        public bool OnlyNotFixed { get; set; }

        /// <summary>
        /// Gets or sets the fix states whose matches are ignored (<c>--ignore-states</c>).
        /// </summary>
        public GrypeFixStates IgnoreStates { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether results are oriented by CVE instead of the original vulnerability id
        /// (<c>--by-cve</c>).
        /// </summary>
        public bool ByCve { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether CPEs are generated for packages without CPE data
        /// (<c>--add-cpes-if-none</c>).
        /// </summary>
        public bool AddCpesIfNone { get; set; }

        /// <summary>
        /// Gets or sets the distro to match against, for example <c>debian:12</c> (<c>--distro</c>).
        /// </summary>
        public string Distro { get; set; }

        /// <summary>
        /// Gets or sets the platform for container image sources, for example <c>linux/arm64</c> (<c>--platform</c>).
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Gets or sets the image layers to analyze (<c>-s</c>).
        /// </summary>
        public GrypeScope? Scope { get; set; }

        /// <summary>
        /// Gets or sets glob expressions of paths excluded from the scan (<c>--exclude</c>, repeatable).
        /// </summary>
        public ICollection<string> Exclude { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the source behaviours to use, for example <c>registry</c> (<c>--from</c>, repeatable).
        /// </summary>
        public ICollection<string> From { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the name of the target being analyzed (<c>--name</c>).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets VEX documents to consider (<c>--vex</c>, repeatable).
        /// </summary>
        public ICollection<FilePath> Vex { get; set; } = new List<FilePath>();

        /// <summary>
        /// Gets or sets a value indicating whether suppressed matches are shown; table output only
        /// (<c>--show-suppressed</c>).
        /// </summary>
        public bool ShowSuppressed { get; set; }
    }
}
```

**File:** `src/Cake.Grype/Scan/GrypeScanner.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Scan
{
    /// <summary>
    /// Runs a Grype vulnerability scan.
    /// </summary>
    public sealed class GrypeScanner : GrypeTool<GrypeScanSettings>
    {
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeScanner" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeScanner(
            IFileSystem fileSystem,
            ICakeEnvironment environment,
            IProcessRunner processRunner,
            IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Scans a source for vulnerabilities.
        /// </summary>
        /// <remarks>
        /// Existing output files are deleted before the scan, so a failed scan cannot leave a stale report behind,
        /// and missing output directories are created.
        /// </remarks>
        /// <param name="source">The source to scan.</param>
        /// <param name="settings">The settings.</param>
        public void Scan(GrypeSource source, GrypeScanSettings settings)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(settings);

            if (settings.FailOn == GrypeSeverity.Unknown)
            {
                throw new ArgumentException(
                    "FailOn cannot be Unknown: Grype only accepts negligible, low, medium, high or critical.",
                    nameof(settings));
            }

            var workingDirectory = ResolveWorkingDirectory(settings);
            var arguments = CreateArgumentBuilder(settings);

            foreach (var output in settings.Outputs ?? Enumerable.Empty<GrypeOutput>())
            {
                if (output == null)
                {
                    continue;
                }

                arguments.Append("-o");
                if (output.File == null)
                {
                    arguments.Append(ToArgument(output.Format));
                }
                else
                {
                    var file = output.File.MakeAbsolute(workingDirectory);
                    PrepareOutputFile(file);
                    arguments.AppendQuoted(ToArgument(output.Format) + "=" + file.FullPath);
                }
            }

            if (settings.OutputFile != null)
            {
                var file = settings.OutputFile.MakeAbsolute(workingDirectory);
                PrepareOutputFile(file);
                arguments.Append("--file");
                arguments.AppendQuoted(file.FullPath);
            }

            if (settings.FailOn.HasValue)
            {
                arguments.Append("-f");
                arguments.Append(settings.FailOn.Value.ToString().ToLowerInvariant());
            }

            if (settings.Template != null)
            {
                arguments.Append("-t");
                arguments.AppendQuoted(settings.Template.MakeAbsolute(workingDirectory).FullPath);
            }

            if (settings.SortBy.HasValue)
            {
                arguments.Append("--sort-by");
                arguments.Append(settings.SortBy.Value.ToString().ToLowerInvariant());
            }

            if (settings.OnlyFixed)
            {
                arguments.Append("--only-fixed");
            }

            if (settings.OnlyNotFixed)
            {
                arguments.Append("--only-notfixed");
            }

            if (settings.IgnoreStates != GrypeFixStates.None)
            {
                arguments.Append("--ignore-states");
                arguments.Append(ToArgument(settings.IgnoreStates));
            }

            if (settings.ByCve)
            {
                arguments.Append("--by-cve");
            }

            if (settings.AddCpesIfNone)
            {
                arguments.Append("--add-cpes-if-none");
            }

            AppendValue(arguments, "--distro", settings.Distro);
            AppendValue(arguments, "--platform", settings.Platform);

            if (settings.Scope.HasValue)
            {
                arguments.Append("-s");
                arguments.Append(ToArgument(settings.Scope.Value));
            }

            foreach (var exclude in settings.Exclude ?? Enumerable.Empty<string>())
            {
                AppendValue(arguments, "--exclude", exclude);
            }

            foreach (var from in settings.From ?? Enumerable.Empty<string>())
            {
                AppendValue(arguments, "--from", from);
            }

            AppendValue(arguments, "--name", settings.Name);

            foreach (var vex in settings.Vex ?? Enumerable.Empty<FilePath>())
            {
                if (vex == null)
                {
                    continue;
                }

                arguments.Append("--vex");
                arguments.AppendQuoted(vex.MakeAbsolute(workingDirectory).FullPath);
            }

            if (settings.ShowSuppressed)
            {
                arguments.Append("--show-suppressed");
            }

            arguments.AppendQuoted(source.ToArgument(workingDirectory));

            Run(settings, arguments);
        }

        private void PrepareOutputFile(FilePath file)
        {
            var existing = _fileSystem.GetFile(file);
            if (existing.Exists)
            {
                existing.Delete();
            }

            var directory = _fileSystem.GetDirectory(file.GetDirectory());
            if (!directory.Exists)
            {
                directory.Create();
            }
        }

        private static void AppendValue(ProcessArgumentBuilder arguments, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            arguments.Append(name);
            arguments.AppendQuoted(value);
        }

        private static string ToArgument(GrypeOutputFormat format)
        {
            return format switch
            {
                GrypeOutputFormat.Table => "table",
                GrypeOutputFormat.Json => "json",
                GrypeOutputFormat.CycloneDx => "cyclonedx",
                GrypeOutputFormat.CycloneDxJson => "cyclonedx-json",
                GrypeOutputFormat.Sarif => "sarif",
                GrypeOutputFormat.Template => "template",
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
            };
        }

        private static string ToArgument(GrypeScope scope)
        {
            return scope switch
            {
                GrypeScope.Squashed => "squashed",
                GrypeScope.AllLayers => "all-layers",
                GrypeScope.DeepSquashed => "deep-squashed",
                _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
            };
        }

        private static string ToArgument(GrypeFixStates states)
        {
            var values = new List<string>();
            if (states.HasFlag(GrypeFixStates.Fixed))
            {
                values.Add("fixed");
            }

            if (states.HasFlag(GrypeFixStates.NotFixed))
            {
                values.Add("not-fixed");
            }

            if (states.HasFlag(GrypeFixStates.Unknown))
            {
                values.Add("unknown");
            }

            if (states.HasFlag(GrypeFixStates.WontFix))
            {
                values.Add("wont-fix");
            }

            return string.Join(",", values);
        }
    }
}
```

Note: validation (`FailOn == Unknown`) runs before any output file is touched, which `Should_Not_Touch_Files_When_Validation_Fails` checks.

- [ ] **Step 4: Implement the scan aliases**

**File:** `src/Cake.Grype/GrypeAliases.Scan.cs`

```csharp
using System;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.Grype.Scan;

namespace Cake.Grype
{
    public static partial class GrypeAliases
    {
        /// <summary>
        /// Scans a source for vulnerabilities with Grype. A string is passed to Grype unchanged, for example
        /// <c>"registry:alpine:3.20"</c>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="source">The source to scan.</param>
        /// <example>
        /// <code>
        /// GrypeScan(GrypeSource.Sbom("./artifacts/bom.cdx.json"));
        /// GrypeScan("registry:alpine:3.20");
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScan(this ICakeContext context, GrypeSource source)
        {
            context.GrypeScan(source, null);
        }

        /// <summary>
        /// Scans a source for vulnerabilities with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="source">The source to scan.</param>
        /// <param name="settings">The settings.</param>
        /// <example>
        /// <code>
        /// var settings = new GrypeScanSettings
        /// {
        ///     Outputs = { GrypeOutput.Table(), GrypeOutput.Json("./artifacts/grype.json") },
        /// };
        /// foreach (var source in new List&lt;GrypeSource&gt; { GrypeSource.Sbom("./artifacts/api.cdx.json"), GrypeSource.Registry("myorg/api:1.2.3") })
        /// {
        ///     GrypeScan(source, settings);
        /// }
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScan(this ICakeContext context, GrypeSource source, GrypeScanSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeScanSettings();
            new GrypeScanner(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools)
                .Scan(source, settings);
        }

        /// <summary>
        /// Scans an SBOM (Syft JSON, CycloneDX or SPDX) for vulnerabilities with Grype.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sbom">The SBOM file.</param>
        /// <example>
        /// <code>
        /// GrypeScanSbom("./artifacts/bom.cdx.json");
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanSbom(this ICakeContext context, FilePath sbom)
        {
            context.GrypeScanSbom(sbom, null);
        }

        /// <summary>
        /// Scans an SBOM (Syft JSON, CycloneDX or SPDX) for vulnerabilities with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sbom">The SBOM file.</param>
        /// <param name="settings">The settings.</param>
        /// <example>
        /// <code>
        /// GrypeScanSbom("./artifacts/bom.cdx.json", new GrypeScanSettings
        /// {
        ///     Outputs = { GrypeOutput.Table(), GrypeOutput.Json("./artifacts/grype.json") },
        ///     SortBy = GrypeSortBy.Risk,
        /// });
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanSbom(this ICakeContext context, FilePath sbom, GrypeScanSettings settings)
        {
            context.GrypeScan(GrypeSource.Sbom(sbom), settings);
        }

        /// <summary>
        /// Scans a directory for vulnerabilities with Grype.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="directory">The directory.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanDirectory(this ICakeContext context, DirectoryPath directory)
        {
            context.GrypeScanDirectory(directory, null);
        }

        /// <summary>
        /// Scans a directory for vulnerabilities with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="directory">The directory.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanDirectory(this ICakeContext context, DirectoryPath directory, GrypeScanSettings settings)
        {
            context.GrypeScan(GrypeSource.Directory(directory), settings);
        }

        /// <summary>
        /// Scans a single file for vulnerabilities with Grype.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="file">The file.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanFile(this ICakeContext context, FilePath file)
        {
            context.GrypeScanFile(file, null);
        }

        /// <summary>
        /// Scans a single file for vulnerabilities with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="file">The file.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanFile(this ICakeContext context, FilePath file, GrypeScanSettings settings)
        {
            context.GrypeScan(GrypeSource.File(file), settings);
        }

        /// <summary>
        /// Scans a container image for vulnerabilities with Grype, using Grype's default image lookup.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="image">The image reference, for example <c>myorg/api:1.2.3</c>.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanImage(this ICakeContext context, string image)
        {
            context.GrypeScanImage(image, null);
        }

        /// <summary>
        /// Scans a container image for vulnerabilities with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="image">The image reference, for example <c>myorg/api:1.2.3</c>.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanImage(this ICakeContext context, string image, GrypeScanSettings settings)
        {
            context.GrypeScan(GrypeSource.Image(image), settings);
        }

        /// <summary>
        /// Scans a container image pulled directly from a registry (no container runtime required) with Grype.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="image">The image reference, for example <c>myorg/api:1.2.3</c>.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanRegistry(this ICakeContext context, string image)
        {
            context.GrypeScanRegistry(image, null);
        }

        /// <summary>
        /// Scans a container image pulled directly from a registry with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="image">The image reference, for example <c>myorg/api:1.2.3</c>.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanRegistry(this ICakeContext context, string image, GrypeScanSettings settings)
        {
            context.GrypeScan(GrypeSource.Registry(image), settings);
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Cake.Grype.sln`
Expected: PASS (all tests so far, all TFMs).

- [ ] **Step 6: Commit**

```bash
git add src tests
git commit -m "feat: add Grype scan runner, outputs and scan aliases"
```

---

### Task 4: JSON report model, reader and `GrypeReadJson`

**Files:**
- Create: `src/Cake.Grype/Json/GrypeReport.cs`, `GrypeMatch.cs`, `GrypeArtifact.cs`, `GrypeVulnerability.cs`, `GrypeVulnerabilitySignals.cs`, `GrypeJsonConverters.cs`, `GrypeReportReader.cs`
- Create: `src/Cake.Grype/GrypeAliases.ReadJson.cs`
- Test: `tests/Cake.Grype.Tests/GrypeReportReaderTests.cs`, `tests/Cake.Grype.Tests/GrypeMatchTests.cs`
- Uses (already committed): `tests/Cake.Grype.Tests/TestData/grype-report.json`

**Interfaces:**
- Consumes: `GrypeSeverity`.
- Produces (namespace `Cake.Grype.Json`):
  - `GrypeReport { Matches, IgnoredMatches, Source, Distro, Descriptor; AtOrAbove(GrypeSeverity); CountBySeverity() }`
  - `GrypeMatch { Vulnerability, RelatedVulnerabilities, Artifact; Severity, Risk, IsKnownExploited, MaxEpssScore, MaxEpssPercentile, MaxCvssBaseScore }`, `GrypeIgnoredMatch : GrypeMatch { AppliedIgnoreRules }`, `GrypeIgnoreRule`, `GrypeIgnoreRulePackage`
  - `GrypeArtifact`, `GrypeVulnerabilityMetadata`, `GrypeVulnerability : GrypeVulnerabilityMetadata`, `GrypeFix`, `enum GrypeFixState { Unknown, Fixed, NotFixed, WontFix }`, `GrypeAdvisory`, `GrypeCvss`, `GrypeEpss`, `GrypeKnownExploited`, `GrypeCwe`, `GrypeDistro`, `GrypeDescriptor`, `GrypeReportSource`
  - `GrypeReportReader(IFileSystem, ICakeEnvironment) { GrypeReport Read(FilePath); static GrypeReport Parse(Stream) }`
  - Alias `GrypeReadJson(this ICakeContext, FilePath) : GrypeReport`

- [ ] **Step 1: Write the failing tests**

**File:** `tests/Cake.Grype.Tests/GrypeReportReaderTests.cs`

```csharp
using System.Text;
using Cake.Core;
using Cake.Core.IO;
using Cake.Grype.Json;
using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeReportReaderTests
{
    // System.IO.Path, not Cake.Core.IO.Path: both namespaces are imported here.
    private static readonly string FixturePath = System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "grype-report.json");

    private static GrypeReport ReadFixture()
    {
        using var stream = File.OpenRead(FixturePath);
        return GrypeReportReader.Parse(stream);
    }

    private static GrypeReport Parse(string json, bool withBom = false)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        if (withBom)
        {
            bytes = Encoding.UTF8.GetPreamble().Concat(bytes).ToArray();
        }

        return GrypeReportReader.Parse(new MemoryStream(bytes));
    }

    private static GrypeMatch Match(GrypeReport report, string id) =>
        Assert.Single(report.Matches, m => m.Vulnerability.Id == id);

    [Fact]
    public void Should_Read_The_Document_Structure()
    {
        var report = ReadFixture();

        Assert.Equal(4, report.Matches.Count);
        Assert.Single(report.IgnoredMatches);
        Assert.Equal("grype", report.Descriptor.Name);
        Assert.Equal("0.119.0", report.Descriptor.Version);
        Assert.Equal(DateTimeOffset.Parse("2026-09-22T20:15:25.2703298+02:00"), report.Descriptor.Timestamp);
        Assert.Equal("debian", report.Distro.Name);
        Assert.Equal("12", report.Distro.Version);
        Assert.Equal(new[] { "debian" }, report.Distro.IdLike);
        Assert.Equal("image", report.Source.Type);
        Assert.Equal("docker.io/library/varnish", report.Source.Target.GetProperty("userInput").GetString());
    }

    [Fact]
    public void Should_Read_A_Known_Exploited_Vulnerability()
    {
        var match = Match(ReadFixture(), "CVE-2023-44487");

        Assert.Equal("varnish", match.Artifact.Name);
        Assert.Equal("7.6.0", match.Artifact.Version);
        Assert.Equal("deb", match.Artifact.Type);
        Assert.Equal("pkg:deb/debian/varnish@7.6.0?arch=amd64&distro=debian-12", match.Artifact.Purl);
        Assert.Equal(GrypeSeverity.High, match.Vulnerability.Severity);
        Assert.Equal("debian:distro:debian:12", match.Vulnerability.Namespace);
        Assert.Equal(GrypeFixState.WontFix, match.Vulnerability.Fix.State);
        Assert.Empty(match.Vulnerability.Fix.Versions);
        Assert.Equal(78.75, match.Vulnerability.Risk, 2);

        var kev = Assert.Single(match.Vulnerability.KnownExploited);
        Assert.Equal("CVE-2023-44487", kev.Cve);
        Assert.Equal("IETF", kev.VendorProject);
        Assert.Equal("HTTP/2", kev.Product);
        Assert.Equal("2023-10-10", kev.DateAdded);
        Assert.Equal("2023-10-31", kev.DueDate);
        Assert.Equal("unknown", kev.KnownRansomwareCampaignUse);
        Assert.Equal(new[] { "CWE-400" }, kev.Cwes);

        var epss = Assert.Single(match.Vulnerability.Epss);
        Assert.Equal(0.99999, epss.Score, 5);
        Assert.Equal(0.99999, epss.Percentile, 5);

        var cvss = Assert.Single(match.Vulnerability.Cvss);
        Assert.Equal("3.1", cvss.Version);
        Assert.Equal("CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:N/A:H", cvss.Vector);
        Assert.Equal(7.5, cvss.BaseScore);
        Assert.Equal(3.9, cvss.ExploitabilityScore);
        Assert.Equal(3.6, cvss.ImpactScore);

        var related = Assert.Single(match.RelatedVulnerabilities);
        Assert.Equal("CVE-2023-44487", related.Id);
        Assert.Equal("nvd:cpe", related.Namespace);
    }

    [Fact]
    public void Should_Read_A_Fixed_Critical_Vulnerability()
    {
        var match = Match(ReadFixture(), "CVE-2025-15467");

        Assert.Equal("libssl3", match.Artifact.Name);
        Assert.Equal(GrypeSeverity.Critical, match.Vulnerability.Severity);
        Assert.Equal(GrypeFixState.Fixed, match.Vulnerability.Fix.State);
        Assert.Equal(new[] { "3.0.18-1~deb12u2" }, match.Vulnerability.Fix.Versions);
        var advisory = Assert.Single(match.Vulnerability.Advisories);
        Assert.Equal("DSA-6113-1", advisory.Id);
        Assert.Equal("https://security-tracker.debian.org/tracker/DSA-6113-1", advisory.Link);
        Assert.Contains("Apache-2.0", match.Artifact.Licenses);
        Assert.Single(match.Artifact.Cpes);
    }

    [Fact]
    public void Should_Read_A_Vulnerability_With_Cvss_Only_On_The_Related_Record()
    {
        var match = Match(ReadFixture(), "CVE-2016-2781");

        Assert.Empty(match.Vulnerability.Cvss);
        Assert.Equal(new double?[] { 6.5, 2.1, 4.6 }, match.RelatedVulnerabilities[0].Cvss.Select(c => c.BaseScore));
    }

    [Fact]
    public void Should_Read_A_Reserved_Cve_With_Empty_Lists()
    {
        var match = Match(ReadFixture(), "CVE-2026-53613");

        Assert.Equal(GrypeSeverity.Unknown, match.Vulnerability.Severity);
        Assert.Equal(0, match.Vulnerability.Risk);
        Assert.Equal(GrypeFixState.NotFixed, match.Vulnerability.Fix.State);
        Assert.Null(match.Vulnerability.Description);
        Assert.Empty(match.Vulnerability.Epss);
        Assert.Empty(match.Vulnerability.KnownExploited);
        Assert.Empty(match.Vulnerability.Cvss);
        Assert.Empty(match.Vulnerability.Cwes);
    }

    [Fact]
    public void Should_Read_Ignored_Matches_With_Their_Rules()
    {
        var ignored = Assert.Single(ReadFixture().IgnoredMatches);

        Assert.Equal("CVE-2004-0230", ignored.Vulnerability.Id);
        Assert.Equal("linux-libc-dev", ignored.Artifact.Name);
        var rule = Assert.Single(ignored.AppliedIgnoreRules);
        Assert.Equal("exact-indirect-match", rule.MatchType);
        Assert.Equal("linux-libc-dev", rule.Package.Name);
        Assert.Equal("deb", rule.Package.Type);
        Assert.Equal("linux", rule.Package.UpstreamName);
    }

    [Fact]
    public void Should_Count_By_Severity_Including_Empty_Severities()
    {
        var counts = ReadFixture().CountBySeverity();

        Assert.Equal(6, counts.Count);
        Assert.Equal(1, counts[GrypeSeverity.Critical]);
        Assert.Equal(1, counts[GrypeSeverity.High]);
        Assert.Equal(0, counts[GrypeSeverity.Medium]);
        Assert.Equal(1, counts[GrypeSeverity.Low]);
        Assert.Equal(0, counts[GrypeSeverity.Negligible]);
        Assert.Equal(1, counts[GrypeSeverity.Unknown]);
    }

    [Fact]
    public void Should_Filter_At_Or_Above_A_Severity()
    {
        var ids = ReadFixture().AtOrAbove(GrypeSeverity.High).Select(m => m.Vulnerability.Id);

        Assert.Equal(new[] { "CVE-2023-44487", "CVE-2025-15467" }, ids);
    }

    [Fact]
    public void Should_Treat_Null_And_Missing_Arrays_As_Empty()
    {
        var report = Parse("""
            {
              "matches": [
                { "vulnerability": { "id": "CVE-1", "severity": "Low", "urls": null, "cvss": null, "fix": { "state": "fixed", "versions": null } },
                  "relatedVulnerabilities": null,
                  "artifact": { "name": "a", "licenses": null } }
              ],
              "ignoredMatches": null
            }
            """);

        var match = Assert.Single(report.Matches);
        Assert.Empty(match.Vulnerability.Urls);
        Assert.Empty(match.Vulnerability.Cvss);
        Assert.Empty(match.Vulnerability.Epss);
        Assert.Empty(match.Vulnerability.Fix.Versions);
        Assert.Empty(match.RelatedVulnerabilities);
        Assert.Empty(match.Artifact.Licenses);
        Assert.Empty(match.Artifact.Cpes);
        Assert.Empty(report.IgnoredMatches);
    }

    [Fact]
    public void Should_Read_An_Empty_Document()
    {
        var report = Parse("{}");

        Assert.Empty(report.Matches);
        Assert.Empty(report.IgnoredMatches);
        Assert.Null(report.Distro);
    }

    [Theory]
    [InlineData("\"Critical\"", GrypeSeverity.Critical)]
    [InlineData("\"critical\"", GrypeSeverity.Critical)]
    [InlineData("\"Negligible\"", GrypeSeverity.Negligible)]
    [InlineData("\"Unknown\"", GrypeSeverity.Unknown)]
    [InlineData("\"Severe\"", GrypeSeverity.Unknown)]
    [InlineData("\"3\"", GrypeSeverity.Unknown)]
    [InlineData("3", GrypeSeverity.Unknown)]
    [InlineData("null", GrypeSeverity.Unknown)]
    [InlineData("{ \"x\": 1 }", GrypeSeverity.Unknown)]
    public void Should_Parse_Severities_Like_Grype(string json, GrypeSeverity expected)
    {
        var report = Parse($$"""{ "matches": [ { "vulnerability": { "id": "CVE-1", "severity": {{json}} } } ] }""");

        Assert.Equal(expected, report.Matches[0].Vulnerability.Severity);
    }

    [Theory]
    [InlineData("\"fixed\"", GrypeFixState.Fixed)]
    [InlineData("\"not-fixed\"", GrypeFixState.NotFixed)]
    [InlineData("\"wont-fix\"", GrypeFixState.WontFix)]
    [InlineData("\"unknown\"", GrypeFixState.Unknown)]
    [InlineData("\"something-new\"", GrypeFixState.Unknown)]
    [InlineData("null", GrypeFixState.Unknown)]
    public void Should_Parse_Fix_States(string json, GrypeFixState expected)
    {
        var report = Parse($$"""{ "matches": [ { "vulnerability": { "id": "CVE-1", "fix": { "state": {{json}} } } } ] }""");

        Assert.Equal(expected, report.Matches[0].Vulnerability.Fix.State);
    }

    [Fact]
    public void Should_Default_A_Missing_Fix_To_Unknown()
    {
        var report = Parse("""{ "matches": [ { "vulnerability": { "id": "CVE-1" } } ] }""");

        Assert.Equal(GrypeFixState.Unknown, report.Matches[0].Vulnerability.Fix.State);
    }

    [Fact]
    public void Should_Ignore_Unknown_Properties()
    {
        var report = Parse("""{ "future": { "a": [1, 2] }, "matches": [ { "newField": true, "vulnerability": { "id": "CVE-1" } } ] }""");

        Assert.Equal("CVE-1", report.Matches[0].Vulnerability.Id);
    }

    [Fact]
    public void Should_Tolerate_A_Byte_Order_Mark()
    {
        var report = Parse("""{ "matches": [ { "vulnerability": { "id": "CVE-1" } } ] }""", withBom: true);

        Assert.Single(report.Matches);
    }

    [Fact]
    public void Should_Throw_A_Cake_Exception_For_Invalid_Json()
    {
        var result = Record.Exception(() => Parse("{ \"matches\": [ "));

        var exception = Assert.IsType<CakeException>(result);
        Assert.StartsWith("Grype: The JSON report is not valid: ", exception.Message);
    }

    [Fact]
    public void Should_Throw_If_The_Stream_Is_Null()
    {
        var result = Record.Exception(() => GrypeReportReader.Parse(null));

        Assertions.IsArgumentNullException(result, "stream");
    }

    [Fact]
    public void Should_Read_A_Relative_Path_From_The_Working_Directory()
    {
        var environment = FakeEnvironment.CreateUnixEnvironment();
        var fileSystem = new FakeFileSystem(environment);
        fileSystem.CreateFile("/Working/out/grype.json").SetContent("""{ "matches": [ { "vulnerability": { "id": "CVE-1" } } ] }""");

        var report = new GrypeReportReader(fileSystem, environment).Read("out/grype.json");

        Assert.Single(report.Matches);
    }

    [Fact]
    public void Should_Throw_If_The_File_Does_Not_Exist()
    {
        var environment = FakeEnvironment.CreateUnixEnvironment();
        var reader = new GrypeReportReader(new FakeFileSystem(environment), environment);

        var result = Record.Exception(() => reader.Read("missing.json"));

        var exception = Assert.IsType<FileNotFoundException>(result);
        Assert.Equal("/Working/missing.json", exception.FileName);
    }

    [Fact]
    public void Alias_Should_Read_The_Report()
    {
        var alias = new AliasContext();
        alias.FileSystem.CreateFile("/Working/grype.json").SetContent(File.ReadAllText(FixturePath));

        var report = alias.Context.GrypeReadJson("grype.json");

        Assert.Equal(4, report.Matches.Count);
    }

    [Fact]
    public void Alias_Should_Throw_If_Context_Is_Null()
    {
        var result = Record.Exception(() => GrypeAliases.GrypeReadJson(null, new FilePath("grype.json")));

        Assertions.IsArgumentNullException(result, "context");
    }
}
```

**File:** `tests/Cake.Grype.Tests/GrypeMatchTests.cs`

```csharp
using System.Text;
using Cake.Grype.Json;

namespace Cake.Grype.Tests;

public sealed class GrypeMatchTests
{
    private static GrypeMatch ParseMatch(string matchJson)
    {
        var json = "{ \"matches\": [ " + matchJson + " ] }";
        return GrypeReportReader.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json))).Matches.Single();
    }

    [Fact]
    public void Signals_Should_Read_The_Primary_Vulnerability()
    {
        var match = ParseMatch("""
            { "vulnerability": { "id": "CVE-1", "severity": "High", "risk": 42.5,
                "knownExploited": [ { "cve": "CVE-1" } ],
                "epss": [ { "cve": "CVE-1", "epss": 0.3, "percentile": 0.9 } ],
                "cvss": [ { "version": "3.1", "metrics": { "baseScore": 8.1 } } ] } }
            """);

        Assert.Equal(GrypeSeverity.High, match.Severity);
        Assert.Equal(42.5, match.Risk);
        Assert.True(match.IsKnownExploited);
        Assert.Equal(0.3, match.MaxEpssScore);
        Assert.Equal(0.9, match.MaxEpssPercentile);
        Assert.Equal(8.1, match.MaxCvssBaseScore);
    }

    [Fact]
    public void Signals_Should_Include_Related_Vulnerabilities()
    {
        var match = ParseMatch("""
            { "vulnerability": { "id": "DSA-1", "severity": "Medium",
                "epss": [ { "cve": "CVE-1", "epss": 0.1, "percentile": 0.5 } ] },
              "relatedVulnerabilities": [
                { "id": "CVE-1", "knownExploited": [ { "cve": "CVE-1" } ],
                  "epss": [ { "cve": "CVE-1", "epss": 0.7, "percentile": 0.95 } ],
                  "cvss": [ { "metrics": { "baseScore": 6.5 } }, { "metrics": { "baseScore": 9.1 } } ] } ] }
            """);

        Assert.True(match.IsKnownExploited);
        Assert.Equal(0.7, match.MaxEpssScore);
        Assert.Equal(0.95, match.MaxEpssPercentile);
        Assert.Equal(9.1, match.MaxCvssBaseScore);
    }

    [Fact]
    public void Signals_Should_Be_Empty_For_An_Unassessed_Vulnerability()
    {
        var match = ParseMatch("""
            { "vulnerability": { "id": "CVE-2026-53613", "severity": "Unknown", "risk": 0, "cvss": [] },
              "relatedVulnerabilities": [ { "id": "CVE-2026-53613", "severity": "Unknown", "cvss": [] } ] }
            """);

        Assert.Equal(GrypeSeverity.Unknown, match.Severity);
        Assert.Equal(0, match.Risk);
        Assert.False(match.IsKnownExploited);
        Assert.Null(match.MaxEpssScore);
        Assert.Null(match.MaxEpssPercentile);
        Assert.Null(match.MaxCvssBaseScore);
    }

    [Fact]
    public void Signals_Should_Ignore_Cvss_Entries_Without_Metrics()
    {
        var match = ParseMatch("""{ "vulnerability": { "id": "CVE-1", "cvss": [ { "version": "2.0" } ] } }""");

        Assert.Null(match.MaxCvssBaseScore);
    }

    [Fact]
    public void Signals_Should_Tolerate_A_Missing_Vulnerability()
    {
        var match = ParseMatch("""{ "artifact": { "name": "a" } }""");

        Assert.Equal(GrypeSeverity.Unknown, match.Severity);
        Assert.Equal(0, match.Risk);
        Assert.False(match.IsKnownExploited);
        Assert.Null(match.MaxCvssBaseScore);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test --project tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj -f net10.0 --filter-class "Cake.Grype.Tests.GrypeReportReaderTests"`
Expected: build FAILS — `Cake.Grype.Json` types do not exist.

- [ ] **Step 3: Implement the model**

All model types are read-only after deserialization (`init` setters). List properties default to an empty array; the reader's converter turns an explicit JSON `null` into an empty list too.

**File:** `src/Cake.Grype/Json/GrypeReport.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Cake.Grype.Json
{
    /// <summary>
    /// A Grype JSON report (<c>-o json</c>).
    /// </summary>
    public sealed class GrypeReport
    {
        /// <summary>Gets the vulnerability matches.</summary>
        public IReadOnlyList<GrypeMatch> Matches { get; init; } = Array.Empty<GrypeMatch>();

        /// <summary>Gets the matches suppressed by ignore rules or VEX documents.</summary>
        public IReadOnlyList<GrypeIgnoredMatch> IgnoredMatches { get; init; } = Array.Empty<GrypeIgnoredMatch>();

        /// <summary>Gets what was scanned.</summary>
        public GrypeReportSource Source { get; init; }

        /// <summary>Gets the Linux distribution matched against, if any.</summary>
        public GrypeDistro Distro { get; init; }

        /// <summary>Gets information about the Grype run that produced the report.</summary>
        public GrypeDescriptor Descriptor { get; init; }

        /// <summary>
        /// Gets the matches with a severity at or above <paramref name="severity"/>.
        /// <see cref="GrypeSeverity.Unknown"/> (unassessed) matches are only included for a threshold of <c>Unknown</c>.
        /// </summary>
        /// <param name="severity">The minimum severity.</param>
        /// <returns>The matches.</returns>
        public IEnumerable<GrypeMatch> AtOrAbove(GrypeSeverity severity)
        {
            return Matches.Where(match => match.Severity >= severity);
        }

        /// <summary>
        /// Counts the matches per severity. Every severity is present, with zero if nothing matched it.
        /// </summary>
        /// <returns>The count per severity.</returns>
        public IReadOnlyDictionary<GrypeSeverity, int> CountBySeverity()
        {
            var counts = Enum.GetValues<GrypeSeverity>().ToDictionary(severity => severity, _ => 0);
            foreach (var match in Matches)
            {
                counts[match.Severity]++;
            }

            return counts;
        }
    }

    /// <summary>
    /// What Grype scanned.
    /// </summary>
    public sealed class GrypeReportSource
    {
        /// <summary>Gets the source type, for example <c>image</c>, <c>directory</c> or <c>file</c>.</summary>
        public string Type { get; init; }

        /// <summary>Gets the source target; its shape depends on <see cref="Type"/>.</summary>
        public JsonElement Target { get; init; }
    }

    /// <summary>
    /// A Linux distribution.
    /// </summary>
    public sealed class GrypeDistro
    {
        /// <summary>Gets the distribution name, for example <c>debian</c>.</summary>
        public string Name { get; init; }

        /// <summary>Gets the distribution version, for example <c>12</c>.</summary>
        public string Version { get; init; }

        /// <summary>Gets the distributions this one is like.</summary>
        public IReadOnlyList<string> IdLike { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Information about the Grype run that produced a report.
    /// </summary>
    public sealed class GrypeDescriptor
    {
        /// <summary>Gets the tool name (<c>grype</c>).</summary>
        public string Name { get; init; }

        /// <summary>Gets the Grype version.</summary>
        public string Version { get; init; }

        /// <summary>Gets when the report was produced.</summary>
        public DateTimeOffset? Timestamp { get; init; }
    }
}
```

**File:** `src/Cake.Grype/Json/GrypeMatch.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Cake.Grype.Json
{
    /// <summary>
    /// A vulnerability found in a package.
    /// </summary>
    /// <remarks>
    /// The signal helpers (<see cref="IsKnownExploited"/>, <see cref="MaxEpssScore"/>, <see cref="MaxCvssBaseScore"/>, …)
    /// look at the <see cref="Vulnerability"/> and all <see cref="RelatedVulnerabilities"/>: distro advisories often carry
    /// no CVSS themselves, only the related NVD record does.
    /// </remarks>
    public class GrypeMatch
    {
        /// <summary>Gets the matched vulnerability.</summary>
        public GrypeVulnerability Vulnerability { get; init; }

        /// <summary>Gets related records of the same vulnerability, for example the NVD entry for a distro advisory.</summary>
        public IReadOnlyList<GrypeVulnerabilityMetadata> RelatedVulnerabilities { get; init; } = Array.Empty<GrypeVulnerabilityMetadata>();

        /// <summary>Gets the vulnerable package.</summary>
        public GrypeArtifact Artifact { get; init; }

        /// <summary>
        /// Gets the severity Grype assigned to the match. <see cref="GrypeSeverity.Unknown"/> means "not assessed yet".
        /// </summary>
        [JsonIgnore]
        public GrypeSeverity Severity => Vulnerability?.Severity ?? GrypeSeverity.Unknown;

        /// <summary>Gets Grype's combined risk score (0–100) from severity, EPSS and KEV; 0 when unassessed.</summary>
        [JsonIgnore]
        public double Risk => Vulnerability?.Risk ?? 0;

        /// <summary>Gets a value indicating whether any record lists the vulnerability as known exploited (CISA KEV).</summary>
        [JsonIgnore]
        public bool IsKnownExploited => Records.Any(record => record.KnownExploited.Count > 0);

        /// <summary>Gets the highest EPSS probability (0–1) of any record, or <c>null</c> if none has EPSS data.</summary>
        [JsonIgnore]
        public double? MaxEpssScore => Records.SelectMany(record => record.Epss).Select(epss => (double?)epss.Score).Max();

        /// <summary>Gets the highest EPSS percentile (0–1) of any record, or <c>null</c> if none has EPSS data.</summary>
        [JsonIgnore]
        public double? MaxEpssPercentile => Records.SelectMany(record => record.Epss).Select(epss => (double?)epss.Percentile).Max();

        /// <summary>Gets the highest CVSS base score of any record, or <c>null</c> if none has CVSS data.</summary>
        [JsonIgnore]
        public double? MaxCvssBaseScore => Records.SelectMany(record => record.Cvss).Select(cvss => cvss.BaseScore).Max();

        private IEnumerable<GrypeVulnerabilityMetadata> Records
        {
            get
            {
                if (Vulnerability != null)
                {
                    yield return Vulnerability;
                }

                foreach (var related in RelatedVulnerabilities.Where(related => related != null))
                {
                    yield return related;
                }
            }
        }
    }

    /// <summary>
    /// A match suppressed by an ignore rule or a VEX document.
    /// </summary>
    public sealed class GrypeIgnoredMatch : GrypeMatch
    {
        /// <summary>Gets the rules that suppressed the match.</summary>
        public IReadOnlyList<GrypeIgnoreRule> AppliedIgnoreRules { get; init; } = Array.Empty<GrypeIgnoreRule>();
    }

    /// <summary>
    /// An ignore rule that suppressed a match.
    /// </summary>
    public sealed class GrypeIgnoreRule
    {
        /// <summary>Gets the ignored vulnerability id, if the rule names one.</summary>
        public string Vulnerability { get; init; }

        /// <summary>Gets the reason given for the rule.</summary>
        public string Reason { get; init; }

        /// <summary>Gets the vulnerability namespace the rule applies to.</summary>
        public string Namespace { get; init; }

        /// <summary>Gets the fix state the rule applies to.</summary>
        [JsonPropertyName("fix-state")]
        public string FixState { get; init; }

        /// <summary>Gets the match type the rule applies to, for example <c>exact-indirect-match</c>.</summary>
        [JsonPropertyName("match-type")]
        public string MatchType { get; init; }

        /// <summary>Gets the package the rule applies to.</summary>
        public GrypeIgnoreRulePackage Package { get; init; }
    }

    /// <summary>
    /// The package part of an ignore rule.
    /// </summary>
    public sealed class GrypeIgnoreRulePackage
    {
        /// <summary>Gets the package name.</summary>
        public string Name { get; init; }

        /// <summary>Gets the package version.</summary>
        public string Version { get; init; }

        /// <summary>Gets the package language.</summary>
        public string Language { get; init; }

        /// <summary>Gets the package type, for example <c>deb</c>.</summary>
        public string Type { get; init; }

        /// <summary>Gets the package location glob.</summary>
        public string Location { get; init; }

        /// <summary>Gets the upstream package name.</summary>
        [JsonPropertyName("upstream-name")]
        public string UpstreamName { get; init; }
    }
}
```

**File:** `src/Cake.Grype/Json/GrypeArtifact.cs`

```csharp
using System;
using System.Collections.Generic;

namespace Cake.Grype.Json
{
    /// <summary>
    /// The package a vulnerability was found in.
    /// </summary>
    public sealed class GrypeArtifact
    {
        /// <summary>Gets Grype's package id.</summary>
        public string Id { get; init; }

        /// <summary>Gets the package name.</summary>
        public string Name { get; init; }

        /// <summary>Gets the installed version.</summary>
        public string Version { get; init; }

        /// <summary>Gets the package type, for example <c>deb</c>, <c>npm</c> or <c>nuget</c>.</summary>
        public string Type { get; init; }

        /// <summary>Gets the package language, if any.</summary>
        public string Language { get; init; }

        /// <summary>Gets the package URL.</summary>
        public string Purl { get; init; }

        /// <summary>Gets the declared licenses.</summary>
        public IReadOnlyList<string> Licenses { get; init; } = Array.Empty<string>();

        /// <summary>Gets the CPEs used for matching.</summary>
        public IReadOnlyList<string> Cpes { get; init; } = Array.Empty<string>();
    }
}
```

**File:** `src/Cake.Grype/Json/GrypeVulnerability.cs`

```csharp
using System;
using System.Collections.Generic;

namespace Cake.Grype.Json
{
    /// <summary>
    /// A vulnerability record: the matched vulnerability or a related one.
    /// </summary>
    public class GrypeVulnerabilityMetadata
    {
        /// <summary>Gets the vulnerability id, for example <c>CVE-2023-44487</c> or <c>GHSA-…</c>.</summary>
        public string Id { get; init; }

        /// <summary>Gets the URL of the record at its data source.</summary>
        public string DataSource { get; init; }

        /// <summary>Gets the vulnerability namespace, for example <c>nvd:cpe</c> or <c>debian:distro:debian:12</c>.</summary>
        public string Namespace { get; init; }

        /// <summary>Gets the severity; <see cref="GrypeSeverity.Unknown"/> when not assessed or unrecognised.</summary>
        public GrypeSeverity Severity { get; init; }

        /// <summary>Gets the description, or <c>null</c> (for example for a reserved CVE).</summary>
        public string Description { get; init; }

        /// <summary>Gets reference URLs.</summary>
        public IReadOnlyList<string> Urls { get; init; } = Array.Empty<string>();

        /// <summary>Gets CVSS scores: potential technical impact.</summary>
        public IReadOnlyList<GrypeCvss> Cvss { get; init; } = Array.Empty<GrypeCvss>();

        /// <summary>Gets EPSS scores: the modelled probability of exploitation.</summary>
        public IReadOnlyList<GrypeEpss> Epss { get; init; } = Array.Empty<GrypeEpss>();

        /// <summary>Gets CISA Known Exploited Vulnerabilities entries: exploitation has been observed.</summary>
        public IReadOnlyList<GrypeKnownExploited> KnownExploited { get; init; } = Array.Empty<GrypeKnownExploited>();

        /// <summary>Gets CWE weakness classifications.</summary>
        public IReadOnlyList<GrypeCwe> Cwes { get; init; } = Array.Empty<GrypeCwe>();
    }

    /// <summary>
    /// The matched vulnerability.
    /// </summary>
    public sealed class GrypeVulnerability : GrypeVulnerabilityMetadata
    {
        /// <summary>Gets the fix information.</summary>
        public GrypeFix Fix { get; init; } = new GrypeFix();

        /// <summary>Gets vendor advisories.</summary>
        public IReadOnlyList<GrypeAdvisory> Advisories { get; init; } = Array.Empty<GrypeAdvisory>();

        /// <summary>Gets Grype's combined risk score (0–100) from severity, EPSS and KEV.</summary>
        public double Risk { get; init; }
    }

    /// <summary>
    /// Whether and where a vulnerability is fixed.
    /// </summary>
    public sealed class GrypeFix
    {
        /// <summary>Gets the fix state.</summary>
        public GrypeFixState State { get; init; }

        /// <summary>Gets the versions the vulnerability is fixed in.</summary>
        public IReadOnlyList<string> Versions { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// A vulnerability's fix state.
    /// </summary>
    public enum GrypeFixState
    {
        /// <summary>Unknown or unrecognised (<c>unknown</c>).</summary>
        Unknown,

        /// <summary>A fix is available (<c>fixed</c>).</summary>
        Fixed,

        /// <summary>No fix is available yet (<c>not-fixed</c>).</summary>
        NotFixed,

        /// <summary>The vendor will not fix it (<c>wont-fix</c>).</summary>
        WontFix,
    }

    /// <summary>
    /// A vendor advisory.
    /// </summary>
    public sealed class GrypeAdvisory
    {
        /// <summary>Gets the advisory id, for example <c>DSA-6113-1</c>.</summary>
        public string Id { get; init; }

        /// <summary>Gets the advisory URL.</summary>
        public string Link { get; init; }
    }
}
```

**File:** `src/Cake.Grype/Json/GrypeVulnerabilitySignals.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cake.Grype.Json
{
    /// <summary>
    /// A CVSS score: potential technical impact, not whether exploitation is occurring.
    /// </summary>
    public sealed class GrypeCvss
    {
        /// <summary>Gets who scored it, for example <c>nvd@nist.gov</c>.</summary>
        public string Source { get; init; }

        /// <summary>Gets the score type, for example <c>Primary</c>.</summary>
        public string Type { get; init; }

        /// <summary>Gets the CVSS version, for example <c>3.1</c>.</summary>
        public string Version { get; init; }

        /// <summary>Gets the CVSS vector.</summary>
        public string Vector { get; init; }

        /// <summary>Gets the base score (0–10).</summary>
        [JsonIgnore]
        public double? BaseScore => Metrics?.BaseScore;

        /// <summary>Gets the exploitability sub-score.</summary>
        [JsonIgnore]
        public double? ExploitabilityScore => Metrics?.ExploitabilityScore;

        /// <summary>Gets the impact sub-score.</summary>
        [JsonIgnore]
        public double? ImpactScore => Metrics?.ImpactScore;

        [JsonInclude]
        [JsonPropertyName("metrics")]
        internal GrypeCvssMetrics Metrics { get; init; }
    }

    /// <summary>
    /// The <c>metrics</c> object of a CVSS entry; flattened onto <see cref="GrypeCvss"/>.
    /// </summary>
    internal sealed class GrypeCvssMetrics
    {
        public double? BaseScore { get; init; }

        public double? ExploitabilityScore { get; init; }

        public double? ImpactScore { get; init; }
    }

    /// <summary>
    /// An EPSS score: the modelled probability that the vulnerability will be exploited.
    /// </summary>
    public sealed class GrypeEpss
    {
        /// <summary>Gets the CVE the score is for.</summary>
        public string Cve { get; init; }

        /// <summary>Gets the probability of exploitation in the next 30 days (0–1).</summary>
        [JsonPropertyName("epss")]
        public double Score { get; init; }

        /// <summary>Gets the percentile of the score among all scored CVEs (0–1).</summary>
        public double Percentile { get; init; }

        /// <summary>Gets the date of the score (<c>yyyy-MM-dd</c>).</summary>
        public string Date { get; init; }
    }

    /// <summary>
    /// A CISA Known Exploited Vulnerabilities (KEV) entry: exploitation has been observed.
    /// </summary>
    public sealed class GrypeKnownExploited
    {
        /// <summary>Gets the CVE.</summary>
        public string Cve { get; init; }

        /// <summary>Gets the vendor or project.</summary>
        public string VendorProject { get; init; }

        /// <summary>Gets the product.</summary>
        public string Product { get; init; }

        /// <summary>Gets the date the entry was added to the catalog (<c>yyyy-MM-dd</c>).</summary>
        public string DateAdded { get; init; }

        /// <summary>Gets the required action.</summary>
        public string RequiredAction { get; init; }

        /// <summary>Gets the remediation due date for US federal agencies (<c>yyyy-MM-dd</c>).</summary>
        public string DueDate { get; init; }

        /// <summary>Gets whether it is known to be used in ransomware campaigns (<c>Known</c> or <c>unknown</c>).</summary>
        public string KnownRansomwareCampaignUse { get; init; }

        /// <summary>Gets notes.</summary>
        public string Notes { get; init; }

        /// <summary>Gets reference URLs.</summary>
        public IReadOnlyList<string> Urls { get; init; } = Array.Empty<string>();

        /// <summary>Gets CWE ids.</summary>
        public IReadOnlyList<string> Cwes { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// A CWE weakness classification.
    /// </summary>
    public sealed class GrypeCwe
    {
        /// <summary>Gets the CVE.</summary>
        public string Cve { get; init; }

        /// <summary>Gets the CWE id, for example <c>CWE-400</c>.</summary>
        public string Cwe { get; init; }

        /// <summary>Gets who classified it.</summary>
        public string Source { get; init; }

        /// <summary>Gets the classification type, for example <c>Primary</c>.</summary>
        public string Type { get; init; }
    }
}
```

- [ ] **Step 4: Implement the converters, reader and alias**

**File:** `src/Cake.Grype/Json/GrypeJsonConverters.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cake.Grype.Json
{
    /// <summary>
    /// Reads JSON <c>null</c> as an empty list for every <see cref="IReadOnlyList{T}"/> property.
    /// </summary>
    internal sealed class NullAsEmptyListConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var elementType = typeToConvert.GetGenericArguments()[0];
            return (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(elementType));
        }

        private sealed class Converter<T> : JsonConverter<IReadOnlyList<T>>
        {
            public override bool HandleNull => true;

            public override IReadOnlyList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return Array.Empty<T>();
                }

                return (IReadOnlyList<T>)JsonSerializer.Deserialize<List<T>>(ref reader, options) ?? Array.Empty<T>();
            }

            public override void Write(Utf8JsonWriter writer, IReadOnlyList<T> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }
    }

    /// <summary>
    /// Reads severities the way Grype's <c>ParseSeverity</c> does: case-insensitive names, anything else is Unknown.
    /// </summary>
    internal sealed class GrypeSeverityConverter : JsonConverter<GrypeSeverity>
    {
        public override GrypeSeverity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString()?.ToLowerInvariant() switch
                {
                    "negligible" => GrypeSeverity.Negligible,
                    "low" => GrypeSeverity.Low,
                    "medium" => GrypeSeverity.Medium,
                    "high" => GrypeSeverity.High,
                    "critical" => GrypeSeverity.Critical,
                    _ => GrypeSeverity.Unknown,
                };
            }

            reader.Skip();
            return GrypeSeverity.Unknown;
        }

        public override void Write(Utf8JsonWriter writer, GrypeSeverity value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// Reads Grype fix states (<c>fixed</c>, <c>not-fixed</c>, <c>wont-fix</c>, <c>unknown</c>); anything else is Unknown.
    /// </summary>
    internal sealed class GrypeFixStateConverter : JsonConverter<GrypeFixState>
    {
        public override GrypeFixState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString()?.ToLowerInvariant() switch
                {
                    "fixed" => GrypeFixState.Fixed,
                    "not-fixed" => GrypeFixState.NotFixed,
                    "wont-fix" => GrypeFixState.WontFix,
                    _ => GrypeFixState.Unknown,
                };
            }

            reader.Skip();
            return GrypeFixState.Unknown;
        }

        public override void Write(Utf8JsonWriter writer, GrypeFixState value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value switch
            {
                GrypeFixState.Fixed => "fixed",
                GrypeFixState.NotFixed => "not-fixed",
                GrypeFixState.WontFix => "wont-fix",
                _ => "unknown",
            });
        }
    }
}
```

`reader.Skip()` on a scalar token (number, `null`) is a no-op; on `{`/`[` it skips the whole value, so a malformed severity never derails the rest of the document.

**File:** `src/Cake.Grype/Json/GrypeReportReader.cs`

```csharp
using System;
using System.IO;
using System.Text.Json;
using Cake.Core;
using Cake.Core.IO;

namespace Cake.Grype.Json
{
    /// <summary>
    /// Reads Grype JSON reports (<c>-o json</c>).
    /// </summary>
    /// <remarks>
    /// The report is deserialized from a stream; fields the model does not map (for example <c>matchDetails</c>,
    /// artifact <c>locations</c> or <c>descriptor.configuration</c>) are skipped, which keeps memory low for large reports.
    /// </remarks>
    public sealed class GrypeReportReader
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new NullAsEmptyListConverterFactory(),
                new GrypeSeverityConverter(),
                new GrypeFixStateConverter(),
            },
        };

        private readonly IFileSystem _fileSystem;
        private readonly ICakeEnvironment _environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeReportReader" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        public GrypeReportReader(IFileSystem fileSystem, ICakeEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(fileSystem);
            ArgumentNullException.ThrowIfNull(environment);

            _fileSystem = fileSystem;
            _environment = environment;
        }

        /// <summary>
        /// Reads a report file.
        /// </summary>
        /// <param name="path">The report file; relative paths are resolved against the working directory.</param>
        /// <returns>The report.</returns>
        public GrypeReport Read(FilePath path)
        {
            ArgumentNullException.ThrowIfNull(path);

            var file = _fileSystem.GetFile(path.MakeAbsolute(_environment));
            if (!file.Exists)
            {
                throw new FileNotFoundException(
                    $"The Grype report '{file.Path.FullPath}' could not be found.",
                    file.Path.FullPath);
            }

            using var stream = file.OpenRead();
            return Parse(stream);
        }

        /// <summary>
        /// Parses a report from a stream of UTF-8 JSON (a byte order mark is allowed).
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The report.</returns>
        public static GrypeReport Parse(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            try
            {
                return JsonSerializer.Deserialize<GrypeReport>(stream, Options) ?? new GrypeReport();
            }
            catch (JsonException exception)
            {
                throw new CakeException("Grype: The JSON report is not valid: " + exception.Message, exception);
            }
        }
    }
}
```

**File:** `src/Cake.Grype/GrypeAliases.ReadJson.cs`

```csharp
using System;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.Grype.Json;

namespace Cake.Grype
{
    public static partial class GrypeAliases
    {
        /// <summary>
        /// Reads a Grype JSON report (written with <c>GrypeOutput.Json(file)</c>) for inspection in C#.
        /// </summary>
        /// <remarks>
        /// <see cref="GrypeSeverity.Unknown"/> means "not assessed yet" (for example a reserved CVE). Such matches have
        /// risk 0 and no EPSS, KEV or CVSS data, so severity, risk and EPSS thresholds skip them; check for
        /// <c>GrypeSeverity.Unknown</c> explicitly if they should block a build.
        /// </remarks>
        /// <param name="context">The context.</param>
        /// <param name="path">The report file.</param>
        /// <returns>The report.</returns>
        /// <example>
        /// <code>
        /// var report = GrypeReadJson("./artifacts/grype.json");
        /// var blocking = report.Matches.Where(m =>
        ///        m.IsKnownExploited
        ///     || m.MaxEpssScore &gt;= 0.5
        ///     || (m.Severity &gt;= GrypeSeverity.Critical &amp;&amp; m.Vulnerability.Fix.State == GrypeFixState.Fixed)
        ///     || m.Risk &gt;= 50).ToList();
        /// if (blocking.Count &gt; 0)
        /// {
        ///     throw new CakeException($"{blocking.Count} blocking vulnerabilities");
        /// }
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Json")]
        public static GrypeReport GrypeReadJson(this ICakeContext context, FilePath path)
        {
            ArgumentNullException.ThrowIfNull(context);

            return new GrypeReportReader(context.FileSystem, context.Environment).Read(path);
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Cake.Grype.sln`
Expected: PASS. If `Should_Read_The_Document_Structure` fails on `Source.Target` with "operation on a disposed JsonDocument": System.Text.Json deserializes `JsonElement` properties as a standalone clone, so this should not happen; if it does, change `Target`'s getter-backed field to store `element.Clone()` via a private `[JsonInclude]` property and re-run.

- [ ] **Step 6: Commit**

```bash
git add src tests
git commit -m "feat: add Grype JSON report model, reader and GrypeReadJson"
```

---

### Task 5: `db update`, `db import`, `db delete`

**Files:**
- Create: `src/Cake.Grype/Db/GrypeDbUpdateSettings.cs`, `GrypeDbUpdater.cs`, `GrypeDbImportSettings.cs`, `GrypeDbImporter.cs`, `GrypeDbDeleteSettings.cs`, `GrypeDbDeleter.cs`
- Create: `src/Cake.Grype/GrypeAliases.Db.cs`
- Create: `tests/Cake.Grype.Tests/Fixtures/DbFixtures.cs`
- Test: `tests/Cake.Grype.Tests/GrypeDbCommandTests.cs`

**Interfaces:**
- Consumes: `GrypeTool<TSettings>` (`CreateArgumentBuilder`, `MakeAbsolute`), `GrypeSettings`.
- Produces (namespace `Cake.Grype.Db`): `GrypeDbUpdateSettings`, `GrypeDbImportSettings`, `GrypeDbDeleteSettings` (all `: GrypeSettings`, no extra properties); `GrypeDbUpdater.Update(GrypeDbUpdateSettings)`; `GrypeDbImporter.Import(FilePath, GrypeDbImportSettings)` and `Import(Uri, GrypeDbImportSettings)`; `GrypeDbDeleter.Delete(GrypeDbDeleteSettings)`. Aliases (namespace `Cake.Grype`, file `GrypeAliases.Db.cs`): `GrypeDbUpdate([s])`, `GrypeDbImport(FilePath[, s])`, `GrypeDbImport(Uri[, s])`, `GrypeDbDelete([s])`.

- [ ] **Step 1: Write the fixtures and the failing tests**

**File:** `tests/Cake.Grype.Tests/Fixtures/DbFixtures.cs`

```csharp
using Cake.Core.IO;
using Cake.Grype.Db;

namespace Cake.Grype.Tests.Fixtures;

internal sealed class DbUpdateFixture : GrypeFixture<GrypeDbUpdateSettings>
{
    protected override void RunTool()
    {
        new GrypeDbUpdater(FileSystem, Environment, ProcessRunner, Tools).Update(Settings);
    }
}

internal sealed class DbDeleteFixture : GrypeFixture<GrypeDbDeleteSettings>
{
    protected override void RunTool()
    {
        new GrypeDbDeleter(FileSystem, Environment, ProcessRunner, Tools).Delete(Settings);
    }
}

internal sealed class DbImportFixture : GrypeFixture<GrypeDbImportSettings>
{
    public FilePath Archive { get; set; }

    public Uri Url { get; set; }

    protected override void RunTool()
    {
        var importer = new GrypeDbImporter(FileSystem, Environment, ProcessRunner, Tools);
        if (Url != null)
        {
            importer.Import(Url, Settings);
        }
        else
        {
            importer.Import(Archive, Settings);
        }
    }
}
```

**File:** `tests/Cake.Grype.Tests/GrypeDbCommandTests.cs`

```csharp
using Cake.Core.IO;
using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeDbCommandTests
{
    [Fact]
    public void Update_Should_Run_Db_Update()
    {
        Assert.Equal("db update", new DbUpdateFixture().Run().Args);
    }

    [Fact]
    public void Update_Should_Add_Global_Flags_After_The_Command()
    {
        var fixture = new DbUpdateFixture();
        fixture.Settings.Quiet = true;
        fixture.Settings.ConfigFiles.Add("grype.yaml");

        Assert.Equal("db update -c \"/Working/grype.yaml\" -q", fixture.Run().Args);
    }

    [Fact]
    public void Update_Should_Throw_If_Settings_Are_Null()
    {
        var result = Record.Exception(() => new DbUpdateFixture { Settings = null }.Run());

        Assertions.IsArgumentNullException(result, "settings");
    }

    [Fact]
    public void Update_Should_Throw_On_Failure()
    {
        var fixture = new DbUpdateFixture();
        fixture.GivenProcessExitsWithCode(1);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 1).");
    }

    [Fact]
    public void Delete_Should_Run_Db_Delete()
    {
        Assert.Equal("db delete", new DbDeleteFixture().Run().Args);
    }

    [Fact]
    public void Import_Should_Pass_An_Absolute_Archive_Path()
    {
        var fixture = new DbImportFixture { Archive = "db/vulnerability-db.tar.zst" };

        Assert.Equal("db import \"/Working/db/vulnerability-db.tar.zst\"", fixture.Run().Args);
    }

    [Fact]
    public void Import_Should_Pass_A_Url_Unchanged()
    {
        var url = "https://grype.anchore.io/databases/v6/vulnerability-db_v6.1.9.tar.zst?checksum=sha256%3A972de5";
        var fixture = new DbImportFixture { Url = new Uri(url) };

        Assert.Equal($"db import \"{url}\"", fixture.Run().Args);
    }

    [Fact]
    public void Import_Should_Throw_If_The_Archive_Is_Null()
    {
        var result = Record.Exception(() => new DbImportFixture { Archive = null }.Run());

        Assertions.IsArgumentNullException(result, "archive");
    }

    [Fact]
    public void Import_Should_Reject_A_Relative_Url()
    {
        var result = Record.Exception(() => new DbImportFixture { Url = new Uri("db.tar.zst", UriKind.Relative) }.Run());

        Assertions.IsArgumentException(result, "url");
    }

    [Fact]
    public void Aliases_Should_Run_The_Db_Commands()
    {
        var alias = new AliasContext();

        alias.Context.GrypeDbUpdate();
        alias.Context.GrypeDbImport(new FilePath("db.tar.zst"));
        alias.Context.GrypeDbImport(new Uri("https://example.com/db.tar.zst"));
        alias.Context.GrypeDbDelete();

        Assert.Equal(
            new[]
            {
                "db update",
                "db import \"/Working/db.tar.zst\"",
                "db import \"https://example.com/db.tar.zst\"",
                "db delete",
            },
            alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Import_Alias_Should_Accept_A_String_As_A_File_Path()
    {
        var alias = new AliasContext();

        alias.Context.GrypeDbImport("db.tar.zst");

        Assert.Equal(new[] { "db import \"/Working/db.tar.zst\"" }, alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Aliases_Should_Throw_If_Context_Is_Null()
    {
        Assertions.IsArgumentNullException(Record.Exception(() => GrypeAliases.GrypeDbUpdate(null)), "context");
        Assertions.IsArgumentNullException(Record.Exception(() => GrypeAliases.GrypeDbDelete(null)), "context");
        Assertions.IsArgumentNullException(
            Record.Exception(() => GrypeAliases.GrypeDbImport(null, new FilePath("db.tar.zst"))),
            "context");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test --project tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj -f net10.0 --filter-class "Cake.Grype.Tests.GrypeDbCommandTests"`
Expected: build FAILS — `Cake.Grype.Db` types do not exist.

- [ ] **Step 3: Implement settings and runners**

**File:** `src/Cake.Grype/Db/GrypeDbUpdateSettings.cs`

```csharp
namespace Cake.Grype.Db
{
    /// <summary>
    /// Contains the settings for <c>grype db update</c>.
    /// </summary>
    public sealed class GrypeDbUpdateSettings : GrypeSettings
    {
    }
}
```

**File:** `src/Cake.Grype/Db/GrypeDbImportSettings.cs`

```csharp
namespace Cake.Grype.Db
{
    /// <summary>
    /// Contains the settings for <c>grype db import</c>.
    /// </summary>
    public sealed class GrypeDbImportSettings : GrypeSettings
    {
    }
}
```

**File:** `src/Cake.Grype/Db/GrypeDbDeleteSettings.cs`

```csharp
namespace Cake.Grype.Db
{
    /// <summary>
    /// Contains the settings for <c>grype db delete</c>.
    /// </summary>
    public sealed class GrypeDbDeleteSettings : GrypeSettings
    {
    }
}
```

**File:** `src/Cake.Grype/Db/GrypeDbUpdater.cs`

```csharp
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Db
{
    /// <summary>
    /// Runs <c>grype db update</c>: downloads and installs the latest vulnerability database.
    /// </summary>
    public sealed class GrypeDbUpdater : GrypeTool<GrypeDbUpdateSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeDbUpdater" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeDbUpdater(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Updates the vulnerability database.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public void Update(GrypeDbUpdateSettings settings)
        {
            Run(settings, CreateArgumentBuilder(settings, "db", "update"));
        }
    }
}
```

**File:** `src/Cake.Grype/Db/GrypeDbDeleter.cs`

```csharp
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Db
{
    /// <summary>
    /// Runs <c>grype db delete</c>: deletes the local vulnerability database.
    /// </summary>
    public sealed class GrypeDbDeleter : GrypeTool<GrypeDbDeleteSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeDbDeleter" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeDbDeleter(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Deletes the vulnerability database.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public void Delete(GrypeDbDeleteSettings settings)
        {
            Run(settings, CreateArgumentBuilder(settings, "db", "delete"));
        }
    }
}
```

**File:** `src/Cake.Grype/Db/GrypeDbImporter.cs`

```csharp
using System;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Db
{
    /// <summary>
    /// Runs <c>grype db import</c>: imports a vulnerability database archive from a file or URL.
    /// </summary>
    public sealed class GrypeDbImporter : GrypeTool<GrypeDbImportSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeDbImporter" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeDbImporter(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Imports a database archive from disk.
        /// </summary>
        /// <param name="archive">The archive; relative paths are resolved against the working directory.</param>
        /// <param name="settings">The settings.</param>
        public void Import(FilePath archive, GrypeDbImportSettings settings)
        {
            ArgumentNullException.ThrowIfNull(archive);

            var arguments = CreateArgumentBuilder(settings, "db", "import");
            arguments.AppendQuoted(MakeAbsolute(archive, settings).FullPath);
            Run(settings, arguments);
        }

        /// <summary>
        /// Imports a database archive from a URL. A <c>checksum=sha256:…</c> query parameter is verified by Grype.
        /// </summary>
        /// <param name="url">The absolute archive URL.</param>
        /// <param name="settings">The settings.</param>
        public void Import(Uri url, GrypeDbImportSettings settings)
        {
            ArgumentNullException.ThrowIfNull(url);
            if (!url.IsAbsoluteUri)
            {
                throw new ArgumentException("The database URL must be absolute.", nameof(url));
            }

            var arguments = CreateArgumentBuilder(settings, "db", "import");
            arguments.AppendQuoted(url.OriginalString);
            Run(settings, arguments);
        }
    }
}
```

Note: `Import(FilePath, …)` checks `archive` before building arguments, so a null archive reports `archive`, not `settings`.

- [ ] **Step 4: Implement the aliases**

**File:** `src/Cake.Grype/GrypeAliases.Db.cs`

```csharp
using System;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.Grype.Db;

namespace Cake.Grype
{
    public static partial class GrypeAliases
    {
        /// <summary>
        /// Downloads and installs the latest Grype vulnerability database.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <example>
        /// <code>
        /// GrypeDbUpdate();
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbUpdate(this ICakeContext context)
        {
            context.GrypeDbUpdate(null);
        }

        /// <summary>
        /// Downloads and installs the latest Grype vulnerability database using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbUpdate(this ICakeContext context, GrypeDbUpdateSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbUpdateSettings();
            new GrypeDbUpdater(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Update(settings);
        }

        /// <summary>
        /// Imports a Grype vulnerability database archive from disk, for offline or air-gapped builds.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="archive">The archive, for example downloaded from <c>https://grype.anchore.io/databases</c>.</param>
        /// <example>
        /// <code>
        /// GrypeDbImport("./cache/vulnerability-db.tar.zst");
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbImport(this ICakeContext context, FilePath archive)
        {
            context.GrypeDbImport(archive, null);
        }

        /// <summary>
        /// Imports a Grype vulnerability database archive from disk using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="archive">The archive.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbImport(this ICakeContext context, FilePath archive, GrypeDbImportSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbImportSettings();
            new GrypeDbImporter(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Import(archive, settings);
        }

        /// <summary>
        /// Imports a Grype vulnerability database archive from a URL. A <c>checksum=sha256:…</c> query parameter is
        /// verified by Grype.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="url">The absolute archive URL.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbImport(this ICakeContext context, Uri url)
        {
            context.GrypeDbImport(url, null);
        }

        /// <summary>
        /// Imports a Grype vulnerability database archive from a URL using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="url">The absolute archive URL.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbImport(this ICakeContext context, Uri url, GrypeDbImportSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbImportSettings();
            new GrypeDbImporter(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Import(url, settings);
        }

        /// <summary>
        /// Deletes the local Grype vulnerability database, for example to start a test from a clean cache.
        /// </summary>
        /// <param name="context">The context.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbDelete(this ICakeContext context)
        {
            context.GrypeDbDelete(null);
        }

        /// <summary>
        /// Deletes the local Grype vulnerability database using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbDelete(this ICakeContext context, GrypeDbDeleteSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbDeleteSettings();
            new GrypeDbDeleter(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Delete(settings);
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Cake.Grype.sln`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src tests
git commit -m "feat: add Grype db update, import and delete"
```

---

### Task 6: `db status` and `db check`

**Files:**
- Create: `src/Cake.Grype/Db/GrypeDbStatus.cs`, `GrypeDbStatusSettings.cs`, `GrypeDbStatusReader.cs`, `GrypeDbCheckResult.cs`, `GrypeDbCheckSettings.cs`, `GrypeDbChecker.cs`
- Modify: `src/Cake.Grype/GrypeAliases.Db.cs` (append four alias methods inside the class)
- Modify: `tests/Cake.Grype.Tests/Fixtures/DbFixtures.cs` (append two fixtures)
- Test: `tests/Cake.Grype.Tests/GrypeDbStatusReaderTests.cs`, `tests/Cake.Grype.Tests/GrypeDbCheckerTests.cs`

**Interfaces:**
- Consumes: `GrypeTool<TSettings>.RunAndReadJson<T>`, `AcceptsExitCode`, `TryParseJson<T>`.
- Produces (namespace `Cake.Grype.Db`):
  - `GrypeDbStatus { string SchemaVersion; string From; DateTimeOffset? Built; string Path; bool Valid; string Error; }`
  - `GrypeDbCheckResult { bool UpdateAvailable; GrypeDbDescription Current; GrypeDbDescription Candidate; }`, `GrypeDbDescription { string SchemaVersion; DateTimeOffset? Built; string Path; string Checksum; }`
  - `GrypeDbStatusSettings`, `GrypeDbCheckSettings`; `GrypeDbStatusReader.Read(GrypeDbStatusSettings)`, `GrypeDbChecker.Check(GrypeDbCheckSettings)`
  - Aliases `GrypeDbStatus([s]) : GrypeDbStatus`, `GrypeDbCheck([s]) : GrypeDbCheckResult`

- [ ] **Step 1: Write the fixtures and the failing tests**

Append to **`tests/Cake.Grype.Tests/Fixtures/DbFixtures.cs`**:

```csharp
internal sealed class DbStatusFixture : GrypeFixture<GrypeDbStatusSettings>
{
    public DbStatusFixture()
    {
        GivenStandardOutput("{}");
    }

    public GrypeDbStatus Result { get; private set; }

    protected override void RunTool()
    {
        Result = new GrypeDbStatusReader(FileSystem, Environment, ProcessRunner, Tools).Read(Settings);
    }
}

internal sealed class DbCheckFixture : GrypeFixture<GrypeDbCheckSettings>
{
    public DbCheckFixture()
    {
        GivenStandardOutput("{}");
    }

    public GrypeDbCheckResult Result { get; private set; }

    protected override void RunTool()
    {
        Result = new GrypeDbChecker(FileSystem, Environment, ProcessRunner, Tools).Check(Settings);
    }
}
```

**File:** `tests/Cake.Grype.Tests/GrypeDbStatusReaderTests.cs`

```csharp
using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeDbStatusReaderTests
{
    // Captured from `grype db status -o json` (Grype 0.119.0), exit code 0.
    private const string ValidStatus = """
        {
         "schemaVersion": "v6.1.9",
         "from": "https://grype.anchore.io/databases/v6/vulnerability-db_v6.1.9_2026-09-22T00:33:02Z_1790058641.tar.zst?checksum=sha256%3A972de542534d3461cc3c3849a37a3f917b823de3b9c367e289db0d163519b42b",
         "built": "2026-09-22T06:30:41Z",
         "path": "C:\\Users\\me\\AppData\\Local\\cache\\grype\\db\\6\\vulnerability.db",
         "valid": true
        }
        """;

    // Captured after `grype db delete`: exit code 1, but valid JSON on stdout.
    private const string MissingStatus = """
        {
         "schemaVersion": "",
         "path": "C:\\Users\\me\\AppData\\Local\\cache\\grype\\db\\6\\vulnerability.db",
         "valid": false,
         "error": "database does not exist"
        }
        """;

    [Fact]
    public void Should_Request_Quiet_Json_Output()
    {
        Assert.Equal("db status -q -o json", new DbStatusFixture().Run().Args);
    }

    [Fact]
    public void Should_Throw_If_Settings_Are_Null()
    {
        var result = Record.Exception(() => new DbStatusFixture { Settings = null }.Run());

        Assertions.IsArgumentNullException(result, "settings");
    }

    [Fact]
    public void Should_Parse_A_Valid_Database()
    {
        var fixture = new DbStatusFixture();
        fixture.GivenStandardOutput(ValidStatus);

        fixture.Run();

        Assert.True(fixture.Result.Valid);
        Assert.Equal("v6.1.9", fixture.Result.SchemaVersion);
        Assert.Equal(new DateTimeOffset(2026, 9, 22, 6, 30, 41, TimeSpan.Zero), fixture.Result.Built);
        Assert.StartsWith("https://grype.anchore.io/databases/v6/", fixture.Result.From);
        Assert.EndsWith("vulnerability.db", fixture.Result.Path);
        Assert.Null(fixture.Result.Error);
    }

    [Fact]
    public void Should_Return_An_Invalid_Status_When_No_Database_Is_Installed()
    {
        var fixture = new DbStatusFixture();
        fixture.GivenStandardOutput(MissingStatus);
        fixture.GivenProcessExitsWithCode(1);

        fixture.Run();

        Assert.False(fixture.Result.Valid);
        Assert.Equal("database does not exist", fixture.Result.Error);
        Assert.Null(fixture.Result.Built);
    }

    [Fact]
    public void Should_Throw_On_Exit_Code_1_Without_Status_Json()
    {
        var fixture = new DbStatusFixture();
        fixture.GivenStandardOutput("failed to load config");
        fixture.GivenProcessExitsWithCode(1);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 1).");
    }

    [Fact]
    public void Should_Throw_On_Exit_Code_1_With_A_Valid_Status()
    {
        var fixture = new DbStatusFixture();
        fixture.GivenStandardOutput(ValidStatus);
        fixture.GivenProcessExitsWithCode(1);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 1).");
    }

    [Fact]
    public void Should_Throw_On_Other_Exit_Codes_Even_With_An_Invalid_Status()
    {
        var fixture = new DbStatusFixture();
        fixture.GivenStandardOutput(MissingStatus);
        fixture.GivenProcessExitsWithCode(2);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 2).");
    }

    [Fact]
    public void Alias_Should_Read_The_Status()
    {
        var alias = new AliasContext();
        alias.ProcessRunner.Process.SetStandardOutput(ValidStatus.Split('\n'));

        var status = alias.Context.GrypeDbStatus();

        Assert.True(status.Valid);
        Assert.Equal(new[] { "db status -q -o json" }, alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Alias_Should_Throw_If_Context_Is_Null()
    {
        var result = Record.Exception(() => GrypeAliases.GrypeDbStatus(null));

        Assertions.IsArgumentNullException(result, "context");
    }
}
```

**File:** `tests/Cake.Grype.Tests/GrypeDbCheckerTests.cs`

```csharp
using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeDbCheckerTests
{
    // Captured from `grype db check -o json` (Grype 0.119.0) with a current database, exit code 0.
    private const string Current = """
        {
         "currentDB": {
          "schemaVersion": "v6.1.9",
          "built": "2026-09-22T06:30:41Z"
         },
         "candidateDB": null,
         "updateAvailable": false
        }
        """;

    // Captured after `grype db delete`: exit code 100.
    private const string NoDatabase = """
        {
         "currentDB": null,
         "candidateDB": {
          "schemaVersion": "v6.1.9",
          "built": "2026-09-22T06:30:41Z",
          "path": "vulnerability-db_v6.1.9_2026-09-22T00:33:02Z_1790058641.tar.zst",
          "checksum": "sha256:972de542534d3461cc3c3849a37a3f917b823de3b9c367e289db0d163519b42b"
         },
         "updateAvailable": true
        }
        """;

    [Fact]
    public void Should_Request_Quiet_Json_Output()
    {
        Assert.Equal("db check -q -o json", new DbCheckFixture().Run().Args);
    }

    [Fact]
    public void Should_Parse_A_Current_Database()
    {
        var fixture = new DbCheckFixture();
        fixture.GivenStandardOutput(Current);

        fixture.Run();

        Assert.False(fixture.Result.UpdateAvailable);
        Assert.Null(fixture.Result.Candidate);
        Assert.Equal("v6.1.9", fixture.Result.Current.SchemaVersion);
        Assert.Equal(new DateTimeOffset(2026, 9, 22, 6, 30, 41, TimeSpan.Zero), fixture.Result.Current.Built);
    }

    [Fact]
    public void Should_Accept_Exit_Code_100_When_An_Update_Is_Available()
    {
        var fixture = new DbCheckFixture();
        fixture.GivenStandardOutput(NoDatabase);
        fixture.GivenProcessExitsWithCode(100);

        fixture.Run();

        Assert.True(fixture.Result.UpdateAvailable);
        Assert.Null(fixture.Result.Current);
        Assert.Equal("v6.1.9", fixture.Result.Candidate.SchemaVersion);
        Assert.Equal("vulnerability-db_v6.1.9_2026-09-22T00:33:02Z_1790058641.tar.zst", fixture.Result.Candidate.Path);
        Assert.Equal("sha256:972de542534d3461cc3c3849a37a3f917b823de3b9c367e289db0d163519b42b", fixture.Result.Candidate.Checksum);
    }

    [Fact]
    public void Should_Throw_On_Other_Exit_Codes()
    {
        var fixture = new DbCheckFixture();
        fixture.GivenStandardOutput(NoDatabase);
        fixture.GivenProcessExitsWithCode(1);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 1).");
    }

    [Fact]
    public void Should_Throw_On_Exit_Code_100_Without_Json()
    {
        var fixture = new DbCheckFixture();
        fixture.GivenStandardOutput(string.Empty);
        fixture.GivenProcessExitsWithCode(100);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: The output is not valid JSON: (no output)");
    }

    [Fact]
    public void Alias_Should_Check_The_Database()
    {
        var alias = new AliasContext();
        alias.ProcessRunner.Process.SetStandardOutput(NoDatabase.Split('\n'));
        alias.ProcessRunner.Process.SetExitCode(100);

        var result = alias.Context.GrypeDbCheck();

        Assert.True(result.UpdateAvailable);
        Assert.Equal(new[] { "db check -q -o json" }, alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Alias_Should_Throw_If_Context_Is_Null()
    {
        var result = Record.Exception(() => GrypeAliases.GrypeDbCheck(null));

        Assertions.IsArgumentNullException(result, "context");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test --project tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj -f net10.0 --filter-class "Cake.Grype.Tests.GrypeDbCheckerTests"`
Expected: build FAILS — status/check types do not exist.

- [ ] **Step 3: Implement result types, settings and runners**

**File:** `src/Cake.Grype/Db/GrypeDbStatus.cs`

```csharp
using System;

namespace Cake.Grype.Db
{
    /// <summary>
    /// The local vulnerability database status reported by <c>grype db status -o json</c>.
    /// </summary>
    public sealed class GrypeDbStatus
    {
        /// <summary>Gets the database schema version, for example <c>v6.1.9</c>; empty when no database exists.</summary>
        public string SchemaVersion { get; init; }

        /// <summary>Gets the URL the database was downloaded from.</summary>
        public string From { get; init; }

        /// <summary>Gets when the database was built.</summary>
        public DateTimeOffset? Built { get; init; }

        /// <summary>Gets the database file path.</summary>
        public string Path { get; init; }

        /// <summary>Gets a value indicating whether the database is present and valid.</summary>
        public bool Valid { get; init; }

        /// <summary>Gets the reason the database is not valid, for example <c>database does not exist</c>.</summary>
        public string Error { get; init; }
    }
}
```

**File:** `src/Cake.Grype/Db/GrypeDbCheckResult.cs`

```csharp
using System;
using System.Text.Json.Serialization;

namespace Cake.Grype.Db
{
    /// <summary>
    /// The result of <c>grype db check -o json</c>.
    /// </summary>
    public sealed class GrypeDbCheckResult
    {
        /// <summary>Gets a value indicating whether a newer database is available.</summary>
        public bool UpdateAvailable { get; init; }

        /// <summary>Gets the installed database, or <c>null</c> if none is installed.</summary>
        [JsonPropertyName("currentDB")]
        public GrypeDbDescription Current { get; init; }

        /// <summary>Gets the available newer database, or <c>null</c> if the installed one is current.</summary>
        [JsonPropertyName("candidateDB")]
        public GrypeDbDescription Candidate { get; init; }
    }

    /// <summary>
    /// A vulnerability database version.
    /// </summary>
    public sealed class GrypeDbDescription
    {
        /// <summary>Gets the schema version, for example <c>v6.1.9</c>.</summary>
        public string SchemaVersion { get; init; }

        /// <summary>Gets when the database was built.</summary>
        public DateTimeOffset? Built { get; init; }

        /// <summary>Gets the archive name (candidate databases only).</summary>
        public string Path { get; init; }

        /// <summary>Gets the archive checksum, for example <c>sha256:…</c> (candidate databases only).</summary>
        public string Checksum { get; init; }
    }
}
```

**File:** `src/Cake.Grype/Db/GrypeDbStatusSettings.cs`

```csharp
namespace Cake.Grype.Db
{
    /// <summary>
    /// Contains the settings for <c>grype db status</c>.
    /// </summary>
    public sealed class GrypeDbStatusSettings : GrypeSettings
    {
    }
}
```

**File:** `src/Cake.Grype/Db/GrypeDbCheckSettings.cs`

```csharp
namespace Cake.Grype.Db
{
    /// <summary>
    /// Contains the settings for <c>grype db check</c>.
    /// </summary>
    public sealed class GrypeDbCheckSettings : GrypeSettings
    {
    }
}
```

**File:** `src/Cake.Grype/Db/GrypeDbStatusReader.cs`

```csharp
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Db
{
    /// <summary>
    /// Runs <c>grype db status</c>.
    /// </summary>
    public sealed class GrypeDbStatusReader : GrypeTool<GrypeDbStatusSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeDbStatusReader" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeDbStatusReader(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Reads the database status. A missing or invalid database is returned with <see cref="GrypeDbStatus.Valid"/>
        /// <c>false</c> instead of throwing.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The status.</returns>
        public GrypeDbStatus Read(GrypeDbStatusSettings settings)
        {
            return RunAndReadJson<GrypeDbStatus>(settings, "db", "status");
        }

        /// <inheritdoc />
        protected override bool AcceptsExitCode(int exitCode, string standardOutput)
        {
            // Grype exits 1 when the database is missing or invalid, but still prints the status.
            return exitCode == 1
                && TryParseJson(standardOutput, out GrypeDbStatus status)
                && !status.Valid;
        }
    }
}
```

**File:** `src/Cake.Grype/Db/GrypeDbChecker.cs`

```csharp
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Db
{
    /// <summary>
    /// Runs <c>grype db check</c>.
    /// </summary>
    public sealed class GrypeDbChecker : GrypeTool<GrypeDbCheckSettings>
    {
        private const int UpdateAvailableExitCode = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeDbChecker" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeDbChecker(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Checks whether a database update is available. An available update is reported through
        /// <see cref="GrypeDbCheckResult.UpdateAvailable"/>, not an exception.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The result.</returns>
        public GrypeDbCheckResult Check(GrypeDbCheckSettings settings)
        {
            return RunAndReadJson<GrypeDbCheckResult>(settings, "db", "check");
        }

        /// <inheritdoc />
        protected override bool AcceptsExitCode(int exitCode, string standardOutput)
        {
            return exitCode == UpdateAvailableExitCode;
        }
    }
}
```

- [ ] **Step 4: Add the aliases**

Append inside the `GrypeAliases` class in **`src/Cake.Grype/GrypeAliases.Db.cs`** (after `GrypeDbDelete(this ICakeContext, GrypeDbDeleteSettings)`):

```csharp
        /// <summary>
        /// Gets the status of the local Grype vulnerability database. A missing database returns
        /// <c>Valid == false</c> with an <c>Error</c> instead of throwing.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The status.</returns>
        /// <example>
        /// <code>
        /// var status = GrypeDbStatus();
        /// if (!status.Valid)
        /// {
        ///     GrypeDbUpdate();
        /// }
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static GrypeDbStatus GrypeDbStatus(this ICakeContext context)
        {
            return context.GrypeDbStatus(null);
        }

        /// <summary>
        /// Gets the status of the local Grype vulnerability database using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The status.</returns>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static GrypeDbStatus GrypeDbStatus(this ICakeContext context, GrypeDbStatusSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbStatusSettings();
            return new GrypeDbStatusReader(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Read(settings);
        }

        /// <summary>
        /// Checks whether a newer Grype vulnerability database is available.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The result.</returns>
        /// <example>
        /// <code>
        /// if (GrypeDbCheck().UpdateAvailable)
        /// {
        ///     GrypeDbUpdate();
        /// }
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static GrypeDbCheckResult GrypeDbCheck(this ICakeContext context)
        {
            return context.GrypeDbCheck(null);
        }

        /// <summary>
        /// Checks whether a newer Grype vulnerability database is available using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The result.</returns>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static GrypeDbCheckResult GrypeDbCheck(this ICakeContext context, GrypeDbCheckSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbCheckSettings();
            return new GrypeDbChecker(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Check(settings);
        }
```

`GrypeDbStatus GrypeDbStatus(...)` returns a type with the alias's own name; that is legal C# (return types are looked up as types, not members) and mirrors Cake's `GitVersion GitVersion()`. Inside `GrypeAliases` the file already has `using Cake.Grype.Db;`, so `GrypeDbStatus` in type position resolves to `Cake.Grype.Db.GrypeDbStatus`.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Cake.Grype.sln`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src tests
git commit -m "feat: add Grype db status and db check with typed results"
```

---

### Task 7: Architecture guard, README, package and end-to-end verification

**Files:**
- Create: `tests/Cake.Grype.Tests/ArchitectureTests.cs`
- Modify: `README.md` (replace entirely)

**Interfaces:**
- Consumes: everything above.
- Produces: the packed `Cake.Grype.0.1.0.nupkg` and a verified end-to-end run.

- [ ] **Step 1: Write the architecture test**

**File:** `tests/Cake.Grype.Tests/ArchitectureTests.cs`

```csharp
using System.Text.RegularExpressions;

namespace Cake.Grype.Tests;

public sealed class ArchitectureTests
{
    private static DirectoryInfo FindJsonSourceDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Cake.Grype.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return new DirectoryInfo(Path.Combine(directory.FullName, "src", "Cake.Grype", "Json"));
    }

    private static string StripComments(string source)
    {
        return Regex.Replace(source, @"//[^\n]*|/\*.*?\*/", string.Empty, RegexOptions.Singleline);
    }

    internal static bool ReferencesRunnerNamespaces(string source)
    {
        var code = StripComments(source);
        return Regex.IsMatch(code, @"\bCake\.Grype\.(Scan|Db)\b")
            || Regex.IsMatch(code, @"(?<![\w.])(Scan|Db)\s*\.");
    }

    [Fact]
    public void Json_Namespace_Does_Not_Reference_Scan_Or_Db()
    {
        var files = FindJsonSourceDirectory().GetFiles("*.cs");
        Assert.NotEmpty(files);

        var offenders = files.Where(file => ReferencesRunnerNamespaces(File.ReadAllText(file.FullName))).Select(file => file.Name);

        Assert.Empty(offenders);
    }

    [Theory]
    [InlineData("using Cake.Grype.Scan;", true)]
    [InlineData("var x = new Db.GrypeDbStatus();", true)]
    [InlineData("Cake.Grype.Db.GrypeDbChecker c;", true)]
    [InlineData("// see Cake.Grype.Scan for the runner", false)]
    [InlineData("/// <see cref=\"Cake.Grype.Db.GrypeDbStatus\"/>", false)]
    [InlineData("var d = GrypeDbStatus.Valid;", false)]
    [InlineData("using Cake.Grype;", false)]
    public void Detects_References_To_Runner_Namespaces(string source, bool expected)
    {
        Assert.Equal(expected, ReferencesRunnerNamespaces(source));
    }
}
```

Run: `dotnet test --project tests/Cake.Grype.Tests/Cake.Grype.Tests.csproj -f net10.0 --filter-class "Cake.Grype.Tests.ArchitectureTests"`
Expected: PASS (the Json namespace was written without such references; the detector cases prove the check can fail).

- [ ] **Step 2: Write the README**

**File:** `README.md`

````markdown
# Cake.Grype

A [Cake](https://cakebuild.net) add-in for [Grype](https://github.com/anchore/grype) by Anchore: scan SBOMs, directories and container images for known vulnerabilities, manage Grype's vulnerability database, and read Grype's JSON report to gate a build in C#.

## Installation

```csharp
#addin nuget:?package=Cake.Grype
```

Grype itself must be installed and on `PATH` (or set `ToolPath` in any settings object):

| Platform | Install |
|---|---|
| Windows | `winget install Anchore.Grype` |
| macOS | `brew install grype` |
| Linux / CI | `curl -sSfL https://get.anchore.io/grype \| sudo sh -s -- -b /usr/local/bin` |

Tested with Grype 0.119.0.

## Scan an SBOM

Print the table in the terminal **and** write a JSON report as a CI artifact, in one run:

```csharp
GrypeScanSbom("./artifacts/bom.cdx.json", new GrypeScanSettings
{
    Outputs = { GrypeOutput.Table(), GrypeOutput.Json("./artifacts/grype.json") },
    SortBy = GrypeSortBy.Risk,
});
```

Output files are written with absolute paths, missing directories are created, and existing files are deleted before the scan so a failed run never leaves a stale report behind. Other formats: `GrypeOutput.Sarif(file)`, `CycloneDx(file)`, `CycloneDxJson(file)`, `Template(file)` (with `Template = "report.tmpl"`).

### Other sources

```csharp
GrypeScanDirectory("./src");
GrypeScanFile("./bin/app.jar");
GrypeScanImage("myorg/api:1.2.3");        // Grype's default lookup (Docker daemon first)
GrypeScanRegistry("myorg/api:1.2.3");     // no container runtime needed

GrypeScan(GrypeSource.OciArchive("./image.tar"));
GrypeScan(GrypeSource.Purl("pkg:nuget/Newtonsoft.Json@12.0.1"));
GrypeScan("registry:alpine:3.20");         // any Grype source string, passed unchanged

// several sources, one loop
var sources = new List<GrypeSource>
{
    GrypeSource.Sbom("./artifacts/api.cdx.json"),
    GrypeSource.Sbom("./artifacts/web.cdx.json"),
};
foreach (var source in sources)
{
    GrypeScan(source, settings);
}
```

## Fail the build

### Let Grype decide

```csharp
GrypeScanSbom("./artifacts/bom.cdx.json", new GrypeScanSettings
{
    Outputs = { GrypeOutput.Table(), GrypeOutput.Json("./artifacts/grype.json") },
    FailOn = GrypeSeverity.High,
});
```

Grype writes all outputs, then exits with code 2, which throws a `CakeException`. To keep going and decide yourself, accept the exit code:

```csharp
HandleExitCode = code => code is 0 or 2,
```

### Decide in C#

```csharp
var report = GrypeReadJson("./artifacts/grype.json");

var blocking = report.Matches.Where(m =>
       m.IsKnownExploited                                      // KEV: exploitation observed
    || m.MaxEpssScore >= 0.5                                   // EPSS: likely to be exploited
    || (m.Severity >= GrypeSeverity.Critical                   // severity/CVSS: potential impact...
        && m.Vulnerability.Fix.State == GrypeFixState.Fixed)   // ...and a fix exists
    || m.Risk >= 50)                                           // Grype's combined risk score
    .ToList();

foreach (var m in blocking)
{
    Error("{0} {1} {2} ({3}, risk {4:0.0})", m.Artifact.Name, m.Artifact.Version, m.Vulnerability.Id, m.Severity, m.Risk);
}

if (blocking.Count > 0)
{
    throw new CakeException($"{blocking.Count} blocking vulnerabilities");
}
```

| Signal | Meaning | Model |
|---|---|---|
| KEV | Known to have been exploited | `m.IsKnownExploited`, `m.Vulnerability.KnownExploited` |
| EPSS | Likely to be exploited, according to the model | `m.MaxEpssScore`, `m.MaxEpssPercentile`, `m.Vulnerability.Epss` |
| CVSS / severity | Potential technical impact, not whether exploitation is occurring | `m.Severity`, `m.MaxCvssBaseScore`, `m.Vulnerability.Cvss` |
| Grype risk | Combined prioritization based on severity, EPSS and KEV | `m.Risk` |

The `m.*` helpers also look at `RelatedVulnerabilities`: a distro advisory (for example Debian's) often has no CVSS itself, only the related NVD record does.

Also available: `report.CountBySeverity()`, `report.AtOrAbove(GrypeSeverity.High)`, `report.IgnoredMatches` (with the ignore rules that applied), `report.Distro`, `report.Descriptor`.

#### `Unknown` severity

`GrypeSeverity.Unknown` means **not assessed yet** — typically a reserved CVE without published analysis. Such matches have risk 0 and no EPSS, KEV or CVSS data, so severity, risk and EPSS thresholds skip them, and so does Grype's own `--fail-on`. If unassessed findings should block a build, say so explicitly:

```csharp
var unassessed = report.Matches.Where(m => m.Severity == GrypeSeverity.Unknown).ToList();
```

## Vulnerability database

```csharp
var version = GrypeVersion();                  // version.Version, version.SupportedDbSchema
var status  = GrypeDbStatus();                 // status.Valid, status.Built, status.SchemaVersion, status.Error
var check   = GrypeDbCheck();                  // check.UpdateAvailable, check.Current, check.Candidate
GrypeDbUpdate();
GrypeDbImport("./cache/vulnerability-db.tar.zst");                   // offline / air-gapped
GrypeDbImport(new Uri("https://grype.anchore.io/databases/...?checksum=sha256:..."));
GrypeDbDelete();                               // start from a clean cache
```

`GrypeDbStatus` returns `Valid == false` (it does not throw) when no database is installed, and `GrypeDbCheck` reports an available update through `UpdateAvailable` (Grype's exit code 100 is expected).

### CI: update once, then scan without network checks

```csharp
var ciEnvironment = new Dictionary<string, string>
{
    ["GRYPE_DB_CACHE_DIR"] = MakeAbsolute(Directory("./.cache/grype")).FullPath,  // cache this directory in CI
    ["GRYPE_DB_AUTO_UPDATE"] = "false",
    ["GRYPE_CHECK_FOR_APP_UPDATE"] = "false",
};

GrypeDbUpdate(new GrypeDbUpdateSettings { EnvironmentVariables = ciEnvironment });
GrypeScanSbom("./artifacts/bom.cdx.json", new GrypeScanSettings
{
    EnvironmentVariables = ciEnvironment,
    Outputs = { GrypeOutput.Table(), GrypeOutput.Json("./artifacts/grype.json") },
});
```

### CI artifact

Upload the report even when the gate fails, for example in GitLab:

```yaml
grype:
  script: dotnet cake --target=Grype
  artifacts:
    when: always
    paths:
      - artifacts/grype.json
```

## Global settings

Every settings class has Grype's global flags: `ConfigFiles` (`-c`), `Profiles` (`--profile`), `Quiet` (`-q`) and `Verbosity` (`-v`/`-vv`), plus Cake's standard `ToolPath`, `WorkingDirectory`, `EnvironmentVariables`, `HandleExitCode`, `PostAction`, … Relative paths are resolved against `WorkingDirectory` (or the Cake working directory).

## Not supported (yet)

`grype db list|providers|search|diff`, `grype config`, `grype explain` (needs stdin; a prototype feature), parsing SARIF/CycloneDX output, and piping Syft output into Grype.

## License

MIT
````

- [ ] **Step 3: Run the full test suite**

Run: `dotnet test Cake.Grype.sln`
Expected: PASS on all three TFMs.

- [ ] **Step 4: Pack and verify the package contents**

```bash
dotnet pack src/Cake.Grype/Cake.Grype.csproj -c Release -o artifacts
unzip -l artifacts/Cake.Grype.0.1.0.nupkg
unzip -p artifacts/Cake.Grype.0.1.0.nupkg Cake.Grype.nuspec
```

Expected: `artifacts/Cake.Grype.0.1.0.nupkg` and `.snupkg`; entries `lib/net8.0/Cake.Grype.dll` and `.xml` (same for `net9.0`, `net10.0`), `icon.png`, `README.md`. The nuspec has tag `cake-addin` and does **not** list `Cake.Core` as a dependency. The build has no CS1591 (missing XML comment) warnings. `artifacts/` is already in `.gitignore`.

- [ ] **Step 5: End-to-end verification with the real Cake runner and real Grype**

This confirms what unit tests cannot: Cake generates the aliases (including `GrypeVersion GrypeVersion()` and `GrypeDbStatus GrypeDbStatus()`), real Grype accepts every argument, `sbom:` with an absolute Windows path works, the table and JSON artifact come from one run, `FailOn` writes the file before exiting 2, the DB exit codes behave as captured, and the 48 MB report reads fine. It needs network access (nuget.org and the Grype DB) and deletes and re-downloads the local Grype database.

```bash
REPO=$(pwd -W 2>/dev/null || pwd)          # forward-slash Windows path in Git Bash, e.g. C:/Dev/GitHub/mgnslndh/Cake.Grype
E2E=$(mktemp -d) && cd "$E2E"
dotnet new tool-manifest
dotnet tool install Cake.Tool --version 6.0.0
```

Create `build.cake` in `$E2E` (replace `<REPO>` with the value of `$REPO`):

```csharp
#addin nuget:file:///<REPO>/artifacts/?package=Cake.Grype&version=0.1.0

var sbom = File("<REPO>/etc/sample.cdx.json");

Task("Db").Does(() =>
{
    var version = GrypeVersion();
    Information("Grype {0}, DB schema {1}", version.Version, version.SupportedDbSchema);

    GrypeDbDelete();
    var status = GrypeDbStatus();
    if (status.Valid) throw new Exception("expected no valid DB after delete");
    Information("Status after delete: {0}", status.Error);

    var check = GrypeDbCheck();
    if (!check.UpdateAvailable || check.Current != null) throw new Exception("expected an available update and no current DB");
    Information("Candidate DB {0} built {1}", check.Candidate.SchemaVersion, check.Candidate.Built);

    GrypeDbUpdate();
    status = GrypeDbStatus();
    if (!status.Valid) throw new Exception("expected a valid DB after update");
    Information("DB {0} built {1}", status.SchemaVersion, status.Built);
    if (GrypeDbCheck().UpdateAvailable) throw new Exception("expected the DB to be current after update");
});

Task("Scan").IsDependentOn("Db").Does(() =>
{
    GrypeScanSbom(sbom, new GrypeScanSettings
    {
        Outputs = { GrypeOutput.Table(), GrypeOutput.Json("out/grype.json") },
        SortBy = GrypeSortBy.Risk,
    });

    var report = GrypeReadJson("out/grype.json");
    foreach (var count in report.CountBySeverity()) Information("{0,-10} {1}", count.Key, count.Value);

    var kev = report.Matches.Where(m => m.IsKnownExploited).ToList();
    Information("Known exploited: {0}", kev.Count);
    if (kev.Count == 0) throw new Exception("expected KEV matches in the sample SBOM");

    var cvssOnRelated = report.Matches.Count(m => m.Vulnerability.Cvss.Count == 0 && m.MaxCvssBaseScore != null);
    Information("CVSS only on a related record: {0}", cvssOnRelated);
    if (cvssOnRelated == 0) throw new Exception("expected matches with CVSS only on a related record");

    Information("Unassessed (Unknown): {0}", report.Matches.Count(m => m.Severity == GrypeSeverity.Unknown));
});

Task("FailOn").IsDependentOn("Db").Does(() =>
{
    try
    {
        GrypeScanSbom(sbom, new GrypeScanSettings { Outputs = { GrypeOutput.Json("out/failon.json") }, FailOn = GrypeSeverity.Critical });
        throw new Exception("expected the FailOn gate to throw");
    }
    catch (CakeException e) when (e.Message.Contains("exit code 2"))
    {
        Information("FailOn gate threw as expected: {0}", e.Message);
    }

    if (!FileExists("out/failon.json")) throw new Exception("expected the report to be written before the gate failed");

    GrypeScanSbom(sbom, new GrypeScanSettings
    {
        Outputs = { GrypeOutput.Json("out/handled.json") },
        FailOn = GrypeSeverity.Critical,
        HandleExitCode = code => code is 0 or 2,
    });
    Information("Handled exit code 2; report has {0} matches", GrypeReadJson("out/handled.json").Matches.Count);
});

Task("Default").IsDependentOn("Scan").IsDependentOn("FailOn");

RunTarget(Argument("target", "Default"));
```

```bash
dotnet cake
```

Expected:
- The `Db` task logs the Grype version, `Status after delete: database does not exist`, a candidate DB, then a valid DB after update — no exceptions.
- The `Scan` task prints Grype's table (starting with `NAME  INSTALLED  FIXED IN  TYPE  VULNERABILITY  SEVERITY  EPSS  RISK`) in the terminal, then severity counts, `Known exploited: 2` or more, a non-zero "CVSS only on a related record", and the unassessed count.
- The `FailOn` task logs `FailOn gate threw as expected: Grype: Process returned an error (exit code 2).` and the handled run's match count.
- `out/grype.json` exists and is tens of MB.

(The exact counts depend on the DB published that day; KEV ≥ 1 and CVSS-only-on-related ≥ 1 held for the 2026-09-22 DB: KEV 2, CVSS only on related 692.)

If a check fails, record the exact Grype/Cake output, fix the responsible runner (argument spelling, quoting, or Windows path form are the likely causes — e.g. if `sbom:C:/…` is rejected, pass the native path form instead and add a unit test), and re-run this step.

- [ ] **Step 6: Commit**

```bash
cd <REPO>
git add README.md tests/Cake.Grype.Tests/ArchitectureTests.cs
git commit -m "docs: add README and architecture guard"
```

**Finally:** any deviation between observed Grype behaviour and the spec's research findings must be written back into the spec (`docs/superpowers/specs/2026-09-22-cake-grype-design.md`) in the same commit.
