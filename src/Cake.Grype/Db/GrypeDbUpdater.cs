using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Db
{
    /// <summary>
    /// Runs <c>grype db update</c>: downloads and installs the latest vulnerability database.
    /// </summary>
    public sealed class GrypeDbUpdater : GrypeTool<GrypeDbUpdateSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeDbUpdater" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeDbUpdater(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Updates the vulnerability database.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public void Update(GrypeDbUpdateSettings settings)
        {
            Run(settings, CreateArgumentBuilder(settings, "db", "update"));
        }
    }
}
