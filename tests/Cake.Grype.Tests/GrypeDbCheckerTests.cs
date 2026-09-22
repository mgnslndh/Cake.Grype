using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeDbCheckerTests
{
    // Captured from `grype db check -o json` (Grype 0.119.0) with a current database, exit code 0.
    private const string Current = """
        {
         "currentDB": {
          "schemaVersion": "v6.1.9",
          "built": "2026-09-22T06:30:41Z"
         },
         "candidateDB": null,
         "updateAvailable": false
        }
        """;

    // Captured after `grype db delete`: exit code 100.
    private const string NoDatabase = """
        {
         "currentDB": null,
         "candidateDB": {
          "schemaVersion": "v6.1.9",
          "built": "2026-09-22T06:30:41Z",
          "path": "vulnerability-db_v6.1.9_2026-09-22T00:33:02Z_1790058641.tar.zst",
          "checksum": "sha256:972de542534d3461cc3c3849a37a3f917b823de3b9c367e289db0d163519b42b"
         },
         "updateAvailable": true
        }
        """;

    [Fact]
    public void Should_Request_Quiet_Json_Output()
    {
        Assert.Equal("db check -q -o json", new DbCheckFixture().Run().Args);
    }

    [Fact]
    public void Should_Parse_A_Current_Database()
    {
        var fixture = new DbCheckFixture();
        fixture.GivenStandardOutput(Current);

        fixture.Run();

        Assert.False(fixture.Result.UpdateAvailable);
        Assert.Null(fixture.Result.Candidate);
        Assert.Equal("v6.1.9", fixture.Result.Current.SchemaVersion);
        Assert.Equal(new DateTimeOffset(2026, 9, 22, 6, 30, 41, TimeSpan.Zero), fixture.Result.Current.Built);
    }

    [Fact]
    public void Should_Accept_Exit_Code_100_When_An_Update_Is_Available()
    {
        var fixture = new DbCheckFixture();
        fixture.GivenStandardOutput(NoDatabase);
        fixture.GivenProcessExitsWithCode(100);

        fixture.Run();

        Assert.True(fixture.Result.UpdateAvailable);
        Assert.Null(fixture.Result.Current);
        Assert.Equal("v6.1.9", fixture.Result.Candidate.SchemaVersion);
        Assert.Equal("vulnerability-db_v6.1.9_2026-09-22T00:33:02Z_1790058641.tar.zst", fixture.Result.Candidate.Path);
        Assert.Equal("sha256:972de542534d3461cc3c3849a37a3f917b823de3b9c367e289db0d163519b42b", fixture.Result.Candidate.Checksum);
    }

    [Fact]
    public void Should_Throw_On_Other_Exit_Codes()
    {
        var fixture = new DbCheckFixture();
        fixture.GivenStandardOutput(NoDatabase);
        fixture.GivenProcessExitsWithCode(1);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 1).");
    }

    [Fact]
    public void Should_Throw_On_Exit_Code_100_Without_Json()
    {
        var fixture = new DbCheckFixture();
        fixture.GivenStandardOutput(string.Empty);
        fixture.GivenProcessExitsWithCode(100);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: The output is not valid JSON: (no output)");
    }

    [Fact]
    public void Alias_Should_Check_The_Database()
    {
        var alias = new AliasContext();
        alias.ProcessRunner.Process.SetStandardOutput(NoDatabase.Split('\n'));
        alias.ProcessRunner.Process.SetExitCode(100);

        var result = alias.Context.GrypeDbCheck();

        Assert.True(result.UpdateAvailable);
        Assert.Equal(new[] { "db check -q -o json" }, alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Alias_Should_Throw_If_Context_Is_Null()
    {
        var result = Record.Exception(() => GrypeAliases.GrypeDbCheck(null));

        Assertions.IsArgumentNullException(result, "context");
    }
}
