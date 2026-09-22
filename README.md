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
