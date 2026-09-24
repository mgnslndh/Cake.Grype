using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    /// <summary>
    /// Smoke-tests the packed Cake.Grype with the .NET Tool runner (Cake.Tool + #addin) and the Cake SDK runner
    /// (file-based #:package), as Cake's add-in best practices ask (§5.1). The scripts in tests/Smoke are templates
    /// rendered into artifacts/smoke with the package version and the local feed.
    /// </summary>
    [TaskName("Smoke")]
    [IsDependentOn(typeof(PackTask))]
    public sealed class SmokeTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var package = context.GetFiles(context.PackagePattern).Single();
            var version = SmokeTestTemplate.GetPackageVersion(package.GetFilename().FullPath);
            var root = context.Environment.WorkingDirectory;
            var templates = root.Combine("tests/Smoke");
            var smoke = context.ArtifactsDirectory.Combine("smoke");

            context.EnsureDirectoryExists(smoke);
            context.CleanDirectory(smoke);

            var values = new Dictionary<string, string>
            {
                ["VERSION"] = version,
                ["FEED"] = context.ArtifactsDirectory.FullPath,
                ["FEED_URI"] = new Uri(context.ArtifactsDirectory.FullPath).AbsoluteUri,
                ["PACKAGES"] = smoke.Combine("packages").FullPath,
                ["FIXTURE"] = root.CombineWithFilePath("tests/Cake.Grype.Tests/TestData/grype-report.json").FullPath,
            };

            foreach (var template in context.GetFiles(templates.FullPath + "/**/*"))
            {
                var target = smoke.CombineWithFilePath(templates.GetRelativePath(template));
                context.EnsureDirectoryExists(target.GetDirectory());
                File.WriteAllText(target.FullPath, SmokeTestTemplate.Render(File.ReadAllText(template.FullPath), values));
            }

            context.Information("Smoke-testing Cake.Grype {0} with the .NET Tool runner", version);
            RunDotNet(context, smoke.Combine("tool"), "tool", "restore");
            RunDotNet(context, smoke.Combine("tool"), "cake", "build.cake");

            context.Information("Smoke-testing Cake.Grype {0} with the Cake SDK runner", version);
            RunDotNet(context, smoke.Combine("sdk"), "cake.cs");
        }

        private static void RunDotNet(ICakeContext context, DirectoryPath workingDirectory, params string[] arguments)
        {
            var builder = new ProcessArgumentBuilder();
            foreach (var argument in arguments)
            {
                builder.Append(argument);
            }

            var exitCode = context.StartProcess("dotnet", new ProcessSettings { Arguments = builder, WorkingDirectory = workingDirectory });
            if (exitCode != 0)
            {
                throw new CakeException($"'dotnet {builder.Render()}' in {workingDirectory} failed (exit code {exitCode}).");
            }
        }
    }
}
