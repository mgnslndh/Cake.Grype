using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Db
{
    /// <summary>
    /// Runs <c>grype db check</c>.
    /// </summary>
    public sealed class GrypeDbChecker : GrypeTool<GrypeDbCheckSettings>
    {
        private const int UpdateAvailableExitCode = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeDbChecker" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeDbChecker(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Checks whether a database update is available. An available update is reported through
        /// <see cref="GrypeDbCheckResult.UpdateAvailable"/>, not an exception.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The result.</returns>
        public GrypeDbCheckResult Check(GrypeDbCheckSettings settings)
        {
            return RunAndReadJson<GrypeDbCheckResult>(settings, "db", "check");
        }

        /// <inheritdoc />
        protected override bool AcceptsExitCode(int exitCode, string standardOutput)
        {
            return exitCode == UpdateAvailableExitCode;
        }
    }
}
