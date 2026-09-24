using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName("All")]
    [IsDependentOn(typeof(TestTask))]
    [IsDependentOn(typeof(PackTask))]
    [IsDependentOn(typeof(DogfoodTask))]
    public sealed class AllTask : FrostingTask
    {
    }
}
