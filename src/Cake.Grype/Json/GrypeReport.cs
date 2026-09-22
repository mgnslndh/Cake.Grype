using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Cake.Grype.Json
{
    /// <summary>
    /// A Grype JSON report (<c>-o json</c>).
    /// </summary>
    public sealed class GrypeReport
    {
        /// <summary>Gets the vulnerability matches.</summary>
        public IReadOnlyList<GrypeMatch> Matches { get; init; } = Array.Empty<GrypeMatch>();

        /// <summary>Gets the matches suppressed by ignore rules or VEX documents.</summary>
        public IReadOnlyList<GrypeIgnoredMatch> IgnoredMatches { get; init; } = Array.Empty<GrypeIgnoredMatch>();

        /// <summary>Gets what was scanned.</summary>
        public GrypeReportSource Source { get; init; }

        /// <summary>Gets the Linux distribution matched against, if any.</summary>
        public GrypeDistro Distro { get; init; }

        /// <summary>Gets information about the Grype run that produced the report.</summary>
        public GrypeDescriptor Descriptor { get; init; }

        /// <summary>
        /// Gets the matches with a severity at or above <paramref name="severity"/>.
        /// <see cref="GrypeSeverity.Unknown"/> (unassessed) matches are only included for a threshold of <c>Unknown</c>.
        /// </summary>
        /// <param name="severity">The minimum severity.</param>
        /// <returns>The matches.</returns>
        public IEnumerable<GrypeMatch> AtOrAbove(GrypeSeverity severity)
        {
            return Matches.Where(match => match.Severity >= severity);
        }

        /// <summary>
        /// Counts the matches per severity. Every severity is present, with zero if nothing matched it.
        /// </summary>
        /// <returns>The count per severity.</returns>
        public IReadOnlyDictionary<GrypeSeverity, int> CountBySeverity()
        {
            var counts = Enum.GetValues<GrypeSeverity>().ToDictionary(severity => severity, _ => 0);
            foreach (var match in Matches)
            {
                counts[match.Severity]++;
            }

            return counts;
        }
    }

    /// <summary>
    /// What Grype scanned.
    /// </summary>
    public sealed class GrypeReportSource
    {
        /// <summary>Gets the source type, for example <c>image</c>, <c>directory</c> or <c>file</c>.</summary>
        public string Type { get; init; }

        /// <summary>Gets the source target; its shape depends on <see cref="Type"/>.</summary>
        public JsonElement Target { get; init; }
    }

    /// <summary>
    /// A Linux distribution.
    /// </summary>
    public sealed class GrypeDistro
    {
        /// <summary>Gets the distribution name, for example <c>debian</c>.</summary>
        public string Name { get; init; }

        /// <summary>Gets the distribution version, for example <c>12</c>.</summary>
        public string Version { get; init; }

        /// <summary>Gets the distributions this one is like.</summary>
        public IReadOnlyList<string> IdLike { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Information about the Grype run that produced a report.
    /// </summary>
    public sealed class GrypeDescriptor
    {
        /// <summary>Gets the tool name (<c>grype</c>).</summary>
        public string Name { get; init; }

        /// <summary>Gets the Grype version.</summary>
        public string Version { get; init; }

        /// <summary>Gets when the report was produced.</summary>
        public DateTimeOffset? Timestamp { get; init; }
    }
}
