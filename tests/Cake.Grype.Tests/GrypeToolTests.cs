using Cake.Core.IO;
using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeToolTests
{
    private static string Args(Action<GrypeVersionSettings> configure)
    {
        var fixture = new VersionFixture();
        configure(fixture.Settings);
        return fixture.Run().Args;
    }

    [Fact]
    public void Should_Find_The_Grype_Executable()
    {
        var result = new VersionFixture().Run();

        Assert.Equal("/Working/tools/grype", result.Path.FullPath);
    }

    [Fact]
    public void Should_Throw_If_Grype_Cannot_Be_Found()
    {
        var fixture = new VersionFixture();
        fixture.GivenDefaultToolDoNotExist();

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Could not locate executable.");
    }

    [Fact]
    public void Should_Use_The_Tool_Path_From_Settings()
    {
        var fixture = new VersionFixture();
        fixture.Settings.ToolPath = "/opt/grype/grype";
        fixture.GivenSettingsToolPathExist();

        var result = fixture.Run();

        Assert.Equal("/opt/grype/grype", result.Path.FullPath);
    }

    [Fact]
    public void Should_Add_Config_Files_As_Absolute_Paths()
    {
        var args = Args(s =>
        {
            s.ConfigFiles.Add("grype.yaml");
            s.ConfigFiles.Add("/etc/grype/base.yaml");
        });

        Assert.Equal("version -c \"/Working/grype.yaml\" -c \"/etc/grype/base.yaml\" -q -o json", args);
    }

    [Fact]
    public void Should_Resolve_Config_Files_Against_The_Settings_Working_Directory()
    {
        var args = Args(s =>
        {
            s.WorkingDirectory = "/Other";
            s.ConfigFiles.Add("grype.yaml");
        });

        Assert.Equal("version -c \"/Other/grype.yaml\" -q -o json", args);
    }

    [Fact]
    public void Should_Resolve_A_Relative_Settings_Working_Directory_Against_The_Cake_Working_Directory()
    {
        var args = Args(s =>
        {
            s.WorkingDirectory = "sub";
            s.ConfigFiles.Add("grype.yaml");
        });

        Assert.Equal("version -c \"/Working/sub/grype.yaml\" -q -o json", args);
    }

    [Fact]
    public void Should_Add_Profiles_And_Skip_Blank_Ones()
    {
        var args = Args(s =>
        {
            s.Profiles.Add("ci");
            s.Profiles.Add(" ");
            s.Profiles.Add("strict");
        });

        Assert.Equal("version --profile \"ci\" --profile \"strict\" -q -o json", args);
    }

    [Fact]
    public void Should_Not_Duplicate_Quiet_For_Json_Commands()
    {
        Assert.Equal("version -q -o json", Args(s => s.Quiet = true));
    }

    [Theory]
    [InlineData(GrypeVerbosity.Default, "version -q -o json")]
    [InlineData(GrypeVerbosity.Info, "version -q -v -o json")]
    [InlineData(GrypeVerbosity.Debug, "version -q -vv -o json")]
    public void Should_Add_Verbosity(GrypeVerbosity verbosity, string expected)
    {
        Assert.Equal(expected, Args(s => s.Verbosity = verbosity));
    }

    [Fact]
    public void Should_Tolerate_Null_Collections()
    {
        var args = Args(s =>
        {
            s.ConfigFiles = null;
            s.Profiles = null;
        });

        Assert.Equal("version -q -o json", args);
    }
}
