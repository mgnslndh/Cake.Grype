using System;
using System.IO;
using System.Text.Json;
using Cake.Core;
using Cake.Core.IO;

namespace Cake.Grype.Json
{
    /// <summary>
    /// Reads Grype JSON reports (<c>-o json</c>).
    /// </summary>
    /// <remarks>
    /// The report is deserialized from a stream; fields the model does not map (for example <c>matchDetails</c>,
    /// artifact <c>locations</c> or <c>descriptor.configuration</c>) are skipped, which keeps memory low for large reports.
    /// </remarks>
    public sealed class GrypeReportReader
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new NullAsEmptyListConverterFactory(),
                new GrypeSeverityConverter(),
                new GrypeFixStateConverter(),
            },
        };

        private readonly IFileSystem _fileSystem;
        private readonly ICakeEnvironment _environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrypeReportReader" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="environment">The environment.</param>
        public GrypeReportReader(IFileSystem fileSystem, ICakeEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(fileSystem);
            ArgumentNullException.ThrowIfNull(environment);

            _fileSystem = fileSystem;
            _environment = environment;
        }

        /// <summary>
        /// Reads a report file.
        /// </summary>
        /// <param name="path">The report file; relative paths are resolved against the working directory.</param>
        /// <returns>The report.</returns>
        public GrypeReport Read(FilePath path)
        {
            ArgumentNullException.ThrowIfNull(path);

            var file = _fileSystem.GetFile(path.MakeAbsolute(_environment));
            if (!file.Exists)
            {
                throw new FileNotFoundException(
                    $"The Grype report '{file.Path.FullPath}' could not be found.",
                    file.Path.FullPath);
            }

            using var stream = file.OpenRead();
            return Parse(stream);
        }

        /// <summary>
        /// Parses a report from a stream of UTF-8 JSON (a byte order mark is allowed).
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The report.</returns>
        public static GrypeReport Parse(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            try
            {
                return JsonSerializer.Deserialize<GrypeReport>(stream, Options) ?? new GrypeReport();
            }
            catch (JsonException exception)
            {
                throw new CakeException("Grype: The JSON report is not valid: " + exception.Message, exception);
            }
        }
    }
}
