using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Test;
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName("Test")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class TestTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.DotNetTest(BuildContext.Solution, new DotNetTestSettings
            {
                Configuration = BuildContext.BuildConfiguration,
                PathType = DotNetTestPathType.Solution,
                NoBuild = true,
                Verbosity = DotNetVerbosity.Minimal,
            });
        }
    }
}
