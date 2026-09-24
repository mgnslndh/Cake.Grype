using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Build
{
    /// <summary>
    /// Checks a packed Cake.Grype .nupkg for the content a Cake add-in package must have.
    /// </summary>
    public static class PackageVerifier
    {
        private static readonly string[] TargetFrameworks = { "net8.0", "net9.0", "net10.0" };

        /// <summary>
        /// Verifies a package.
        /// </summary>
        /// <param name="packagePath">The .nupkg file.</param>
        /// <returns>The problems found; empty when the package is valid.</returns>
        public static IReadOnlyList<string> Verify(string packagePath)
        {
            var problems = new List<string>();

            using var package = ZipFile.OpenRead(packagePath);
            var entries = new HashSet<string>(package.Entries.Select(entry => entry.FullName), StringComparer.OrdinalIgnoreCase);

            foreach (var framework in TargetFrameworks)
            {
                RequireEntry(entries, $"lib/{framework}/Cake.Grype.dll", problems);
                RequireEntry(entries, $"lib/{framework}/Cake.Grype.xml", problems);
            }

            RequireEntry(entries, "icon.png", problems);
            RequireEntry(entries, "README.md", problems);

            var nuspecEntry = package.Entries.SingleOrDefault(
                entry => !entry.FullName.Contains('/') && entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
            if (nuspecEntry == null)
            {
                problems.Add("missing .nuspec");
                return problems;
            }

            using var stream = nuspecEntry.Open();
            var nuspec = XDocument.Load(stream);
            var ns = nuspec.Root.Name.Namespace;

            var tags = (string)nuspec.Root.Element(ns + "metadata")?.Element(ns + "tags") ?? string.Empty;
            if (!tags.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("cake-addin"))
            {
                problems.Add("nuspec tags do not contain 'cake-addin'");
            }

            if (nuspec.Descendants(ns + "dependency").Any(
                dependency => string.Equals((string)dependency.Attribute("id"), "Cake.Core", StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add("nuspec declares a dependency on Cake.Core (it must stay PrivateAssets=All)");
            }

            return problems;
        }

        private static void RequireEntry(HashSet<string> entries, string path, List<string> problems)
        {
            if (!entries.Contains(path))
            {
                problems.Add($"missing {path}");
            }
        }
    }
}
