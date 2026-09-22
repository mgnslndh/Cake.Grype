using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype
{
    /// <summary>
    /// Runs <c>grype version</c> and returns the parsed version information.
    /// </summary>
    public sealed class GrypeVersionReader : GrypeTool<GrypeVersionSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeVersionReader" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeVersionReader(
            IFileSystem fileSystem,
            ICakeEnvironment environment,
            IProcessRunner processRunner,
            IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Reads Grype's version information.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The version information.</returns>
        public GrypeVersion Read(GrypeVersionSettings settings)
        {
            return RunAndReadJson<GrypeVersion>(settings, "version");
        }
    }
}
