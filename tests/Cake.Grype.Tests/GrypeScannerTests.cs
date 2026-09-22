using Cake.Core.IO;
using Cake.Grype.Scan;
using Cake.Grype.Tests.Fixtures;
using Cake.Testing;

namespace Cake.Grype.Tests;

public sealed class GrypeScannerTests
{
    private const string Source = "\"sbom:/Working/bom.cdx.json\"";

    private static string Args(Action<GrypeScanSettings> configure)
    {
        var fixture = new ScanFixture();
        configure(fixture.Settings);
        return fixture.Run().Args;
    }

    [Fact]
    public void Should_Throw_If_Settings_Are_Null()
    {
        var fixture = new ScanFixture { Settings = null };

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsArgumentNullException(result, "settings");
    }

    [Fact]
    public void Should_Throw_If_Source_Is_Null()
    {
        var fixture = new ScanFixture { Source = null };

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsArgumentNullException(result, "source");
    }

    [Fact]
    public void Should_Pass_Only_The_Source_By_Default()
    {
        Assert.Equal(Source, new ScanFixture().Run().Args);
    }

    [Fact]
    public void Should_Not_Redirect_Standard_Output()
    {
        Assert.False(new ScanFixture().Run().Process.RedirectStandardOutput);
    }

    [Fact]
    public void Should_Resolve_The_Source_Against_The_Settings_Working_Directory()
    {
        var args = Args(s => s.WorkingDirectory = "/Other");

        Assert.Equal("\"sbom:/Other/bom.cdx.json\"", args);
    }

    [Fact]
    public void Should_Emit_Global_Flags_Before_Scan_Flags()
    {
        var args = Args(s =>
        {
            s.Quiet = true;
            s.OnlyFixed = true;
        });

        Assert.Equal("-q --only-fixed " + Source, args);
    }

    [Theory]
    [InlineData(GrypeOutputFormat.Table, "table")]
    [InlineData(GrypeOutputFormat.Json, "json")]
    [InlineData(GrypeOutputFormat.CycloneDx, "cyclonedx")]
    [InlineData(GrypeOutputFormat.CycloneDxJson, "cyclonedx-json")]
    [InlineData(GrypeOutputFormat.Sarif, "sarif")]
    [InlineData(GrypeOutputFormat.Template, "template")]
    public void Should_Render_Output_Formats(GrypeOutputFormat format, string expected)
    {
        var output = format switch
        {
            GrypeOutputFormat.Table => GrypeOutput.Table(),
            GrypeOutputFormat.Json => GrypeOutput.Json(),
            GrypeOutputFormat.CycloneDx => GrypeOutput.CycloneDx(),
            GrypeOutputFormat.CycloneDxJson => GrypeOutput.CycloneDxJson(),
            GrypeOutputFormat.Sarif => GrypeOutput.Sarif(),
            _ => GrypeOutput.Template(),
        };

        Assert.Equal(format, output.Format);
        Assert.Equal($"-o {expected} {Source}", Args(s => s.Outputs.Add(output)));
    }

    [Fact]
    public void Should_Write_Several_Outputs_With_Absolute_File_Paths()
    {
        var args = Args(s =>
        {
            s.Outputs.Add(GrypeOutput.Table());
            s.Outputs.Add(GrypeOutput.Json("out/grype.json"));
            s.Outputs.Add(GrypeOutput.Sarif("/reports/grype.sarif"));
        });

        Assert.Equal(
            "-o table -o \"json=/Working/out/grype.json\" -o \"sarif=/reports/grype.sarif\" " + Source,
            args);
    }

    [Fact]
    public void Should_Skip_Null_Outputs()
    {
        Assert.Equal("-o table " + Source, Args(s =>
        {
            s.Outputs.Add(null);
            s.Outputs.Add(GrypeOutput.Table());
        }));
    }

    [Fact]
    public void Should_Add_The_Default_Output_File()
    {
        Assert.Equal("--file \"/Working/out/report.txt\" " + Source, Args(s => s.OutputFile = "out/report.txt"));
    }

    [Fact]
    public void Should_Quote_An_Output_File_Path_With_A_Space()
    {
        Assert.Equal(
            "-o \"json=/Working/my out/grype.json\" " + Source,
            Args(s => s.Outputs.Add(GrypeOutput.Json("my out/grype.json"))));
    }

    [Fact]
    public void Should_Quote_A_Source_Path_With_A_Space()
    {
        var fixture = new ScanFixture { Source = GrypeSource.Sbom("my boms/bom.cdx.json") };

        Assert.Equal("\"sbom:/Working/my boms/bom.cdx.json\"", fixture.Run().Args);
    }

    [Theory]
    [InlineData(GrypeSeverity.Negligible, "negligible")]
    [InlineData(GrypeSeverity.Low, "low")]
    [InlineData(GrypeSeverity.Medium, "medium")]
    [InlineData(GrypeSeverity.High, "high")]
    [InlineData(GrypeSeverity.Critical, "critical")]
    public void Should_Add_Fail_On(GrypeSeverity severity, string expected)
    {
        Assert.Equal($"-f {expected} {Source}", Args(s => s.FailOn = severity));
    }

    [Fact]
    public void Should_Reject_Fail_On_Unknown()
    {
        var fixture = new ScanFixture();
        fixture.Settings.FailOn = GrypeSeverity.Unknown;

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsArgumentException(result, "settings");
    }

    [Fact]
    public void Should_Reject_An_Undefined_Fail_On()
    {
        var fixture = new ScanFixture();
        fixture.Settings.FailOn = (GrypeSeverity)9;

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsArgumentException(result, "settings");
    }

    [Fact]
    public void Should_Reject_An_Undefined_Sort_By()
    {
        var fixture = new ScanFixture();
        fixture.Settings.SortBy = (GrypeSortBy)99;

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsArgumentException(result, "settings");
    }

    [Fact]
    public void Should_Reject_An_Undefined_Scope()
    {
        var fixture = new ScanFixture();
        fixture.Settings.Scope = (GrypeScope)99;

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsArgumentException(result, "settings");
    }

    [Fact]
    public void Should_Add_The_Template_As_An_Absolute_Path()
    {
        Assert.Equal("-t \"/Working/tmpl/report.tmpl\" " + Source, Args(s => s.Template = "tmpl/report.tmpl"));
    }

    [Theory]
    [InlineData(GrypeSortBy.Package, "package")]
    [InlineData(GrypeSortBy.Severity, "severity")]
    [InlineData(GrypeSortBy.Epss, "epss")]
    [InlineData(GrypeSortBy.Risk, "risk")]
    [InlineData(GrypeSortBy.Kev, "kev")]
    [InlineData(GrypeSortBy.Vulnerability, "vulnerability")]
    public void Should_Add_Sort_By(GrypeSortBy sortBy, string expected)
    {
        Assert.Equal($"--sort-by {expected} {Source}", Args(s => s.SortBy = sortBy));
    }

    [Theory]
    [InlineData(GrypeIgnoreStates.Fixed, "fixed")]
    [InlineData(GrypeIgnoreStates.WontFix | GrypeIgnoreStates.NotFixed, "not-fixed,wont-fix")]
    [InlineData(GrypeIgnoreStates.Fixed | GrypeIgnoreStates.NotFixed | GrypeIgnoreStates.Unknown | GrypeIgnoreStates.WontFix, "fixed,not-fixed,unknown,wont-fix")]
    public void Should_Add_Ignore_States(GrypeIgnoreStates states, string expected)
    {
        Assert.Equal($"--ignore-states {expected} {Source}", Args(s => s.IgnoreStates = states));
    }

    [Theory]
    [InlineData(GrypeScope.Squashed, "squashed")]
    [InlineData(GrypeScope.AllLayers, "all-layers")]
    [InlineData(GrypeScope.DeepSquashed, "deep-squashed")]
    public void Should_Add_Scope(GrypeScope scope, string expected)
    {
        Assert.Equal($"-s {expected} {Source}", Args(s => s.Scope = scope));
    }

    [Fact]
    public void Should_Add_Switches()
    {
        Assert.Equal("--only-fixed " + Source, Args(s => s.OnlyFixed = true));
        Assert.Equal("--only-notfixed " + Source, Args(s => s.OnlyNotFixed = true));
        Assert.Equal("--by-cve " + Source, Args(s => s.ByCve = true));
        Assert.Equal("--add-cpes-if-none " + Source, Args(s => s.AddCpesIfNone = true));
        Assert.Equal("--show-suppressed " + Source, Args(s => s.ShowSuppressed = true));
    }

    [Fact]
    public void Should_Add_Values()
    {
        Assert.Equal("--distro \"debian:12\" " + Source, Args(s => s.Distro = "debian:12"));
        Assert.Equal("--platform \"linux/arm64\" " + Source, Args(s => s.Platform = "linux/arm64"));
        Assert.Equal("--name \"my-app\" " + Source, Args(s => s.Name = "my-app"));
    }

    [Fact]
    public void Should_Add_Repeatable_Options_And_Skip_Blank_Ones()
    {
        var args = Args(s =>
        {
            s.Exclude.Add("./tmp/**");
            s.Exclude.Add("");
            s.Exclude.Add("**/*.log");
            s.From.Add("registry");
            s.Vex.Add("vex/app.openvex.json");
            s.Vex.Add(null);
        });

        Assert.Equal(
            "--exclude \"./tmp/**\" --exclude \"**/*.log\" --from \"registry\" --vex \"/Working/vex/app.openvex.json\" " + Source,
            args);
    }

    [Fact]
    public void Should_Emit_All_Options_In_Order()
    {
        var args = Args(s =>
        {
            s.Quiet = true;
            s.Outputs.Add(GrypeOutput.Table());
            s.Outputs.Add(GrypeOutput.Json("out/grype.json"));
            s.OutputFile = "out/default.txt";
            s.FailOn = GrypeSeverity.High;
            s.Template = "tmpl/report.tmpl";
            s.SortBy = GrypeSortBy.Risk;
            s.OnlyFixed = true;
            s.OnlyNotFixed = true;
            s.IgnoreStates = GrypeIgnoreStates.WontFix;
            s.ByCve = true;
            s.AddCpesIfNone = true;
            s.Distro = "debian:12";
            s.Platform = "linux/arm64";
            s.Scope = GrypeScope.AllLayers;
            s.Exclude.Add("./tmp/**");
            s.From.Add("registry");
            s.Name = "my-app";
            s.Vex.Add("vex/app.openvex.json");
            s.ShowSuppressed = true;
        });

        Assert.Equal(
            "-q -o table -o \"json=/Working/out/grype.json\" --file \"/Working/out/default.txt\" -f high " +
            "-t \"/Working/tmpl/report.tmpl\" --sort-by risk --only-fixed --only-notfixed --ignore-states wont-fix " +
            "--by-cve --add-cpes-if-none --distro \"debian:12\" --platform \"linux/arm64\" -s all-layers " +
            "--exclude \"./tmp/**\" --from \"registry\" --name \"my-app\" --vex \"/Working/vex/app.openvex.json\" " +
            "--show-suppressed " + Source,
            args);
    }

    [Fact]
    public void Should_Delete_Stale_Output_Files_Before_Running()
    {
        var fixture = new ScanFixture();
        fixture.Settings.Outputs.Add(GrypeOutput.Json("out/grype.json"));
        fixture.Settings.OutputFile = "out/report.txt";
        fixture.FileSystem.CreateFile("/Working/out/grype.json").SetContent("stale");
        fixture.FileSystem.CreateFile("/Working/out/report.txt").SetContent("stale");

        fixture.Run();

        Assert.False(fixture.FileSystem.Exist(new FilePath("/Working/out/grype.json")));
        Assert.False(fixture.FileSystem.Exist(new FilePath("/Working/out/report.txt")));
    }

    [Fact]
    public void Should_Create_Output_Directories()
    {
        var fixture = new ScanFixture();
        fixture.Settings.Outputs.Add(GrypeOutput.Json("out/json/grype.json"));
        fixture.Settings.OutputFile = "out/text/report.txt";

        fixture.Run();

        Assert.True(fixture.FileSystem.Exist(new DirectoryPath("/Working/out/json")));
        Assert.True(fixture.FileSystem.Exist(new DirectoryPath("/Working/out/text")));
    }

    [Fact]
    public void Should_Not_Touch_Files_When_Validation_Fails()
    {
        var fixture = new ScanFixture();
        fixture.Settings.Outputs.Add(GrypeOutput.Json("grype.json"));
        fixture.Settings.FailOn = GrypeSeverity.Unknown;
        fixture.FileSystem.CreateFile("/Working/grype.json").SetContent("previous");

        Record.Exception(() => fixture.Run());

        Assert.True(fixture.FileSystem.Exist(new FilePath("/Working/grype.json")));
    }

    [Fact]
    public void Should_Throw_On_Exit_Code_2_From_Fail_On()
    {
        var fixture = new ScanFixture();
        fixture.Settings.FailOn = GrypeSeverity.Critical;
        fixture.GivenProcessExitsWithCode(2);

        var result = Record.Exception(() => fixture.Run());

        Assertions.IsCakeException(result, "Grype: Process returned an error (exit code 2).");
    }

    [Fact]
    public void Should_Not_Throw_On_Exit_Code_2_When_It_Is_Handled()
    {
        var fixture = new ScanFixture();
        fixture.Settings.FailOn = GrypeSeverity.Critical;
        fixture.Settings.HandleExitCode = code => code is 0 or 2;
        fixture.GivenProcessExitsWithCode(2);

        Assert.Null(Record.Exception(() => fixture.Run()));
    }

    [Fact]
    public void Should_Invoke_The_Users_Post_Action_Once()
    {
        var invocations = 0;
        var fixture = new ScanFixture();
        fixture.Settings.PostAction = _ => invocations++;

        fixture.Run();

        Assert.Equal(1, invocations);
    }
}
