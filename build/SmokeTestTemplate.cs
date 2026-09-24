using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cake.Core;

namespace Build
{
    /// <summary>
    /// Renders the smoke-test script templates in tests/Smoke, whose placeholders look like <c>@@NAME@@</c>.
    /// </summary>
    public static class SmokeTestTemplate
    {
        private static readonly Regex PackageFileName = new Regex(@"^Cake\.Grype\.(?<version>\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?)\.nupkg$");
        private static readonly Regex Placeholder = new Regex("@@(?<name>[A-Z_]+)@@");

        /// <summary>
        /// Gets the package version from a package file name, e.g. <c>Cake.Grype.1.2.0-preview.1.nupkg</c> → <c>1.2.0-preview.1</c>.
        /// </summary>
        /// <param name="fileName">The package file name.</param>
        /// <returns>The version.</returns>
        public static string GetPackageVersion(string fileName)
        {
            var match = PackageFileName.Match(fileName ?? string.Empty);
            if (!match.Success)
            {
                throw new CakeException($"'{fileName}' is not a Cake.Grype package file name.");
            }

            return match.Groups["version"].Value;
        }

        /// <summary>
        /// Replaces every <c>@@NAME@@</c> placeholder with its value. Throws if a placeholder has no value, so a template
        /// can never run half-rendered.
        /// </summary>
        /// <param name="template">The template text.</param>
        /// <param name="values">The values, keyed by placeholder name without the <c>@@</c>.</param>
        /// <returns>The rendered text.</returns>
        public static string Render(string template, IReadOnlyDictionary<string, string> values)
        {
            var missing = new SortedSet<string>();
            var rendered = Placeholder.Replace(template, match =>
            {
                var name = match.Groups["name"].Value;
                if (values.TryGetValue(name, out var value))
                {
                    return value;
                }

                missing.Add(match.Value);
                return match.Value;
            });

            if (missing.Count > 0)
            {
                throw new CakeException($"Smoke-test template has placeholders without a value: {string.Join(", ", missing)}.");
            }

            return rendered;
        }
    }
}
