// Cake SDK (file-based) runner smoke test for the packed Cake.Grype. The build's Smoke target renders this template
// into artifacts/smoke/sdk (filling in @@-placeholders) and runs it with `dotnet cake.cs`; it is not meant to run from here.
// No using directives on purpose: the aliases and their namespace imports must work as a script author sees them.
#:sdk Cake.Sdk@6.3.0
#:package Cake.Grype@@@VERSION@@

var grypePath = EnvironmentVariable("GRYPE_PATH");

Task("Default").Does(() =>
{
    var version = GrypeVersion(new GrypeVersionSettings
    {
        ToolPath = string.IsNullOrWhiteSpace(grypePath) ? null : new FilePath(grypePath),
    });
    Information("Grype {0} (DB schema {1})", version.Version, version.SupportedDbSchema);

    var report = GrypeReadJson("@@FIXTURE@@");
    var knownExploited = report.Matches.Count(match => match.IsKnownExploited);
    Information("Report: {0} matches, {1} known exploited", report.Matches.Count, knownExploited);
    if (report.Matches.Count != 4 || knownExploited != 1)
    {
        throw new Exception("The test report did not read as expected.");
    }

    var settings = new GrypeScanSettings { SortBy = GrypeSortBy.Risk, Outputs = { GrypeOutput.Json("grype.json") } };
    Information("Scan settings: {0} output(s), sorted by {1}", settings.Outputs.Count, settings.SortBy);
});

RunTarget(Argument("target", "Default"));
