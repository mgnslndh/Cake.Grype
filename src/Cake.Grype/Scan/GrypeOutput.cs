using Cake.Core.IO;

namespace Cake.Grype.Scan
{
    /// <summary>
    /// One Grype report output (<c>-o format</c> or <c>-o format=file</c>). Several outputs can be written by one scan,
    /// for example the table to the terminal and JSON to a file.
    /// </summary>
    public sealed class GrypeOutput
    {
        private GrypeOutput(GrypeOutputFormat format, FilePath file)
        {
            Format = format;
            File = file;
        }

        /// <summary>Gets the report format.</summary>
        public GrypeOutputFormat Format { get; }

        /// <summary>Gets the file the report is written to, or <c>null</c> for standard output.</summary>
        public FilePath File { get; }

        /// <summary>The table format.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput Table(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.Table, file);

        /// <summary>Grype's native JSON format.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput Json(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.Json, file);

        /// <summary>The CycloneDX XML format.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput CycloneDx(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.CycloneDx, file);

        /// <summary>The CycloneDX JSON format.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput CycloneDxJson(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.CycloneDxJson, file);

        /// <summary>The SARIF format.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput Sarif(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.Sarif, file);

        /// <summary>The Go template format; set <see cref="GrypeScanSettings.Template"/> too.</summary>
        /// <param name="file">The file to write to; standard output if <c>null</c>.</param>
        /// <returns>The output.</returns>
        public static GrypeOutput Template(FilePath file = null) => new GrypeOutput(GrypeOutputFormat.Template, file);
    }
}
