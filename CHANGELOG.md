# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- The package README now has a table of contents and covers installation on the Cake .NET Tool,
  Cake.Sdk and Cake Frosting.
- The XML documentation of every alias now has an example and lists the exceptions it throws, so
  IntelliSense and the Cake website show them. Each namespace is documented too.

## [0.1.0-preview.1] - 2026-09-24

First public release.

### Added

- `GrypeScan`, `GrypeScanSbom`, `GrypeScanDirectory`, `GrypeScanFile`, `GrypeScanImage` and
  `GrypeScanRegistry` aliases to scan SBOMs, directories, files and container images with Grype.
  `GrypeSource` covers the other Grype sources, such as OCI archives and purls.
- `GrypeScanSettings` writes several outputs in one run (`GrypeOutput.Table`, `Json`, `Sarif`,
  `CycloneDx`, `CycloneDxJson`, `Template`) and supports `FailOn`, `SortBy`, `OnlyFixed`,
  `OnlyNotFixed`, `IgnoreStates`, `Vex` and Grype's other scan flags.
- `GrypeReadJson` alias to read Grype's JSON report and gate a build in C# on severity, KEV,
  EPSS, CVSS or Grype's risk score.
- `GrypeDbUpdate`, `GrypeDbImport`, `GrypeDbDelete`, `GrypeDbStatus` and `GrypeDbCheck` aliases
  to manage Grype's vulnerability database, including offline imports.
- `GrypeVersion` alias to read Grype's version and supported database schema.
- Every settings class has Grype's global flags: `ConfigFiles`, `Profiles`, `Quiet` and `Verbosity`.
- Targets `net8.0`, `net9.0` and `net10.0`, for Cake 6.0.0 and later.

[Unreleased]: https://github.com/mgnslndh/Cake.Grype/compare/v0.1.0-preview.1...HEAD
[0.1.0-preview.1]: https://github.com/mgnslndh/Cake.Grype/releases/tag/v0.1.0-preview.1
