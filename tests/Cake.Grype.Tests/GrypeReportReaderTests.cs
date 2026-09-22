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
