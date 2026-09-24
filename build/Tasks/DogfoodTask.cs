using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Run;
using Cake.Frosting;

namespace Build.Tasks
{
    /// <summary>
    /// Runs the dogfood project: Cake.CycloneDX SBOM of the solution, Grype scan with the current Cake.Grype, C# gate.
    /// </summary>
    [TaskName("Dogfood")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class DogfoodTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.DotNetRun(BuildContext.DogfoodProject, new DotNetRunSettings
            {
                Configuration = BuildContext.BuildConfiguration,
                NoBuild = true,
                NoRestore = true,
            });
        }
    }
}
