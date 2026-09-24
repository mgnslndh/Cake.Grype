using System.Linq;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    public sealed class BuildContext : FrostingContext
    {
        public const string BuildConfiguration = "Release";
        public const string Solution = "Cake.Grype.sln";
        public const string LibraryProject = "src/Cake.Grype/Cake.Grype.csproj";
        public const string DogfoodProject = "src/Cake.Grype.Dogfooding.Build/Cake.Grype.Dogfooding.Build.csproj";

        public BuildContext(ICakeContext context)
            : base(context)
        {
            ArtifactsDirectory = context.Environment.WorkingDirectory.Combine("artifacts");
        }

        /// <summary>Gets the directory packages are written to.</summary>
        public DirectoryPath ArtifactsDirectory { get; }

        /// <summary>Gets the glob matching packages in <see cref="ArtifactsDirectory"/>.</summary>
        public string PackagePattern => ArtifactsDirectory.CombineWithFilePath("*.nupkg").FullPath;

        public string NuGetApiKey => Environment.GetEnvironmentVariable("NUGET_API_KEY");

        public string GitHubRefName => Environment.GetEnvironmentVariable("GITHUB_REF_NAME");

        /// <summary>
        /// Gets the package matching the pushed tag: GITHUB_REF_NAME <c>v1.2.3</c> requires
        /// <c>artifacts/Cake.Grype.1.2.3.nupkg</c>. Guarantees the published version equals the tag.
        /// </summary>
        /// <returns>The package path.</returns>
        public FilePath ResolveReleasePackage()
        {
            var tag = GitHubRefName;
            if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith('v'))
            {
                throw new CakeException($"GITHUB_REF_NAME must be a version tag like v1.2.3 (was '{tag}').");
            }

            var expected = ArtifactsDirectory.CombineWithFilePath($"Cake.Grype.{tag.Substring(1)}.nupkg");
            if (!this.FileExists(expected))
            {
                var found = string.Join(", ", this.GetFiles(PackagePattern).Select(file => file.GetFilename().FullPath));
                throw new CakeException(
                    $"Tag {tag} requires {expected.GetFilename()}, but artifacts contains: {(found.Length == 0 ? "(nothing)" : found)}.");
            }

            this.Information("Release package: {0}", expected.GetFilename());
            return expected;
        }

        /// <summary>
        /// Gets whether the GitHub Release for the tag is missing, a draft or published.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>The state.</returns>
        public GitHubReleaseState GetGitHubReleaseState(string tag)
        {
            var exitCode = this.StartProcess(
                "gh",
                new ProcessSettings { Arguments = GitHubRelease.View(tag), RedirectStandardOutput = true, RedirectStandardError = true },
                out var output,
                out var error);
            return GitHubRelease.ParseViewResult(exitCode, output, error);
        }

        /// <summary>
        /// Runs the GitHub CLI and throws on a non-zero exit code.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        public void RunGitHubCli(ProcessArgumentBuilder arguments)
        {
            // StartProcess does not go through a shell, so paths are passed explicitly (no globs).
            var exitCode = this.StartProcess("gh", new ProcessSettings { Arguments = arguments });
            if (exitCode != 0)
            {
                throw new CakeException($"'gh {arguments.RenderSafe()}' failed (exit code {exitCode}).");
            }
        }
    }
}
