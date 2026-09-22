# Cake.Grype — Design

Date: 2026-09-22
Status: Draft for review

## Goal

A Cake add-in that wraps [Grype](https://github.com/anchore/grype) by Anchore, a vulnerability scanner for container images, filesystems and SBOMs. The primary use case is scanning a CycloneDX/SPDX/Syft SBOM (`sbom:` source) for vulnerabilities in declared components.

From a Cake build a user must be able to (not necessarily in the same run):

- **A.** See Grype's output in the terminal.
- **B.** Write an output file to use as a CI job artifact.
- **C.** Inspect the findings programmatically in C# and act on them (e.g. throw on a critical or known-exploited vulnerability).

A and B are achievable in one run via multiple `-o` outputs. C is achieved by reading Grype's JSON report into a typed model.

## Decisions

| Topic | Decision |
|---|---|
| Structure | Mirror Cake's built-in tool design (e.g. `Cake.Common/Tools/DotNet`) and the sibling add-in Cake.DotNetOutdated: abstract base runner + base settings, one runner and one settings class per logical command, aliases split into one partial file per command. |
| Alias naming | Tool name first, then verb, as in Cake's built-in aliases (`DotNetBuild`, `NuGetPack`): `GrypeScan`, `GrypeReadJson`, `GrypeDbUpdate`, … Runner classes use nouns (`GrypeScanner`, `GrypeDbUpdater`), like `DotNetBuilder`. |
| Result API | Composable. `GrypeScan` returns `void`; findings are read with `GrypeReadJson(FilePath)`. No hidden temp files. |
| Scan target | Typed `GrypeSource` with factory methods plus an implicit conversion from `string`. Thin shortcut aliases only where they add value (typed path argument or a common CI case); every other scheme goes through `GrypeSource`. |
| Namespaces | `Cake.Grype` (base, settings, source, severity, version), `Cake.Grype.Scan`, `Cake.Grype.Db`, `Cake.Grype.Json` (the model of Grype's native `-o json` document; leaves room for `.Sarif`/`.CycloneDx` later). `GrypeSeverity` lives in the root because both scan settings and the JSON model use it. |
| Exit codes | Not remapped (Cake.DotNetOutdated precedent). Non-zero throws `CakeException`; users opt out with `HandleExitCode`. |
| Cake conventions | Baseline Cake 6.0.0; multi-target `net8.0;net9.0;net10.0`; `Cake.Core` with `PrivateAssets="All"`, no `Cake.Common` reference; package tag `cake-addin`; embedded `PackageIcon` and `PackageReadmeFile`; XML docs shipped; `[CakeMethodAlias]`, `[CakeAliasCategory("Grype")]`, `[CakeNamespaceImport]` for every namespace an alias's types use. |

## Research findings

Verified against Grype 0.119.0 (Syft 1.52.0, DB schema v6) on Windows, installed via `winget install Anchore.Grype` (executable on `PATH` through the WinGet links directory).

**Commands**

| Command | Build value | Feasibility | Decision |
|---|---|---|---|
| root scan (`grype <source>`) | Core | Easy | Stage 1 |
| `db update` | Pre-warm/cache the DB in CI | Easy | Stage 1 |
| `db status` | Log/validate the DB in use; `-o json` | Easy | Stage 1 |
| `db check` | Detect a stale DB; `-o json` | Easy | Stage 1 |
| `db import` | Offline/air-gapped runners | Easy | Stage 1 |
| `db delete` | Reset to a clean cache in tests and CI | Easy | Stage 1 |
| `version` | Log version, enforce a minimum; `-o json` | Easy | Stage 1 |
| `db list`, `db providers`, `db search`, `db diff` | Interactive/diagnostic | Easy, low value | Postponed |
| `config`, `config locations` | Debugging configuration | Easy, low value | Postponed |
| `explain` | Requires Grype JSON on stdin; labelled a prototype ("subject to change") | Awkward | Postponed |
| `completion`, `help` | None | — | Never |

**Scan behaviour (observed)**

- `-o` is repeatable and accepts `format=path`: `grype sbom:etc/sample.cdx.json -o table -o json=<path>` prints the table on stdout **and** writes JSON to the file in one run.
- Output formats: `json`, `table`, `cyclonedx`, `cyclonedx-json`, `sarif`, `template` (with `-t <file>`). The deprecated `embedded-cyclonedx-vex-*` formats are not exposed.
- `--fail-on <severity>` exits with code `2` after writing outputs; stderr shows `discovered vulnerabilities at or above the severity threshold`.
- A missing SBOM file exits `1`.
- Global flags on every command: `-c/--config` (repeatable), `--profile` (repeatable), `-q/--quiet`, `-v` (count).
- Environment-only settings relevant to CI (not CLI flags): `GRYPE_DB_AUTO_UPDATE`, `GRYPE_CHECK_FOR_APP_UPDATE`, `GRYPE_DB_CACHE_DIR`. Set through `ToolSettings.EnvironmentVariables`.

**JSON report (observed on `etc/sample.cdx.json`)**

- 48 MB, 1,408 matches, 6,713 ignored matches. Top level: `matches`, `ignoredMatches`, `source`, `distro`, `descriptor`.
- `vulnerability`: `id`, `dataSource`, `namespace`, `severity` (`Unknown|Negligible|Low|Medium|High|Critical`), `urls`, `description`, `cvss[]` (`source`, `type`, `version`, `vector`, `metrics.baseScore|exploitabilityScore|impactScore`), `knownExploited[]` (CISA KEV: `cve`, `vendorProject`, `product`, `dateAdded`, `requiredAction`, `dueDate`, `knownRansomwareCampaignUse`, …), `epss[]` (`cve`, `epss`, `percentile`, `date`), `cwes[]`, `fix` (`versions[]`, `state`: `fixed|not-fixed|wont-fix|unknown`), `advisories[]`, `risk` (float, 0–100).
- `relatedVulnerabilities[]` has the same metadata shape as `vulnerability` minus `fix`, `advisories`, `risk`.
- **692 of 1,408 matches have no CVSS on the primary vulnerability; it exists only on a related (NVD) record.** Related records can also carry KEV/EPSS data. Signal helpers must therefore aggregate across both.
- Signal counts in the sample: KEV 2, EPSS 1,390 (8 with score ≥ 0.1), risk > 0 on 1,390 (max 78.75).
- `artifact`: `id`, `name`, `version`, `type`, `locations[]`, `language`, `licenses[]`, `cpes[]`, `purl`, `upstreams[]`.

**Other JSON outputs (observed)**

- `db status -o json`: `schemaVersion`, `from`, `built`, `path`, `valid`, `error`. Exit `0` with a valid DB. **With no DB installed it exits `1` but still prints valid JSON:** `schemaVersion: ""`, `valid: false`, `error: "database does not exist"`.
- `db check -o json`: `currentDB { schemaVersion, built }` (null when no DB is installed), `candidateDB { schemaVersion, built, path, checksum }` (null when current), `updateAvailable`. Exit `0` when current; **exit `100` when an update is available** (observed after `grype db delete`; stderr: `ERROR db upgrade available`).
- **Reserved / unanalysed CVEs** (observed: 18 matches, e.g. `CVE-2026-53613` on util-linux packages): `severity: "Unknown"`, `risk: 0`, `epss`/`knownExploited`/`description` absent, `cvss: []`, and the related NVD record is equally empty. The table shows `Unknown  N/A  N/A`.
- `version -o json`: `application`, `version`, `buildDate`, `gitCommit`, `gitDescription`, `platform`, `goVersion`, `compiler`, `syftVersion`, `supportedDbSchema`.

## Solution layout

```
Cake.Grype.sln
global.json                           MTP test runner
src/Cake.Grype/
  GrypeTool.cs                        abstract base runner
  GrypeSettings.cs                    global flags
  GrypeSource.cs
  GrypeSeverity.cs
  GrypeVersion.cs, GrypeVersionReader.cs, GrypeVersionSettings.cs
  Scan/   GrypeScanner, GrypeScanSettings, GrypeOutput, GrypeOutputFormat, GrypeSortBy, GrypeScope, GrypeFixStates
  Db/     GrypeDbUpdater, GrypeDbStatusReader, GrypeDbChecker, GrypeDbImporter, GrypeDbDeleter,
          their settings classes, GrypeDbStatus, GrypeDbCheckResult
  Json/   GrypeReport model, GrypeReportReader
  GrypeAliases.Scan.cs, GrypeAliases.ReadJson.cs, GrypeAliases.Db.cs, GrypeAliases.Version.cs
  icon.png
tests/Cake.Grype.Tests/               xUnit v3 (MTP) + Cake.Testing
```

## Section 1 — Tool layer

- `GrypeTool<TSettings> : Tool<TSettings>` (Cake.Core). Tool name `Grype`; executable names `grype.exe`, `grype`. Resolution is Cake's standard order: `ToolPath` → tool registrations → `PATH`.
- Argument order: command words (e.g. `db status`), then global flags, then command-specific flags, then the positional argument.
- `GrypeSettings : ToolSettings` — global flags:
  - `ConfigFiles` (`ICollection<FilePath>`, `-c`, repeatable)
  - `Profiles` (`ICollection<string>`, `--profile`, repeatable)
  - `Quiet` (`bool`, `-q`)
  - `Verbosity` (`GrypeVerbosity`: `Default`, `Info` → `-v`, `Debug` → `-vv`)
- Runners that parse JSON from stdout (`db status`, `db check`, `version`) redirect standard output, always pass `-o json`, and always add `-q` so log lines cannot mix into the JSON. Invalid JSON throws `CakeException` including the first 500 characters of stdout.

## Section 2 — Scan command

**`GrypeScanSettings : GrypeSettings`**

| Property | Type | Flag |
|---|---|---|
| `Outputs` | `ICollection<GrypeOutput>` | `-o <format>[=<path>]`, repeatable |
| `OutputFile` | `FilePath` | `--file` |
| `FailOn` | `GrypeSeverity?` | `-f` (`negligible|low|medium|high|critical`; `Unknown` is rejected with `ArgumentException`) |
| `Template` | `FilePath` | `-t` |
| `SortBy` | `GrypeSortBy?` (`Package`, `Severity`, `Epss`, `Risk`, `Kev`, `Vulnerability`) | `--sort-by` |
| `OnlyFixed` | `bool` | `--only-fixed` |
| `OnlyNotFixed` | `bool` | `--only-notfixed` |
| `IgnoreStates` | `GrypeFixStates` (flags: `Fixed`, `NotFixed`, `Unknown`, `WontFix`) | `--ignore-states a,b` |
| `ByCve` | `bool` | `--by-cve` |
| `AddCpesIfNone` | `bool` | `--add-cpes-if-none` |
| `Distro` | `string` | `--distro` |
| `Platform` | `string` | `--platform` |
| `Scope` | `GrypeScope?` (`Squashed`, `AllLayers`, `DeepSquashed`) | `-s` |
| `Exclude` | `ICollection<string>` | `--exclude`, repeatable |
| `From` | `ICollection<string>` | `--from`, repeatable |
| `Name` | `string` | `--name` |
| `Vex` | `ICollection<FilePath>` | `--vex`, repeatable |
| `ShowSuppressed` | `bool` | `--show-suppressed` |

**`GrypeOutput`** — immutable `{ Format, File }`. Factories: `Table()`, `Json(FilePath file = null)`, `Sarif(…)`, `CycloneDx(…)`, `CycloneDxJson(…)`, `Template(…)`. Renders `-o json` or `-o json=<absolute path>`. When `Outputs` is empty no `-o` is emitted and Grype's default table is used.

**`GrypeSource`** — immutable, `ToString()` yields the Grype argument. Factories:

| Factory | Grype form |
|---|---|
| `Sbom(FilePath)` | `sbom:<abs path>` |
| `Directory(DirectoryPath)` | `dir:<abs path>` |
| `File(FilePath)` | `file:<abs path>` |
| `Image(string)` | `<ref>` (Grype's default daemon lookup) |
| `Docker(string)` / `Podman(string)` | `docker:<ref>` / `podman:<ref>` |
| `Registry(string)` | `registry:<ref>` |
| `DockerArchive(FilePath)` / `OciArchive(FilePath)` | `docker-archive:<abs>` / `oci-archive:<abs>` |
| `OciDirectory(DirectoryPath)` | `oci-dir:<abs>` |
| `Singularity(FilePath)` | `singularity:<abs>` |
| `PurlFile(FilePath)` / `Purl(string)` | `purl:<abs>` / `<purl>` |
| `CpeFile(FilePath)` / `Cpe(string)` | `cpes:<abs>` / `<cpe>` |
| `Zarf(FilePath)` | `zarf:<abs>` |

Relative paths are made absolute against the Cake working directory (`settings.WorkingDirectory` if set). An implicit conversion from `string` passes the value through unchanged (`"registry:alpine:3.20"`). Null/empty input throws `ArgumentNullException`/`ArgumentException`.

**Runner behaviour (`GrypeScanner`)**

- Creates missing parent directories of `OutputFile` and of every `GrypeOutput.File`.
- Deletes existing output files before running, so a failed run cannot leave a stale report that looks current.
- `FailOn` → exit `2` after outputs are written → `CakeException`. With CI artifacts configured to upload on failure the file is still available. To inspect findings instead of failing: `HandleExitCode = c => c is 0 or 2` (documented).

**Aliases** (`GrypeAliases.Scan.cs`), each with and without settings:

- `GrypeScan(GrypeSource source[, GrypeScanSettings settings])` — the general form; supports building a list of sources and scanning dynamically.
- `GrypeScanSbom(FilePath)`, `GrypeScanDirectory(DirectoryPath)`, `GrypeScanFile(FilePath)`, `GrypeScanImage(string)`, `GrypeScanRegistry(string)` — thin shortcuts delegating to `GrypeScan`.

```csharp
GrypeScanSbom("./artifacts/bom.cdx.json", new GrypeScanSettings {
    Outputs = { GrypeOutput.Table(), GrypeOutput.Json("./artifacts/grype.json") },
    SortBy = GrypeSortBy.Risk,
});

foreach (var source in new List<GrypeSource> {
    GrypeSource.Sbom("./artifacts/api.cdx.json"),
    GrypeSource.Registry("myorg/api:1.2.3"),
})
    GrypeScan(source, settings);
```

## Section 3 — JSON model and reader (`Cake.Grype.Json`)

Immutable, read-only types; only fields present in Grype's JSON.

```
GrypeReport
  Matches         IReadOnlyList<GrypeMatch>
  IgnoredMatches  IReadOnlyList<GrypeIgnoredMatch>     GrypeMatch + AppliedIgnoreRules
  Distro          GrypeDistro? { Name, Version, IdLike }
  Descriptor      GrypeDescriptor { Name, Version, Timestamp }
  Source          GrypeReportSource { Type, Target (JsonElement; shape varies by scheme) }
  AtOrAbove(GrypeSeverity)  → IEnumerable<GrypeMatch>
  CountBySeverity()         → IReadOnlyDictionary<GrypeSeverity, int>

GrypeMatch
  Vulnerability           GrypeVulnerability
  RelatedVulnerabilities  IReadOnlyList<GrypeVulnerabilityMetadata>
  Artifact                GrypeArtifact { Id, Name, Version, Type, Language, Purl, Cpes, Licenses }
  // signal helpers — aggregate over Vulnerability ∪ RelatedVulnerabilities
  IsKnownExploited   bool
  MaxEpssScore       double?
  MaxEpssPercentile  double?
  MaxCvssBaseScore   double?   (null when no record has CVSS)
  Severity           GrypeSeverity  (= Vulnerability.Severity)
  Risk               double         (= Vulnerability.Risk)

GrypeVulnerabilityMetadata
  Id, DataSource, Namespace, Description, Urls
  Severity        GrypeSeverity
  Cvss            [{ Source, Type, Version, Vector, BaseScore, ExploitabilityScore, ImpactScore }]
  Epss            [{ Cve, Score, Percentile, Date }]
  KnownExploited  [{ Cve, VendorProject, Product, DateAdded, RequiredAction, DueDate, KnownRansomwareCampaignUse, Notes, Urls, Cwes }]
  Cwes            [{ Cve, Cwe, Source, Type }]

GrypeVulnerability : GrypeVulnerabilityMetadata
  Fix         GrypeFix { State: GrypeFixState (Fixed|NotFixed|WontFix|Unknown), Versions }
  Advisories  [{ Id, Link }]
  Risk        double
```

**Signals covered**

| Signal | Meaning | Model |
|---|---|---|
| KEV | Known to have been exploited | `KnownExploited`, `GrypeMatch.IsKnownExploited` |
| EPSS | Likely to be exploited, per the model | `Epss`, `GrypeMatch.MaxEpssScore` / `MaxEpssPercentile` |
| CVSS / severity | Potential technical impact | `Severity`, `Cvss`, `GrypeMatch.MaxCvssBaseScore` |
| Grype risk | Combined prioritisation (severity, EPSS, KEV) | `Risk` |

**Reader (`GrypeReportReader`)**

- `System.Text.Json`, deserialising from a `FileStream` (no full-file string). Case-insensitive property names; unknown properties ignored; UTF-8 BOM tolerated.
- Not mapped (reduces memory on large reports; addable later without breaking changes): `matchDetails`, artifact `locations`/`upstreams`, `descriptor.configuration`, `descriptor.db`.
- `GrypeSeverity` and `GrypeFixState` parse case-insensitively; unrecognised values map to `Unknown` instead of throwing, so newer Grype versions don't break existing scripts.
- `GrypeSeverity` is ordered `Unknown < Negligible < Low < Medium < High < Critical`, so `>=` comparisons work.
- **`Unknown` means "not yet assessed", not "low".** Reserved or unanalysed CVEs have `Severity == Unknown`, `Risk == 0` and no EPSS/KEV/CVSS, so severity, risk and EPSS gates skip them. This is deliberate (they carry no signal yet); a gate that should block on them must check `m.Severity == GrypeSeverity.Unknown` explicitly. Documented in the README next to the gating example. Missing arrays (`epss`, `knownExploited`, `cwes`, `urls`) deserialise as empty lists, never null; missing `description` is null.

**Alias** (`GrypeAliases.ReadJson.cs`): `GrypeReport GrypeReadJson(FilePath)`. Relative paths resolve against the Cake working directory; a missing file throws `FileNotFoundException`.

```csharp
var report = GrypeReadJson("./artifacts/grype.json");
var blocking = report.Matches.Where(m =>
       m.IsKnownExploited
    || m.MaxEpssScore >= 0.5
    || (m.Severity >= GrypeSeverity.Critical && m.Vulnerability.Fix.State == GrypeFixState.Fixed)
    || m.Risk >= 50).ToList();
if (blocking.Count > 0)
    throw new CakeException($"{blocking.Count} blocking vulnerabilities");
```

## Section 4 — DB and version commands

All settings classes derive from `GrypeSettings`; each command has its own runner.

| Alias | Runner / Settings | Invocation | Returns |
|---|---|---|---|
| `GrypeDbUpdate([s])` | `GrypeDbUpdater` / `GrypeDbUpdateSettings` | `grype db update` | `void` |
| `GrypeDbStatus([s])` | `GrypeDbStatusReader` / `GrypeDbStatusSettings` | `grype db status -o json -q` | `GrypeDbStatus { SchemaVersion, From?, Built?, Path, Valid, Error? }` |
| `GrypeDbCheck([s])` | `GrypeDbChecker` / `GrypeDbCheckSettings` | `grype db check -o json -q` | `GrypeDbCheckResult { UpdateAvailable, Current: GrypeDbDescription?, Candidate: GrypeDbDescription? }` with `GrypeDbDescription { SchemaVersion, Built, Path?, Checksum? }` |
| `GrypeDbImport(FilePath[, s])` / `GrypeDbImport(Uri[, s])` | `GrypeDbImporter` / `GrypeDbImportSettings` | `grype db import <abs path or URL>` | `void` |
| `GrypeDbDelete([s])` | `GrypeDbDeleter` / `GrypeDbDeleteSettings` | `grype db delete` | `void` |
| `GrypeVersion([s])` | `GrypeVersionReader` / `GrypeVersionSettings` | `grype version -o json -q` | `GrypeVersion { Application, Version, BuildDate, GitCommit, GitDescription, Platform, GoVersion, Compiler, SyftVersion, SupportedDbSchema }` |

- `db check`: the runner accepts exit `0` and `100` (update available) and relies on `updateAvailable` in the JSON. `Current` is null when no DB is installed.
- `db status`: the runner accepts exit `1` **only** when stdout parses as status JSON with `valid: false`; it then returns the result (`Valid == false`, `Error` set) instead of throwing, and the caller decides. Exit `1` without parseable status JSON throws `CakeException` as usual.
- `GrypeDbImport` takes `FilePath` or `Uri`, not `string`: a `string` overload next to `FilePath` would always win overload resolution (`FilePath` converts implicitly from `string`) and make the path/URL distinction implicit. A `Uri` may carry a `checksum=sha256:…` query parameter, which Grype verifies.
- Aliases and result types share names where the alias *is* the query (`GrypeVersion GrypeVersion()`, `GrypeDbStatus GrypeDbStatus()`), following Cake's `GitVersion GitVersion()` precedent.

## Error handling summary

- Tool not found → Cake's standard `CakeException` ("Grype: Could not locate executable").
- Non-zero exit → `CakeException` unless `HandleExitCode` accepts it (and except the `db check` update-available code).
- Invalid JSON on stdout → `CakeException` with an excerpt.
- Null/invalid arguments → `ArgumentNullException` / `ArgumentException` at the alias boundary.

## Testing

- **Unit tests** (xUnit v3 on Microsoft Testing Platform, `Cake.Testing` `ToolFixture` pattern):
  - Argument building for every setting of every runner, including global flags, ordering and quoting of paths with spaces.
  - Every `GrypeSource` factory, relative-to-absolute path handling, `WorkingDirectory` override, implicit string conversion.
  - `GrypeOutput` rendering; output directory creation and stale-file deletion (fake file system).
  - Stdout parsing for `db status` (valid; no DB with exit 1 → `Valid == false`; exit 1 with non-JSON → throws), `db check` (current / exit 100 with update available / no DB installed), `version`, and invalid JSON.
  - Exit code handling: `FailOn` exit `2` throws; `HandleExitCode` suppresses it.
- **Report reader tests** against a small hand-trimmed fixture derived from real Grype output, covering: KEV, EPSS, CVSS present only on a related record, a reserved CVE (`Unknown` severity, `risk: 0`, absent `epss`/`knownExploited`/`description`), all fix states, an unrecognised severity string, ignored matches, BOM, unknown properties. Tests for each `GrypeMatch` signal helper.
- **Architecture test:** `Cake.Grype.Json` does not reference `Cake.Grype.Scan` or `Cake.Grype.Db`.
- **Manual end-to-end** (documented in the plan; needs network for the DB): with the packed add-in and real Grype on `etc/sample.cdx.json`, confirm table in terminal plus JSON artifact from one run; `FailOn = Critical` exits 2 with the file written; `GrypeReadJson` over the full 48 MB report and a sample gate; `GrypeDbStatus`/`GrypeDbCheck`/`GrypeVersion` results; `GrypeDbDelete`, then `GrypeDbStatus` (`Valid == false`) and `GrypeDbCheck` (`UpdateAvailable`), then `GrypeDbUpdate`; whether `FailOn` counts `Unknown`-severity matches (expected: no).
- Tests run on all target frameworks.

## Out of scope (postponed)

- `db list`, `db providers`, `db search`, `db diff`.
- `config`, `config locations`.
- `explain` (needs stdin; prototype feature).
- `completion`, `help` (never).
- Parsing SARIF / CycloneDX output formats.
- Piping Syft JSON into Grype via stdin.
- Publishing/CI pipeline for the add-in itself (separate task).

## Documentation

README: installation (`#addin nuget:?package=Cake.Grype`, installing Grype via winget/brew/install script, `ToolPath`), scanning an SBOM with table + JSON artifact, the `FailOn`/`HandleExitCode` note, reading the report and gating on KEV/EPSS/CVSS/risk, DB commands for CI caching (`GRYPE_DB_AUTO_UPDATE=false`, `GRYPE_DB_CACHE_DIR`), and a CI artifact example that uploads on failure.
