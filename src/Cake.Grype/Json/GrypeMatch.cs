using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Cake.Grype.Json
{
    /// <summary>
    /// A vulnerability found in a package.
    /// </summary>
    /// <remarks>
    /// The signal helpers (<see cref="IsKnownExploited"/>, <see cref="MaxEpssScore"/>, <see cref="MaxCvssBaseScore"/>, …)
    /// look at the <see cref="Vulnerability"/> and all <see cref="RelatedVulnerabilities"/>: distro advisories often carry
    /// no CVSS themselves, only the related NVD record does.
    /// </remarks>
    public class GrypeMatch
    {
        /// <summary>Gets the matched vulnerability.</summary>
        public GrypeVulnerability Vulnerability { get; init; }

        /// <summary>Gets related records of the same vulnerability, for example the NVD entry for a distro advisory.</summary>
        public IReadOnlyList<GrypeVulnerabilityMetadata> RelatedVulnerabilities { get; init; } = Array.Empty<GrypeVulnerabilityMetadata>();

        /// <summary>Gets the vulnerable package.</summary>
        public GrypeArtifact Artifact { get; init; }

        /// <summary>
        /// Gets the severity Grype assigned to the match. <see cref="GrypeSeverity.Unknown"/> means "not assessed yet".
        /// </summary>
        [JsonIgnore]
        public GrypeSeverity Severity => Vulnerability?.Severity ?? GrypeSeverity.Unknown;

        /// <summary>Gets Grype's combined risk score (0–100) from severity, EPSS and KEV; 0 when unassessed.</summary>
        [JsonIgnore]
        public double Risk => Vulnerability?.Risk ?? 0;

        /// <summary>Gets a value indicating whether any record lists the vulnerability as known exploited (CISA KEV).</summary>
        [JsonIgnore]
        public bool IsKnownExploited => Records.Any(record => record.KnownExploited.Count > 0);

        /// <summary>Gets the highest EPSS probability (0–1) of any record, or <c>null</c> if none has EPSS data.</summary>
        [JsonIgnore]
        public double? MaxEpssScore => Records.SelectMany(record => record.Epss).Select(epss => (double?)epss.Score).Max();

        /// <summary>Gets the highest EPSS percentile (0–1) of any record, or <c>null</c> if none has EPSS data.</summary>
        [JsonIgnore]
        public double? MaxEpssPercentile => Records.SelectMany(record => record.Epss).Select(epss => (double?)epss.Percentile).Max();

        /// <summary>
        /// Gets the highest CVSS base score of any record, or <c>null</c> if none has CVSS data. This is the maximum
        /// across all CVSS versions present: a record's <see cref="GrypeVulnerabilityMetadata.Cvss"/> can mix v2 and
        /// v3.x entries (see <see cref="GrypeCvss.Version"/>), and scores are not comparable across versions, so
        /// treat this as "the worst reported score", not a score on a single consistent scale.
        /// </summary>
        [JsonIgnore]
        public double? MaxCvssBaseScore => Records.SelectMany(record => record.Cvss).Select(cvss => cvss.BaseScore).Max();

        private IEnumerable<GrypeVulnerabilityMetadata> Records
        {
            get
            {
                if (Vulnerability != null)
                {
                    yield return Vulnerability;
                }

                foreach (var related in RelatedVulnerabilities.Where(related => related != null))
                {
                    yield return related;
                }
            }
        }
    }

    /// <summary>
    /// A match suppressed by an ignore rule or a VEX document.
    /// </summary>
    public sealed class GrypeIgnoredMatch : GrypeMatch
    {
        /// <summary>Gets the rules that suppressed the match.</summary>
        public IReadOnlyList<GrypeIgnoreRule> AppliedIgnoreRules { get; init; } = Array.Empty<GrypeIgnoreRule>();
    }

    /// <summary>
    /// An ignore rule that suppressed a match.
    /// </summary>
    public sealed class GrypeIgnoreRule
    {
        /// <summary>Gets the ignored vulnerability id, if the rule names one.</summary>
        public string Vulnerability { get; init; }

        /// <summary>Gets the reason given for the rule.</summary>
        public string Reason { get; init; }

        /// <summary>Gets the vulnerability namespace the rule applies to.</summary>
        public string Namespace { get; init; }

        /// <summary>Gets the fix state the rule applies to.</summary>
        [JsonPropertyName("fix-state")]
        public string FixState { get; init; }

        /// <summary>Gets the match type the rule applies to, for example <c>exact-indirect-match</c>.</summary>
        [JsonPropertyName("match-type")]
        public string MatchType { get; init; }

        /// <summary>Gets the package the rule applies to.</summary>
        public GrypeIgnoreRulePackage Package { get; init; }
    }

    /// <summary>
    /// The package part of an ignore rule.
    /// </summary>
    public sealed class GrypeIgnoreRulePackage
    {
        /// <summary>Gets the package name.</summary>
        public string Name { get; init; }

        /// <summary>Gets the package version.</summary>
        public string Version { get; init; }

        /// <summary>Gets the package language.</summary>
        public string Language { get; init; }

        /// <summary>Gets the package type, for example <c>deb</c>.</summary>
        public string Type { get; init; }

        /// <summary>Gets the package location glob.</summary>
        public string Location { get; init; }

        /// <summary>Gets the upstream package name.</summary>
        [JsonPropertyName("upstream-name")]
        public string UpstreamName { get; init; }
    }
}
