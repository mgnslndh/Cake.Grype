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
