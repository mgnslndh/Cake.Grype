using Cake.Core;

namespace Build.Tests;

public sealed class SmokeTestTemplateTests
{
    [Theory]
    [InlineData("Cake.Grype.0.1.0.nupkg", "0.1.0")]
    [InlineData("Cake.Grype.0.1.0-preview.1.nupkg", "0.1.0-preview.1")]
    [InlineData("Cake.Grype.0.0.0-alpha.0.24.nupkg", "0.0.0-alpha.0.24")]
    public void GetPackageVersion_Reads_The_Version_From_The_Package_File_Name(string fileName, string expected)
    {
        Assert.Equal(expected, SmokeTestTemplate.GetPackageVersion(fileName));
    }

    [Theory]
    [InlineData("Cake.Grype.nupkg")]
    [InlineData("Other.Package.1.0.0.nupkg")]
    [InlineData("Cake.Grype.1.0.0.snupkg")]
    public void GetPackageVersion_Rejects_Other_Files(string fileName)
    {
        var result = Record.Exception(() => SmokeTestTemplate.GetPackageVersion(fileName));

        Assert.IsType<CakeException>(result);
    }

    [Fact]
    public void Render_Replaces_Every_Placeholder()
    {
        var rendered = SmokeTestTemplate.Render(
            "#addin nuget:file:///@@FEED@@/?package=Cake.Grype&version=@@VERSION@@ // @@FIXTURE@@ @@VERSION@@",
            new Dictionary<string, string>
            {
                ["FEED"] = "C:/repo/artifacts",
                ["VERSION"] = "1.2.0",
                ["FIXTURE"] = "C:/repo/fixture.json",
            });

        Assert.Equal("#addin nuget:file:///C:/repo/artifacts/?package=Cake.Grype&version=1.2.0 // C:/repo/fixture.json 1.2.0", rendered);
    }

    [Fact]
    public void Render_Fails_On_A_Placeholder_Without_A_Value()
    {
        var result = Record.Exception(() => SmokeTestTemplate.Render(
            "version=@@VERSION@@ feed=@@FEED@@",
            new Dictionary<string, string> { ["VERSION"] = "1.2.0" }));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("@@FEED@@", exception.Message);
    }
}
