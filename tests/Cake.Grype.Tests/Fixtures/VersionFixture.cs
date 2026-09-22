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
