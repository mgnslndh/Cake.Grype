using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype.Scan
{
    /// <summary>
    /// Runs a Grype vulnerability scan.
    /// </summary>
    public sealed class GrypeScanner : GrypeTool<GrypeScanSettings>
    {
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeScanner" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        public GrypeScanner(
            IFileSystem fileSystem,
            ICakeEnvironment environment,
            IProcessRunner processRunner,
            IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Scans a source for vulnerabilities.
        /// </summary>
        /// <remarks>
        /// Existing output files are deleted before the scan, so a failed scan cannot leave a stale report behind,
        /// and missing output directories are created.
        /// </remarks>
        /// <param name="source">The source to scan.</param>
        /// <param name="settings">The settings.</param>
        public void Scan(GrypeSource source, GrypeScanSettings settings)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(settings);

            if (settings.FailOn == GrypeSeverity.Unknown)
            {
                throw new ArgumentException(
                    "FailOn cannot be Unknown: Grype only accepts negligible, low, medium, high or critical.",
                    nameof(settings));
            }

            if (settings.FailOn.HasValue && !Enum.IsDefined(settings.FailOn.Value))
            {
                throw new ArgumentException("FailOn is not a defined GrypeSeverity value.", nameof(settings));
            }

            if (settings.SortBy.HasValue && !Enum.IsDefined(settings.SortBy.Value))
            {
                throw new ArgumentException("SortBy is not a defined GrypeSortBy value.", nameof(settings));
            }

            if (settings.Scope.HasValue && !Enum.IsDefined(settings.Scope.Value))
            {
                throw new ArgumentException("Scope is not a defined GrypeScope value.", nameof(settings));
            }

            var workingDirectory = ResolveWorkingDirectory(settings);
            var arguments = CreateArgumentBuilder(settings);

            foreach (var output in settings.Outputs ?? Enumerable.Empty<GrypeOutput>())
            {
                if (output == null)
                {
                    continue;
                }

                arguments.Append("-o");
                if (output.File == null)
                {
                    arguments.Append(ToArgument(output.Format));
                }
                else
                {
                    var file = output.File.MakeAbsolute(workingDirectory);
                    PrepareOutputFile(file);
                    arguments.AppendQuoted(ToArgument(output.Format) + "=" + file.FullPath);
                }
            }

            if (settings.OutputFile != null)
            {
                var file = settings.OutputFile.MakeAbsolute(workingDirectory);
                PrepareOutputFile(file);
                arguments.Append("--file");
                arguments.AppendQuoted(file.FullPath);
            }

            if (settings.FailOn.HasValue)
            {
                arguments.Append("-f");
                arguments.Append(settings.FailOn.Value.ToString().ToLowerInvariant());
            }

            if (settings.Template != null)
            {
                arguments.Append("-t");
                arguments.AppendQuoted(settings.Template.MakeAbsolute(workingDirectory).FullPath);
            }

            if (settings.SortBy.HasValue)
            {
                arguments.Append("--sort-by");
                arguments.Append(settings.SortBy.Value.ToString().ToLowerInvariant());
            }

            if (settings.OnlyFixed)
            {
                arguments.Append("--only-fixed");
            }

            if (settings.OnlyNotFixed)
            {
                arguments.Append("--only-notfixed");
            }

            if (settings.IgnoreStates != GrypeIgnoreStates.None)
            {
                arguments.Append("--ignore-states");
                arguments.Append(ToArgument(settings.IgnoreStates));
            }

            if (settings.ByCve)
            {
                arguments.Append("--by-cve");
            }

            if (settings.AddCpesIfNone)
            {
                arguments.Append("--add-cpes-if-none");
            }

            AppendValue(arguments, "--distro", settings.Distro);
            AppendValue(arguments, "--platform", settings.Platform);

            if (settings.Scope.HasValue)
            {
                arguments.Append("-s");
                arguments.Append(ToArgument(settings.Scope.Value));
            }

            foreach (var exclude in settings.Exclude ?? Enumerable.Empty<string>())
            {
                AppendValue(arguments, "--exclude", exclude);
            }

            foreach (var from in settings.From ?? Enumerable.Empty<string>())
            {
                AppendValue(arguments, "--from", from);
            }

            AppendValue(arguments, "--name", settings.Name);

            foreach (var vex in settings.Vex ?? Enumerable.Empty<FilePath>())
            {
                if (vex == null)
                {
                    continue;
                }

                arguments.Append("--vex");
                arguments.AppendQuoted(vex.MakeAbsolute(workingDirectory).FullPath);
            }

            if (settings.ShowSuppressed)
            {
                arguments.Append("--show-suppressed");
            }

            arguments.AppendQuoted(source.ToArgument(workingDirectory));

            Run(settings, arguments);
        }

        private void PrepareOutputFile(FilePath file)
        {
            var existing = _fileSystem.GetFile(file);
            if (existing.Exists)
            {
                existing.Delete();
            }

            var directory = _fileSystem.GetDirectory(file.GetDirectory());
            if (!directory.Exists)
            {
                directory.Create();
            }
        }

        private static void AppendValue(ProcessArgumentBuilder arguments, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            arguments.Append(name);
            arguments.AppendQuoted(value);
        }

        private static string ToArgument(GrypeOutputFormat format)
        {
            return format switch
            {
                GrypeOutputFormat.Table => "table",
                GrypeOutputFormat.Json => "json",
                GrypeOutputFormat.CycloneDx => "cyclonedx",
                GrypeOutputFormat.CycloneDxJson => "cyclonedx-json",
                GrypeOutputFormat.Sarif => "sarif",
                GrypeOutputFormat.Template => "template",
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
            };
        }

        private static string ToArgument(GrypeScope scope)
        {
            return scope switch
            {
                GrypeScope.Squashed => "squashed",
                GrypeScope.AllLayers => "all-layers",
                GrypeScope.DeepSquashed => "deep-squashed",
                _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
            };
        }

        private static string ToArgument(GrypeIgnoreStates states)
        {
            var values = new List<string>();
            if (states.HasFlag(GrypeIgnoreStates.Fixed))
            {
                values.Add("fixed");
            }

            if (states.HasFlag(GrypeIgnoreStates.NotFixed))
            {
                values.Add("not-fixed");
            }

            if (states.HasFlag(GrypeIgnoreStates.Unknown))
            {
                values.Add("unknown");
            }

            if (states.HasFlag(GrypeIgnoreStates.WontFix))
            {
                values.Add("wont-fix");
            }

            return string.Join(",", values);
        }
    }
}
