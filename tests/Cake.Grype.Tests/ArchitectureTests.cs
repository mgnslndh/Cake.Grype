using System.Text.RegularExpressions;

namespace Cake.Grype.Tests;

public sealed class ArchitectureTests
{
    private static DirectoryInfo FindJsonSourceDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Cake.Grype.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return new DirectoryInfo(Path.Combine(directory.FullName, "src", "Cake.Grype", "Json"));
    }

    private static string StripComments(string source)
    {
        return Regex.Replace(source, @"//[^\n]*|/\*.*?\*/", string.Empty, RegexOptions.Singleline);
    }

    internal static bool ReferencesRunnerNamespaces(string source)
    {
        var code = StripComments(source);
        return Regex.IsMatch(code, @"\bCake\.Grype\.(Scan|Db)\b")
            || Regex.IsMatch(code, @"(?<![\w.])(Scan|Db)\s*\.");
    }

    [Fact]
    public void Json_Namespace_Does_Not_Reference_Scan_Or_Db()
    {
        var files = FindJsonSourceDirectory().GetFiles("*.cs");
        Assert.NotEmpty(files);

        var offenders = files.Where(file => ReferencesRunnerNamespaces(File.ReadAllText(file.FullName))).Select(file => file.Name);

        Assert.Empty(offenders);
    }

    [Theory]
    [InlineData("using Cake.Grype.Scan;", true)]
    [InlineData("var x = new Db.GrypeDbStatus();", true)]
    [InlineData("Cake.Grype.Db.GrypeDbChecker c;", true)]
    [InlineData("// see Cake.Grype.Scan for the runner", false)]
    [InlineData("/// <see cref=\"Cake.Grype.Db.GrypeDbStatus\"/>", false)]
    [InlineData("var d = GrypeDbStatus.Valid;", false)]
    [InlineData("using Cake.Grype;", false)]
    public void Detects_References_To_Runner_Namespaces(string source, bool expected)
    {
        Assert.Equal(expected, ReferencesRunnerNamespaces(source));
    }
}
