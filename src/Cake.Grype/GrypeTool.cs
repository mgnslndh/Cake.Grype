using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Grype
{
    /// <summary>
    /// Base class for the Grype commands.
    /// </summary>
    /// <typeparam name="TSettings">The settings type.</typeparam>
    public abstract class GrypeTool<TSettings> : Tool<TSettings>
        where TSettings : GrypeSettings
    {
        private const int OutputExcerptLength = 500;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly ICakeEnvironment _environment;
        private string _standardOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeTool{TSettings}" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="processRunner">The process runner.</param>
        /// <param name="tools">The tool locator.</param>
        protected GrypeTool(
            IFileSystem fileSystem,
            ICakeEnvironment environment,
            IProcessRunner processRunner,
            IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
            _environment = environment;
        }

        /// <summary>
        /// Gets the name of the tool.
        /// </summary>
        /// <returns>The name of the tool.</returns>
        protected override string GetToolName()
        {
            return "Grype";
        }

        /// <summary>
        /// Gets the possible names of the tool executable.
        /// </summary>
        /// <returns>The tool executable names.</returns>
        protected override IEnumerable<string> GetToolExecutableNames()
        {
            return new[] { "grype.exe", "grype" };
        }

        /// <summary>
        /// Creates a <see cref="ProcessArgumentBuilder"/> containing the command words followed by the global flags.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="command">The command words, for example <c>db</c>, <c>update</c>; none for a scan.</param>
        /// <returns>The argument builder.</returns>
        protected ProcessArgumentBuilder CreateArgumentBuilder(TSettings settings, params string[] command)
        {
            return CreateArgumentBuilder(settings, false, command);
        }

        /// <summary>
        /// Gets the absolute directory that relative paths are resolved against: the settings' working directory
        /// if set, otherwise the Cake working directory.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The absolute working directory.</returns>
        protected DirectoryPath ResolveWorkingDirectory(TSettings settings)
        {
            return GetWorkingDirectory(settings).MakeAbsolute(_environment);
        }

        /// <summary>
        /// Makes a path absolute against <see cref="ResolveWorkingDirectory"/>.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The absolute path.</returns>
        protected FilePath MakeAbsolute(FilePath path, TSettings settings)
        {
            return path.MakeAbsolute(ResolveWorkingDirectory(settings));
        }

        /// <summary>
        /// Runs a command with <c>-q</c> and <c>-o json</c>, and parses its standard output.
        /// </summary>
        /// <remarks>
        /// Standard output is read before <see cref="ToolSettings.PostAction"/> runs, because redirected output can only
        /// be read once. The exit code is checked (see <see cref="AcceptsExitCode"/>) before the output is parsed.
        /// </remarks>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="settings">The settings.</param>
        /// <param name="command">The command words.</param>
        /// <returns>The parsed result.</returns>
        protected T RunAndReadJson<T>(TSettings settings, params string[] command)
            where T : class
        {
            var arguments = CreateArgumentBuilder(settings, true, command);
            arguments.Append("-o");
            arguments.Append("json");

            _standardOutput = null;
            Run(settings, arguments, new ProcessSettings { RedirectStandardOutput = true }, process =>
            {
                _standardOutput = string.Join("\n", process.GetStandardOutput() ?? Enumerable.Empty<string>());
                settings.PostAction?.Invoke(process);
            });

            if (TryParseJson(_standardOutput, out T result))
            {
                return result;
            }

            throw new CakeException($"{GetToolName()}: The output is not valid JSON: {Excerpt(_standardOutput)}");
        }

        /// <summary>
        /// Determines whether a non-zero exit code is expected for this command and must not throw.
        /// </summary>
        /// <param name="exitCode">The non-zero exit code.</param>
        /// <param name="standardOutput">The captured standard output, or <c>null</c> if it was not redirected.</param>
        /// <returns><c>true</c> to accept the exit code.</returns>
        protected virtual bool AcceptsExitCode(int exitCode, string standardOutput)
        {
            return false;
        }

        /// <inheritdoc />
        protected sealed override void ProcessExitCode(int exitCode)
        {
            if (exitCode != 0 && AcceptsExitCode(exitCode, _standardOutput))
            {
                return;
            }

            base.ProcessExitCode(exitCode);
        }

        /// <summary>
        /// Parses JSON without throwing.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="json">The JSON text.</param>
        /// <param name="value">The parsed value, or <c>null</c>.</param>
        /// <returns><c>true</c> if the text is a JSON value of the requested shape.</returns>
        protected static bool TryParseJson<T>(string json, out T value)
            where T : class
        {
            value = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return value != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private ProcessArgumentBuilder CreateArgumentBuilder(TSettings settings, bool quiet, string[] command)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var builder = new ProcessArgumentBuilder();
            foreach (var word in command)
            {
                builder.Append(word);
            }

            foreach (var configFile in settings.ConfigFiles ?? Enumerable.Empty<FilePath>())
            {
                if (configFile == null)
                {
                    continue;
                }

                builder.Append("-c");
                builder.AppendQuoted(MakeAbsolute(configFile, settings).FullPath);
            }

            foreach (var profile in settings.Profiles ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(profile))
                {
                    continue;
                }

                builder.Append("--profile");
                builder.AppendQuoted(profile);
            }

            if (settings.Quiet || quiet)
            {
                builder.Append("-q");
            }

            switch (settings.Verbosity)
            {
                case GrypeVerbosity.Info:
                    builder.Append("-v");
                    break;
                case GrypeVerbosity.Debug:
                    builder.Append("-vv");
                    break;
            }

            return builder;
        }

        private static string Excerpt(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return "(no output)";
            }

            return output.Length <= OutputExcerptLength ? output : output.Substring(0, OutputExcerptLength) + "...";
        }
    }
}
