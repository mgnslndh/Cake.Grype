using System;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.Grype.Scan;

namespace Cake.Grype
{
    public static partial class GrypeAliases
    {
        /// <summary>
        /// Scans a source for vulnerabilities with Grype. A string is passed to Grype unchanged, for example
        /// <c>"registry:alpine:3.20"</c>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="source">The source to scan.</param>
        /// <example>
        /// <code>
        /// GrypeScan(GrypeSource.Sbom("./artifacts/bom.cdx.json"));
        /// GrypeScan("registry:alpine:3.20");
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScan(this ICakeContext context, GrypeSource source)
        {
            context.GrypeScan(source, null);
        }

        /// <summary>
        /// Scans a source for vulnerabilities with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="source">The source to scan.</param>
        /// <param name="settings">The settings.</param>
        /// <example>
        /// <code>
        /// var settings = new GrypeScanSettings
        /// {
        ///     Outputs = { GrypeOutput.Table(), GrypeOutput.Json("./artifacts/grype.json") },
        /// };
        /// foreach (var source in new List&lt;GrypeSource&gt; { GrypeSource.Sbom("./artifacts/api.cdx.json"), GrypeSource.Registry("myorg/api:1.2.3") })
        /// {
        ///     GrypeScan(source, settings);
        /// }
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScan(this ICakeContext context, GrypeSource source, GrypeScanSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeScanSettings();
            new GrypeScanner(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools)
                .Scan(source, settings);
        }

        /// <summary>
        /// Scans an SBOM (Syft JSON, CycloneDX or SPDX) for vulnerabilities with Grype.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sbom">The SBOM file.</param>
        /// <example>
        /// <code>
        /// GrypeScanSbom("./artifacts/bom.cdx.json");
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanSbom(this ICakeContext context, FilePath sbom)
        {
            context.GrypeScanSbom(sbom, null);
        }

        /// <summary>
        /// Scans an SBOM (Syft JSON, CycloneDX or SPDX) for vulnerabilities with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sbom">The SBOM file.</param>
        /// <param name="settings">The settings.</param>
        /// <example>
        /// <code>
        /// GrypeScanSbom("./artifacts/bom.cdx.json", new GrypeScanSettings
        /// {
        ///     Outputs = { GrypeOutput.Table(), GrypeOutput.Json("./artifacts/grype.json") },
        ///     SortBy = GrypeSortBy.Risk,
        /// });
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanSbom(this ICakeContext context, FilePath sbom, GrypeScanSettings settings)
        {
            context.GrypeScan(GrypeSource.Sbom(sbom), settings);
        }

        /// <summary>
        /// Scans a directory for vulnerabilities with Grype.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="directory">The directory.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanDirectory(this ICakeContext context, DirectoryPath directory)
        {
            context.GrypeScanDirectory(directory, null);
        }

        /// <summary>
        /// Scans a directory for vulnerabilities with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="directory">The directory.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanDirectory(this ICakeContext context, DirectoryPath directory, GrypeScanSettings settings)
        {
            context.GrypeScan(GrypeSource.Directory(directory), settings);
        }

        /// <summary>
        /// Scans a single file for vulnerabilities with Grype.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="file">The file.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanFile(this ICakeContext context, FilePath file)
        {
            context.GrypeScanFile(file, null);
        }

        /// <summary>
        /// Scans a single file for vulnerabilities with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="file">The file.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanFile(this ICakeContext context, FilePath file, GrypeScanSettings settings)
        {
            context.GrypeScan(GrypeSource.File(file), settings);
        }

        /// <summary>
        /// Scans a container image for vulnerabilities with Grype, using Grype's default image lookup.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="image">The image reference, for example <c>myorg/api:1.2.3</c>.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanImage(this ICakeContext context, string image)
        {
            context.GrypeScanImage(image, null);
        }

        /// <summary>
        /// Scans a container image for vulnerabilities with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="image">The image reference, for example <c>myorg/api:1.2.3</c>.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanImage(this ICakeContext context, string image, GrypeScanSettings settings)
        {
            context.GrypeScan(GrypeSource.Image(image), settings);
        }

        /// <summary>
        /// Scans a container image pulled directly from a registry (no container runtime required) with Grype.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="image">The image reference, for example <c>myorg/api:1.2.3</c>.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanRegistry(this ICakeContext context, string image)
        {
            context.GrypeScanRegistry(image, null);
        }

        /// <summary>
        /// Scans a container image pulled directly from a registry with Grype using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="image">The image reference, for example <c>myorg/api:1.2.3</c>.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Scan")]
        public static void GrypeScanRegistry(this ICakeContext context, string image, GrypeScanSettings settings)
        {
            context.GrypeScan(GrypeSource.Registry(image), settings);
        }
    }
}
