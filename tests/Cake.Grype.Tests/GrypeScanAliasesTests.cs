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
