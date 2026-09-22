using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Db
{
    /// <summary>
    /// Runs <c>grype db delete</c>: deletes the local vulnerability database.
    /// </summary>
    public sealed class GrypeDbDeleter : GrypeTool<GrypeDbDeleteSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeDbDeleter" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeDbDeleter(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Deletes the vulnerability database.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public void Delete(GrypeDbDeleteSettings settings)
        {
            Run(settings, CreateArgumentBuilder(settings, "db", "delete"));
        }
    }
}
