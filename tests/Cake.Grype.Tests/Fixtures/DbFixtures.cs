using Cake.Core.IO;
using Cake.Grype.Db;

namespace Cake.Grype.Tests.Fixtures;

internal sealed class DbUpdateFixture : GrypeFixture<GrypeDbUpdateSettings>
{
    protected override void RunTool()
    {
        new GrypeDbUpdater(FileSystem, Environment, ProcessRunner, Tools).Update(Settings);
    }
}

internal sealed class DbDeleteFixture : GrypeFixture<GrypeDbDeleteSettings>
{
    protected override void RunTool()
    {
        new GrypeDbDeleter(FileSystem, Environment, ProcessRunner, Tools).Delete(Settings);
    }
}

internal sealed class DbImportFixture : GrypeFixture<GrypeDbImportSettings>
{
    public FilePath Archive { get; set; }

    public Uri Url { get; set; }

    protected override void RunTool()
    {
        var importer = new GrypeDbImporter(FileSystem, Environment, ProcessRunner, Tools);
        if (Url != null)
        {
            importer.Import(Url, Settings);
        }
        else
        {
            importer.Import(Archive, Settings);
        }
    }
}

internal sealed class DbStatusFixture : GrypeFixture<GrypeDbStatusSettings>
{
    public DbStatusFixture()
    {
        GivenStandardOutput("{}");
    }

    public GrypeDbStatus Result { get; private set; }

    protected override void RunTool()
    {
        Result = new GrypeDbStatusReader(FileSystem, Environment, ProcessRunner, Tools).Read(Settings);
    }
}

internal sealed class DbCheckFixture : GrypeFixture<GrypeDbCheckSettings>
{
    public DbCheckFixture()
    {
        GivenStandardOutput("{}");
    }

    public GrypeDbCheckResult Result { get; private set; }

    protected override void RunTool()
    {
        Result = new GrypeDbChecker(FileSystem, Environment, ProcessRunner, Tools).Check(Settings);
    }
}
