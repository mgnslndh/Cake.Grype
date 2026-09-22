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
