using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName("All")]
    [IsDependentOn(typeof(TestTask))]
    [IsDependentOn(typeof(PackTask))]
    [IsDependentOn(typeof(DogfoodTask))]
    [IsDependentOn(typeof(SmokeTask))]
    public sealed class AllTask : FrostingTask
    {
    }
}
