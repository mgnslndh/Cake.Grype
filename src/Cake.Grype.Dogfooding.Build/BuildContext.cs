using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Cake.Grype.Dogfooding.Build
{
    public sealed class BuildContext : FrostingContext
    {
        public BuildContext(ICakeContext context)
            : base(context)
        {
            OutputDirectory = context.Environment.WorkingDirectory.Combine("artifacts/dogfood");
            SbomFile = OutputDirectory.CombineWithFilePath("Cake.Grype.cdx.json");
            ReportFile = OutputDirectory.CombineWithFilePath("grype.json");

            var grypePath = context.Environment.GetEnvironmentVariable("GRYPE_PATH");
            GrypePath = string.IsNullOrWhiteSpace(grypePath) ? null : new FilePath(grypePath);
        }

        /// <summary>Gets the directory the SBOM and the Grype report are written to.</summary>
        public DirectoryPath OutputDirectory { get; }

        /// <summary>Gets the CycloneDX SBOM of the solution.</summary>
        public FilePath SbomFile { get; }

        /// <summary>Gets Grype's JSON report.</summary>
        public FilePath ReportFile { get; }

        /// <summary>Gets the Grype executable from GRYPE_PATH, or <c>null</c> to resolve Grype from PATH.</summary>
        public FilePath GrypePath { get; }
    }
}
