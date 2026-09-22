using System;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.Grype.Json;

namespace Cake.Grype
{
    public static partial class GrypeAliases
    {
        /// <summary>
        /// Reads a Grype JSON report (written with <c>GrypeOutput.Json(file)</c>) for inspection in C#.
        /// </summary>
        /// <remarks>
        /// <see cref="GrypeSeverity.Unknown"/> means "not assessed yet" (for example a reserved CVE). Such matches have
        /// risk 0 and no EPSS, KEV or CVSS data, so severity, risk and EPSS thresholds skip them; check for
        /// <c>GrypeSeverity.Unknown</c> explicitly if they should block a build.
        /// </remarks>
        /// <param name="context">The context.</param>
        /// <param name="path">The report file.</param>
        /// <returns>The report.</returns>
        /// <example>
        /// <code>
        /// var report = GrypeReadJson("./artifacts/grype.json");
        /// var blocking = report.Matches.Where(m =>
        ///        m.IsKnownExploited
        ///     || m.MaxEpssScore &gt;= 0.5
        ///     || (m.Severity &gt;= GrypeSeverity.Critical &amp;&amp; m.Vulnerability.Fix.State == GrypeFixState.Fixed)
        ///     || m.Risk &gt;= 50).ToList();
        /// if (blocking.Count &gt; 0)
        /// {
        ///     throw new CakeException($"{blocking.Count} blocking vulnerabilities");
        /// }
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Json")]
        public static GrypeReport GrypeReadJson(this ICakeContext context, FilePath path)
        {
            ArgumentNullException.ThrowIfNull(context);

            return new GrypeReportReader(context.FileSystem, context.Environment).Read(path);
        }
    }
}
