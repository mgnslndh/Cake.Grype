using System.Reflection;
using System.Xml.Linq;
using Cake.Core.Annotations;

namespace Cake.Grype.Tests;

/// <summary>
/// Enforces the Cake documentation guidelines (https://cakebuild.net/community/contributing/documentation) on the
/// generated XML documentation file. The compiler only checks that public members have some documentation.
/// </summary>
public sealed class DocumentationTests
{
    private static readonly Assembly Addin = typeof(GrypeAliases).Assembly;

    private static readonly Lazy<XDocument> Documentation =
        new(() => XDocument.Load(Path.ChangeExtension(Addin.Location, ".xml")));

    [Fact]
    public void Every_Alias_Has_A_Summary_Params_Returns_Exceptions_And_An_Example()
    {
        var aliases = Addin.GetExportedTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.IsDefined(typeof(CakeMethodAliasAttribute)))
            .ToList();
        Assert.NotEmpty(aliases);

        var problems = aliases.SelectMany(FindProblems).ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public void Every_Public_Namespace_Has_A_NamespaceDoc()
    {
        var namespaces = Addin.GetExportedTypes().Select(type => type.Namespace).Distinct().ToList();

        var undocumented = namespaces.Where(ns => !HasText(Member($"T:{ns}.NamespaceDoc")?.Element("summary"))).ToList();

        Assert.Empty(undocumented);
    }

    private static IEnumerable<string> FindProblems(MethodInfo alias)
    {
        var parameters = alias.GetParameters();
        var id = $"M:{alias.DeclaringType.FullName}.{alias.Name}({string.Join(",", parameters.Select(p => p.ParameterType.FullName))})";
        var name = $"{alias.Name}({string.Join(", ", parameters.Select(p => p.ParameterType.Name))})";
        var member = Member(id);
        if (member == null)
        {
            yield return $"{name}: no documentation";
            yield break;
        }

        if (!HasText(member.Element("summary")))
        {
            yield return $"{name}: no <summary>";
        }

        foreach (var parameter in parameters)
        {
            if (!HasText(member.Elements("param").FirstOrDefault(p => (string)p.Attribute("name") == parameter.Name)))
            {
                yield return $"{name}: no <param name=\"{parameter.Name}\">";
            }
        }

        if (alias.ReturnType != typeof(void) && !HasText(member.Element("returns")))
        {
            yield return $"{name}: no <returns>";
        }

        if (!member.Elements("exception").Any(HasText))
        {
            yield return $"{name}: no <exception>";
        }

        if (!HasText(member.Element("example")?.Element("code")))
        {
            yield return $"{name}: no <example><code>";
        }
    }

    private static XElement Member(string id) =>
        Documentation.Value.Descendants("member").FirstOrDefault(member => (string)member.Attribute("name") == id);

    private static bool HasText(XElement element) =>
        element != null && (!string.IsNullOrWhiteSpace(element.Value) || element.HasElements);
}
