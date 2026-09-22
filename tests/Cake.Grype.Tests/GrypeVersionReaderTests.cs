using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeVersionReaderTests
{
    // Captured from `grype version -o json` (Grype 0.119.0 on Windows).
    private const string VersionJson = """
        {
         "application": "grype",
         "buildDate": "2026-09-17T16:17:07Z",
         "compiler": "gc",
         "gitCommit": "b6f5194537747ee7f705f4113069ac9eb269919f",
         "gitDescription": "v0.119.0",
         "goVersion": "go1.26.3",
         "platform": "windows/amd64",
         "supportedDbSchema": 6,
         "syftVersion": "v1.52.0",
         "version": "0.119.0"
        }
        """;

    [Fact]
    public void Should_Throw_If_Settings_Are_Null()
    {
        var fixture = new VersionFixture { Settings = null };

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsArgumentNullException(result, "settings");
    }

    [Fact]
    public void Should_Request_Quiet_Json_Output()
    {
        Assert.Equal("version -q -o json", new VersionFixture().Run().Args);
    }

    [Fact]
    public void Should_Redirect_Standard_Output()
    {
        var result = new VersionFixture().Run();

        Assert.True(result.Process.RedirectStandardOutput);
    }

    [Fact]
    public void Should_Parse_The_Version()
    {
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput(VersionJson);

        fixture.Run();

        Assert.Equal("grype", fixture.Result.Application);
        Assert.Equal("0.119.0", fixture.Result.Version);
        Assert.Equal("2026-09-17T16:17:07Z", fixture.Result.BuildDate);
        Assert.Equal("b6f5194537747ee7f705f4113069ac9eb269919f", fixture.Result.GitCommit);
        Assert.Equal("v0.119.0", fixture.Result.GitDescription);
        Assert.Equal("windows/amd64", fixture.Result.Platform);
        Assert.Equal("go1.26.3", fixture.Result.GoVersion);
        Assert.Equal("gc", fixture.Result.Compiler);
        Assert.Equal("v1.52.0", fixture.Result.SyftVersion);
        Assert.Equal(6, fixture.Result.SupportedDbSchema);
    }

    [Fact]
    public void Should_Throw_If_The_Output_Is_Not_Json()
    {
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput("Application: grype");

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: The output is not valid JSON: Application: grype");
    }

    [Fact]
    public void Should_Throw_If_There_Is_No_Output()
    {
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput(string.Empty);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: The output is not valid JSON: (no output)");
    }

    [Fact]
    public void Should_Truncate_Long_Invalid_Output_In_The_Error()
    {
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput(new string('x', 600));

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: The output is not valid JSON: " + new string('x', 500) + "...");
    }

    [Fact]
    public void Should_Throw_On_A_Non_Zero_Exit_Code_Before_Parsing()
    {
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput(string.Empty);
        fixture.GivenProcessExitsWithCode(1);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 1).");
    }

    [Fact]
    public void Should_Invoke_The_Users_Post_Action_Once_After_Reading_The_Output()
    {
        var invocations = 0;
        var fixture = new VersionFixture();
        fixture.GivenStandardOutput(VersionJson);
        fixture.Settings.PostAction = _ => invocations++;

        fixture.Run();

        Assert.Equal(1, invocations);
        Assert.Equal("0.119.0", fixture.Result.Version);
    }

    [Fact]
    public void Alias_Should_Run_Version()
    {
        var alias = new AliasContext();
        alias.ProcessRunner.Process.SetStandardOutput(VersionJson.Split('\n'));

        var version = alias.Context.GrypeVersion();

        Assert.Equal("0.119.0", version.Version);
        Assert.Equal(new[] { "version -q -o json" }, alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Alias_Should_Throw_If_Context_Is_Null()
    {
        var result = Record.Exception(() => GrypeAliases.GrypeVersion(null));

        Assertions.IsArgumentNullException(result, "context");
    }
}
