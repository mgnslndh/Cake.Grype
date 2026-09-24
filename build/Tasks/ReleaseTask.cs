using Cake.Common;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    /// <summary>
    /// Creates the GitHub Release for the pushed tag with generated notes and the package attached.
    /// A tag containing '-' (e.g. v1.2.0-preview.1) becomes a prerelease that is not marked latest.
    /// </summary>
    [TaskName("Release")]
    [IsDependentOn(typeof(PublishTask))]
    public sealed class ReleaseTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var package = context.ResolveReleasePackage();
            var tag = context.GitHubRefName;

            var arguments = new ProcessArgumentBuilder()
                .Append("release")
                .Append("create")
                .Append(tag)
                .AppendQuoted(package.FullPath)
                .Append("--generate-notes");

            if (tag.Contains('-'))
            {
                arguments.Append("--prerelease").Append("--latest=false");
            }
            else
            {
                arguments.Append("--verify-tag").Append("--fail-on-no-commits");
            }

            // StartProcess does not go through a shell, so the package is passed as an explicit path (no globs).
            var exitCode = context.StartProcess("gh", new ProcessSettings { Arguments = arguments });
            if (exitCode != 0)
            {
                throw new CakeException($"gh release create failed (exit code {exitCode}).");
            }
        }
    }
}
