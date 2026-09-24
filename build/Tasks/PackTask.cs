using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks
{
    /// <summary>
    /// Packs Cake.Grype into ./artifacts (deleting older packages first) and verifies the package content.
    /// </summary>
    [TaskName("Pack")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PackTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.EnsureDirectoryExists(context.ArtifactsDirectory);
            context.DeleteFiles(context.PackagePattern);

            context.DotNetPack(BuildContext.LibraryProject, new DotNetPackSettings
            {
                Configuration = BuildContext.BuildConfiguration,
                NoBuild = true,
                OutputDirectory = context.ArtifactsDirectory,
                Verbosity = DotNetVerbosity.Minimal,
            });

            var packages = context.GetFiles(context.PackagePattern).ToList();
            if (packages.Count != 1)
            {
                throw new CakeException($"Expected exactly one package in {context.ArtifactsDirectory}, found {packages.Count}.");
            }

            var problems = PackageVerifier.Verify(packages[0].FullPath);
            if (problems.Count > 0)
            {
                throw new CakeException($"Package {packages[0].GetFilename()} is invalid: {string.Join("; ", problems)}");
            }

            context.Information("Verified {0}", packages[0].GetFilename());
        }
    }
}
