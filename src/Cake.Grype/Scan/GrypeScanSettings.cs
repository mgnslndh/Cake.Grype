using System.Collections.Generic;
using Cake.Core.IO;

namespace Cake.Grype.Scan
{
    /// <summary>
    /// Contains the settings for a Grype scan.
    /// </summary>
    public sealed class GrypeScanSettings : GrypeSettings
    {
        /// <summary>
        /// Gets or sets the report outputs (<c>-o</c>, repeatable). Grype prints the table when none are set.
        /// </summary>
        public ICollection<GrypeOutput> Outputs { get; set; } = new List<GrypeOutput>();

        /// <summary>
        /// Gets or sets the file the default report is written to instead of standard output (<c>--file</c>).
        /// </summary>
        public FilePath OutputFile { get; set; }

        /// <summary>
        /// Gets or sets the severity at or above which Grype exits with code 2 (<c>-f</c>). The exit code throws a
        /// <see cref="Cake.Core.CakeException"/> after the outputs are written; accept it with
        /// <see cref="Cake.Core.Tooling.ToolSettings.HandleExitCode"/> to continue. <see cref="GrypeSeverity.Unknown"/>
        /// is not allowed.
        /// </summary>
        public GrypeSeverity? FailOn { get; set; }

        /// <summary>
        /// Gets or sets the Go template file for the <see cref="GrypeOutputFormat.Template"/> output (<c>-t</c>).
        /// </summary>
        public FilePath Template { get; set; }

        /// <summary>
        /// Gets or sets how matches are sorted (<c>--sort-by</c>).
        /// </summary>
        public GrypeSortBy? SortBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether vulnerabilities without a fix are ignored (<c>--only-fixed</c>).
        /// </summary>
        public bool OnlyFixed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether vulnerabilities with a fix are ignored (<c>--only-notfixed</c>).
        /// </summary>
        public bool OnlyNotFixed { get; set; }

        /// <summary>
        /// Gets or sets the fix states whose matches are ignored (<c>--ignore-states</c>).
        /// </summary>
        public GrypeFixStates IgnoreStates { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether results are oriented by CVE instead of the original vulnerability id
        /// (<c>--by-cve</c>).
        /// </summary>
        public bool ByCve { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether CPEs are generated for packages without CPE data
        /// (<c>--add-cpes-if-none</c>).
        /// </summary>
        public bool AddCpesIfNone { get; set; }

        /// <summary>
        /// Gets or sets the distro to match against, for example <c>debian:12</c> (<c>--distro</c>).
        /// </summary>
        public string Distro { get; set; }

        /// <summary>
        /// Gets or sets the platform for container image sources, for example <c>linux/arm64</c> (<c>--platform</c>).
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Gets or sets the image layers to analyze (<c>-s</c>).
        /// </summary>
        public GrypeScope? Scope { get; set; }

        /// <summary>
        /// Gets or sets glob expressions of paths excluded from the scan (<c>--exclude</c>, repeatable).
        /// </summary>
        public ICollection<string> Exclude { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the source behaviours to use, for example <c>registry</c> (<c>--from</c>, repeatable).
        /// </summary>
        public ICollection<string> From { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the name of the target being analyzed (<c>--name</c>).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets VEX documents to consider (<c>--vex</c>, repeatable).
        /// </summary>
        public ICollection<FilePath> Vex { get; set; } = new List<FilePath>();

        /// <summary>
        /// Gets or sets a value indicating whether suppressed matches are shown; table output only
        /// (<c>--show-suppressed</c>).
        /// </summary>
        public bool ShowSuppressed { get; set; }
    }
}
