using Cake.Core.IO;
using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeDbCommandTests
{
    [Fact]
    public void Update_Should_Run_Db_Update()
    {
        Assert.Equal("db update", new DbUpdateFixture().Run().Args);
    }

    [Fact]
    public void Update_Should_Add_Global_Flags_After_The_Command()
    {
        var fixture = new DbUpdateFixture();
        fixture.Settings.Quiet = true;
        fixture.Settings.ConfigFiles.Add("grype.yaml");

        Assert.Equal("db update -c \"/Working/grype.yaml\" -q", fixture.Run().Args);
    }

    [Fact]
    public void Update_Should_Throw_If_Settings_Are_Null()
    {
        var result = Record.Exception(() => new DbUpdateFixture { Settings = null }.Run());

        Assertions.IsArgumentNullException(result, "settings");
    }

    [Fact]
    public void Update_Should_Throw_On_Failure()
    {
        var fixture = new DbUpdateFixture();
        fixture.GivenProcessExitsWithCode(1);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 1).");
    }

    [Fact]
    public void Delete_Should_Run_Db_Delete()
    {
        Assert.Equal("db delete", new DbDeleteFixture().Run().Args);
    }

    [Fact]
    public void Import_Should_Pass_An_Absolute_Archive_Path()
    {
        var fixture = new DbImportFixture { Archive = "db/vulnerability-db.tar.zst" };

        Assert.Equal("db import \"/Working/db/vulnerability-db.tar.zst\"", fixture.Run().Args);
    }

    [Fact]
    public void Import_Should_Pass_A_Url_Unchanged()
    {
        var url = "https://grype.anchore.io/databases/v6/vulnerability-db_v6.1.9.tar.zst?checksum=sha256%3A972de5";
        var fixture = new DbImportFixture { Url = new Uri(url) };

        Assert.Equal($"db import \"{url}\"", fixture.Run().Args);
    }

    [Fact]
    public void Import_Should_Throw_If_The_Archive_Is_Null()
    {
        var result = Record.Exception(() => new DbImportFixture { Archive = null }.Run());

        Assertions.IsArgumentNullException(result, "archive");
    }

    [Fact]
    public void Import_Should_Reject_A_Relative_Url()
    {
        var result = Record.Exception(() => new DbImportFixture { Url = new Uri("db.tar.zst", UriKind.Relative) }.Run());

        Assertions.IsArgumentException(result, "url");
    }

    [Fact]
    public void Aliases_Should_Run_The_Db_Commands()
    {
        var alias = new AliasContext();

        alias.Context.GrypeDbUpdate();
        alias.Context.GrypeDbImport(new FilePath("db.tar.zst"));
        alias.Context.GrypeDbImport(new Uri("https://example.com/db.tar.zst"));
        alias.Context.GrypeDbDelete();

        Assert.Equal(
            new[]
            {
                "db update",
                "db import \"/Working/db.tar.zst\"",
                "db import \"https://example.com/db.tar.zst\"",
                "db delete",
            },
            alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Import_Alias_Should_Accept_A_String_As_A_File_Path()
    {
        var alias = new AliasContext();

        alias.Context.GrypeDbImport("db.tar.zst");

        Assert.Equal(new[] { "db import \"/Working/db.tar.zst\"" }, alias.ProcessRunner.Arguments);
    }

    [Fact]
    public void Aliases_Should_Throw_If_Context_Is_Null()
    {
        Assertions.IsArgumentNullException(Record.Exception(() => GrypeAliases.GrypeDbUpdate(null)), "context");
        Assertions.IsArgumentNullException(Record.Exception(() => GrypeAliases.GrypeDbDelete(null)), "context");
        Assertions.IsArgumentNullException(
            Record.Exception(() => GrypeAliases.GrypeDbImport(null, new FilePath("db.tar.zst"))),
            "context");
    }
}
