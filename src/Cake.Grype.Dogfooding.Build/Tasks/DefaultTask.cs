using Cake.Frosting;

namespace Cake.Grype.Dogfooding.Build.Tasks
{
    [TaskName("Default")]
    [IsDependentOn(typeof(GateTask))]
    public sealed class DefaultTask : FrostingTask
    {
    }
}
