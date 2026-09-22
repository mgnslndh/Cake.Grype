using System;
using System.Text.Json.Serialization;

namespace Cake.Grype.Db
{
    /// <summary>
    /// The result of <c>grype db check -o json</c>.
    /// </summary>
    public sealed class GrypeDbCheckResult
    {
        /// <summary>Gets a value indicating whether a newer database is available.</summary>
        public bool UpdateAvailable { get; init; }

        /// <summary>Gets the installed database, or <c>null</c> if none is installed.</summary>
        [JsonPropertyName("currentDB")]
        public GrypeDbDescription Current { get; init; }

        /// <summary>Gets the available newer database, or <c>null</c> if the installed one is current.</summary>
        [JsonPropertyName("candidateDB")]
        public GrypeDbDescription Candidate { get; init; }
    }

    /// <summary>
    /// A vulnerability database version.
    /// </summary>
    public sealed class GrypeDbDescription
    {
        /// <summary>Gets the schema version, for example <c>v6.1.9</c>.</summary>
        public string SchemaVersion { get; init; }

        /// <summary>Gets when the database was built.</summary>
        public DateTimeOffset? Built { get; init; }

        /// <summary>Gets the archive name (candidate databases only).</summary>
        public string Path { get; init; }

        /// <summary>Gets the archive checksum, for example <c>sha256:…</c> (candidate databases only).</summary>
        public string Checksum { get; init; }
    }
}
