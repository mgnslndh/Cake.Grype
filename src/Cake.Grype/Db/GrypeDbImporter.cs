using System;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Db
{
    /// <summary>
    /// Runs <c>grype db import</c>: imports a vulnerability database archive from a file or URL.
    /// </summary>
    public sealed class GrypeDbImporter : GrypeTool<GrypeDbImportSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeDbImporter" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeDbImporter(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Imports a database archive from disk.
        /// </summary>
        /// <param name="archive">The archive; relative paths are resolved against the working directory.</param>
        /// <param name="settings">The settings.</param>
        public void Import(FilePath archive, GrypeDbImportSettings settings)
        {
            ArgumentNullException.ThrowIfNull(archive);

            var arguments = CreateArgumentBuilder(settings, "db", "import");
            arguments.AppendQuoted(MakeAbsolute(archive, settings).FullPath);
            Run(settings, arguments);
        }

        /// <summary>
        /// Imports a database archive from a URL. A <c>checksum=sha256:…</c> query parameter is verified by Grype.
        /// </summary>
        /// <param name="url">The absolute archive URL.</param>
        /// <param name="settings">The settings.</param>
        public void Import(Uri url, GrypeDbImportSettings settings)
        {
            ArgumentNullException.ThrowIfNull(url);
            if (!url.IsAbsoluteUri)
            {
                throw new ArgumentException("The database URL must be absolute.", nameof(url));
            }

            var arguments = CreateArgumentBuilder(settings, "db", "import");
            arguments.AppendQuoted(url.OriginalString);
            Run(settings, arguments);
        }
    }
}
