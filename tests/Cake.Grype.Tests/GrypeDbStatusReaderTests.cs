using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeDbStatusReaderTests
{
    // Captured from `grype db status -o json` (Grype 0.119.0), exit code 0.
    private const string ValidStatus = """
        {
         "schemaVersion": "v6.1.9",
         "from": "https://grype.anchore.io/databases/v6/vulnerability-db_v6.1.9_2026-09-22T00:33:02Z_1790058641.tar.zst?checksum=sha256%3A972de542534d3461cc3c3849a37a3f917b823de3b9c367e289db0d163519b42b",
         "built": "2026-09-22T06:30:41Z",
         "path": "C:\\Users\\me\\AppData\\Local\\cache\\grype\\db\\6\\vulnerability.db",
         "valid": true
        }
        """;

    // Captured after `grype db delete`: exit code 1, but valid JSON on stdout.
    private const string MissingStatus = """
        {
         "schemaVersion": "",
         "path": "C:\\Users\\me\\AppData\\Local\\cache\\grype\\db\\6\\vulnerability.db",
         "valid": false,
         "error": "database does not exist"
        }
        """;

    [Fact]
    public void Should_Request_Quiet_Json_Output()
    {
        Assert.Equal("db status -q -o json", new DbStatusFixture().Run().Args);
    }

    [Fact]
    public void Should_Throw_If_Settings_Are_Null()
    {
        var result = Record.Exception(() => new DbStatusFixture { Settings = null }.Run());

        Assertions.IsArgumentNullException(result, "settings");
    }

    [Fact]
    public void Should_Parse_A_Valid_Database()
    {
        var fixture = new DbStatusFixture();
        fixture.GivenStandardOutput(ValidStatus);

        fixture.Run();

        Assert.True(fixture.Result.Valid);
        Assert.Equal("v6.1.9", fixture.Result.SchemaVersion);
        Assert.Equal(new DateTimeOffset(2026, 9, 22, 6, 30, 41, TimeSpan.Zero), fixture.Result.Built);
        Assert.StartsWith("https://grype.anchore.io/databases/v6/", fixture.Result.From);
        Assert.EndsWith("vulnerability.db", fixture.Result.Path);
        Assert.Null(fixture.Result.Error);
    }

    [Fact]
    public void Should_Return_An_Invalid_Status_When_No_Database_Is_Installed()
    {
        var fixture = new DbStatusFixture();
        fixture.GivenStandardOutput(MissingStatus);
        fixture.GivenProcessExitsWithCode(1);

        fixture.Run();

        Assert.False(fixture.Result.Valid);
        Assert.Equal("database does not exist", fixture.Result.Error);
        Assert.Null(fixture.Result.Built);
    }

    [Fact]
    public void Should_Throw_On_Exit_Code_1_Without_Status_Json()
    {
        var fixture = new DbStatusFixture();
        fixture.GivenStandardOutput("failed to load config");
        fixture.GivenProcessExitsWithCode(1);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 1).");
    }

    [Fact]
    public void Should_Throw_On_Exit_Code_1_With_A_Valid_Status()
    {
        var fixture = new DbStatusFixture();
        fixture.GivenStandardOutput(ValidStatus);
        fixture.GivenProcessExitsWithCode(1);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 1).");
    }

    [Fact]
    public void Should_Throw_On_Other_Exit_Codes_Even_With_An_Invalid_Status()
    {
        var fixture = new DbStatusFixture();
        fixture.GivenStandardOutput(MissingStatus);
        fixture.GivenProcessExitsWithCode(2);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 2).");
    }

    [Fact]
    public void Alias_Should_Read_The_Status()
    {
        var alias = new AliasContext();
        alias.ProcessRunner.Process.SetStandardOutput(ValidStatus.Split('\n'));

        var status = alias.Context.GrypeDbStatus();

        Assert.True(status.Valid);
        Assert.Equal(new[] { "db status -q -o json" }, alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Alias_Should_Throw_If_Context_Is_Null()
    {
        var result = Record.Exception(() => GrypeAliases.GrypeDbStatus(null));

        Assertions.IsArgumentNullException(result, "context");
    }
}
