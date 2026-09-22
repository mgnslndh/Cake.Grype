using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Db
{
    /// <summary>
    /// Runs <c>grype db status</c>.
    /// </summary>
    public sealed class GrypeDbStatusReader : GrypeTool<GrypeDbStatusSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeDbStatusReader" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeDbStatusReader(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Reads the database status. A missing or invalid database is returned with <see cref="GrypeDbStatus.Valid"/>
        /// <c>false</c> instead of throwing.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The status.</returns>
        public GrypeDbStatus Read(GrypeDbStatusSettings settings)
        {
            return RunAndReadJson<GrypeDbStatus>(settings, "db", "status");
        }

        /// <inheritdoc />
        protected override bool AcceptsExitCode(int exitCode, string standardOutput)
        {
            // Grype exits 1 when the database is missing or invalid, but still prints the status.
            return exitCode == 1
                && TryParseJson(standardOutput, out GrypeDbStatus status)
                && !status.Valid;
        }
    }
}
