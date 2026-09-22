using System.Collections.Generic;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype
{
    /// <summary>
    /// Contains the settings shared by all Grype commands (Grype's global flags).
    /// </summary>
    /// <remarks>
    /// Grype settings that have no command-line flag, such as <c>GRYPE_DB_AUTO_UPDATE</c>,
    /// <c>GRYPE_DB_CACHE_DIR</c> or <c>GRYPE_CHECK_FOR_APP_UPDATE</c>, can be set through
    /// <see cref="ToolSettings.EnvironmentVariables"/>.
    /// </remarks>
    public class GrypeSettings : ToolSettings
    {
        /// <summary>
        /// Gets or sets the Grype configuration files to use (<c>-c</c>, repeatable).
        /// Relative paths are resolved against the working directory.
        /// </summary>
        public ICollection<FilePath> ConfigFiles { get; set; } = new List<FilePath>();

        /// <summary>
        /// Gets or sets the configuration profiles to use (<c>--profile</c>, repeatable).
        /// </summary>
        public ICollection<string> Profiles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether all logging output is suppressed (<c>-q</c>).
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Gets or sets the log verbosity (<c>-v</c> or <c>-vv</c>).
        /// </summary>
        public GrypeVerbosity Verbosity { get; set; }
    }
}
