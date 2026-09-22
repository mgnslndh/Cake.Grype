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
