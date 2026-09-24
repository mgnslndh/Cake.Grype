using Cake.Common.IO;
using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxDotNet;
using Cake.Frosting;

namespace Cake.Grype.Dogfooding.Build.Tasks
{
    /// <summary>
    /// Generates a CycloneDX SBOM of the whole solution (add-in, tests, dogfood project) with Cake.CycloneDX.
    /// </summary>
    [TaskName("Generate-Sbom")]
    public sealed class GenerateSbomTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.EnsureDirectoryExists(context.OutputDirectory);
            context.CdxDotNet(new FilePath("Cake.Grype.sln"), new CdxDotNetSettings
            {
                ComponentName = "Cake.Grype",
                ComponentType = CdxComponentClassification.Library,
                OutputFormat = CdxDotNetOutputFormat.Json,
                Output = context.OutputDirectory,
                FileName = context.SbomFile.GetFilename().FullPath,
            });
        }
    }
}
