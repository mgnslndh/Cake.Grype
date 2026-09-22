namespace Cake.Grype
{
    /// <summary>
    /// Version information reported by <c>grype version -o json</c>.
    /// </summary>
    public sealed class GrypeVersion
    {
        /// <summary>Gets the application name (<c>grype</c>).</summary>
        public string Application { get; init; }

        /// <summary>Gets the Grype version, for example <c>0.119.0</c>.</summary>
        public string Version { get; init; }

        /// <summary>Gets the build date as reported by Grype (RFC 3339 for release builds).</summary>
        public string BuildDate { get; init; }

        /// <summary>Gets the git commit Grype was built from.</summary>
        public string GitCommit { get; init; }

        /// <summary>Gets the git description, for example <c>v0.119.0</c>.</summary>
        public string GitDescription { get; init; }

        /// <summary>Gets the platform, for example <c>windows/amd64</c>.</summary>
        public string Platform { get; init; }

        /// <summary>Gets the Go version Grype was built with.</summary>
        public string GoVersion { get; init; }

        /// <summary>Gets the Go compiler.</summary>
        public string Compiler { get; init; }

        /// <summary>Gets the version of the embedded Syft library.</summary>
        public string SyftVersion { get; init; }

        /// <summary>Gets the vulnerability database schema version this Grype supports.</summary>
        public int? SupportedDbSchema { get; init; }
    }
}
