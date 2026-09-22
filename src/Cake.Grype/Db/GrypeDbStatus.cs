using System;

namespace Cake.Grype.Db
{
    /// <summary>
    /// The local vulnerability database status reported by <c>grype db status -o json</c>.
    /// </summary>
    public sealed class GrypeDbStatus
    {
        /// <summary>Gets the database schema version, for example <c>v6.1.9</c>; empty when no database exists.</summary>
        public string SchemaVersion { get; init; }

        /// <summary>Gets the URL the database was downloaded from.</summary>
        public string From { get; init; }

        /// <summary>Gets when the database was built.</summary>
        public DateTimeOffset? Built { get; init; }

        /// <summary>Gets the database file path.</summary>
        public string Path { get; init; }

        /// <summary>Gets a value indicating whether the database is present and valid.</summary>
        public bool Valid { get; init; }

        /// <summary>Gets the reason the database is not valid, for example <c>database does not exist</c>.</summary>
        public string Error { get; init; }
    }
}
