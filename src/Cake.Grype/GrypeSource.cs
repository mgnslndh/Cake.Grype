using System;
using Cake.Core.IO;

namespace Cake.Grype
{
    /// <summary>
    /// What Grype scans: an SBOM, a directory, a file, a container image, package URLs or CPEs.
    /// Relative paths are made absolute when the scan runs. A <see cref="string"/> converts implicitly and is
    /// passed to Grype unchanged, for example <c>"registry:alpine:3.20"</c>.
    /// </summary>
    public sealed class GrypeSource
    {
        private readonly string _scheme;
        private readonly string _value;
        private readonly FilePath _file;
        private readonly DirectoryPath _directory;

        private GrypeSource(string scheme, string value, FilePath file, DirectoryPath directory)
        {
            _scheme = scheme;
            _value = value;
            _file = file;
            _directory = directory;
        }

        /// <summary>An SBOM file: Syft JSON, CycloneDX or SPDX (<c>sbom:</c>).</summary>
        /// <param name="file">The SBOM file.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Sbom(FilePath file) => FromFile("sbom", file);

        /// <summary>A directory on disk (<c>dir:</c>).</summary>
        /// <param name="directory">The directory.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Directory(DirectoryPath directory) => FromDirectory("dir", directory);

        /// <summary>A single file on disk (<c>file:</c>).</summary>
        /// <param name="file">The file.</param>
        /// <returns>The source.</returns>
        public static GrypeSource File(FilePath file) => FromFile("file", file);

        /// <summary>A container image reference, resolved by Grype's default lookup (a Docker daemon first).</summary>
        /// <param name="reference">The image reference, for example <c>myorg/api:1.2.3</c>.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Image(string reference) => FromValue(null, reference);

        /// <summary>An image from the Docker daemon (<c>docker:</c>).</summary>
        /// <param name="reference">The image reference.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Docker(string reference) => FromValue("docker", reference);

        /// <summary>An image from the Podman daemon (<c>podman:</c>).</summary>
        /// <param name="reference">The image reference.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Podman(string reference) => FromValue("podman", reference);

        /// <summary>An image pulled directly from a registry, no container runtime required (<c>registry:</c>).</summary>
        /// <param name="reference">The image reference.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Registry(string reference) => FromValue("registry", reference);

        /// <summary>A tarball created by <c>docker save</c> (<c>docker-archive:</c>).</summary>
        /// <param name="file">The archive.</param>
        /// <returns>The source.</returns>
        public static GrypeSource DockerArchive(FilePath file) => FromFile("docker-archive", file);

        /// <summary>An OCI archive (<c>oci-archive:</c>).</summary>
        /// <param name="file">The archive.</param>
        /// <returns>The source.</returns>
        public static GrypeSource OciArchive(FilePath file) => FromFile("oci-archive", file);

        /// <summary>An OCI layout directory (<c>oci-dir:</c>).</summary>
        /// <param name="directory">The directory.</param>
        /// <returns>The source.</returns>
        public static GrypeSource OciDirectory(DirectoryPath directory) => FromDirectory("oci-dir", directory);

        /// <summary>A Singularity Image Format container (<c>singularity:</c>).</summary>
        /// <param name="file">The SIF file.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Singularity(FilePath file) => FromFile("singularity", file);

        /// <summary>A file with one package URL per line (<c>purl:</c>).</summary>
        /// <param name="file">The file.</param>
        /// <returns>The source.</returns>
        public static GrypeSource PurlFile(FilePath file) => FromFile("purl", file);

        /// <summary>A single package URL, for example <c>pkg:apk/openssl@3.2.1?distro=alpine-3.20.3</c>.</summary>
        /// <param name="reference">The package URL.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Purl(string reference) => FromValue(null, reference);

        /// <summary>A file with one CPE per line (<c>cpes:</c>).</summary>
        /// <param name="file">The file.</param>
        /// <returns>The source.</returns>
        public static GrypeSource CpeFile(FilePath file) => FromFile("cpes", file);

        /// <summary>A single CPE, for example <c>cpe:2.3:a:openssl:openssl:3.0.14:*:*:*:*:*:*:*</c>.</summary>
        /// <param name="reference">The CPE.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Cpe(string reference) => FromValue(null, reference);

        /// <summary>All SBOMs within a Zarf package archive (<c>zarf:</c>).</summary>
        /// <param name="file">The package archive.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Zarf(FilePath file) => FromFile("zarf", file);

        /// <summary>
        /// A source exactly as Grype expects it on the command line; relative paths are not resolved.
        /// </summary>
        /// <param name="source">The source text, for example <c>registry:alpine:3.20</c>.</param>
        /// <returns>The source.</returns>
        public static GrypeSource Parse(string source)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentException.ThrowIfNullOrWhiteSpace(source);
            return new GrypeSource(null, source, null, null);
        }

        /// <summary>
        /// Converts source text to a <see cref="GrypeSource"/> (see <see cref="Parse"/>).
        /// </summary>
        /// <param name="source">The source text.</param>
        public static implicit operator GrypeSource(string source) => Parse(source);

        /// <summary>
        /// Renders the command-line argument, making relative paths absolute.
        /// </summary>
        /// <param name="workingDirectory">The absolute directory relative paths are resolved against.</param>
        /// <returns>The argument.</returns>
        public string ToArgument(DirectoryPath workingDirectory)
        {
            ArgumentNullException.ThrowIfNull(workingDirectory);

            var value = _file != null
                ? _file.MakeAbsolute(workingDirectory).FullPath
                : _directory != null
                    ? _directory.MakeAbsolute(workingDirectory).FullPath
                    : _value;

            return Render(value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Render(_file?.FullPath ?? _directory?.FullPath ?? _value);
        }

        private static GrypeSource FromFile(string scheme, FilePath file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return new GrypeSource(scheme, null, file, null);
        }

        private static GrypeSource FromDirectory(string scheme, DirectoryPath directory)
        {
            ArgumentNullException.ThrowIfNull(directory);
            return new GrypeSource(scheme, null, null, directory);
        }

        private static GrypeSource FromValue(string scheme, string reference)
        {
            ArgumentNullException.ThrowIfNull(reference);
            ArgumentException.ThrowIfNullOrWhiteSpace(reference);
            return new GrypeSource(scheme, reference, null, null);
        }

        private string Render(string value)
        {
            return _scheme == null ? value : _scheme + ":" + value;
        }
    }
}
