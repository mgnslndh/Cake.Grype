using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks
{
    /// <summary>
    /// Pushes the package that matches the pushed tag to nuget.org. Requires NUGET_API_KEY and GITHUB_REF_NAME,
    /// and a package produced by a previous Pack in the same checkout.
    /// </summary>
    [TaskName("Publish")]
    public sealed class PublishTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var package = context.ResolveReleasePackage();
            if (string.IsNullOrWhiteSpace(context.NuGetApiKey))
            {
                throw new CakeException("NUGET_API_KEY environment variable is not set.");
            }

            context.DotNetNuGetPush(package, new DotNetNuGetPushSettings
            {
                ApiKey = context.NuGetApiKey,
                Source = "https://api.nuget.org/v3/index.json",
                SkipDuplicate = true,
            });
        }
    }
}
