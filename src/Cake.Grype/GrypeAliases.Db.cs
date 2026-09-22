using System;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.Grype.Db;

namespace Cake.Grype
{
    public static partial class GrypeAliases
    {
        /// <summary>
        /// Downloads and installs the latest Grype vulnerability database.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <example>
        /// <code>
        /// GrypeDbUpdate();
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbUpdate(this ICakeContext context)
        {
            context.GrypeDbUpdate(null);
        }

        /// <summary>
        /// Downloads and installs the latest Grype vulnerability database using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbUpdate(this ICakeContext context, GrypeDbUpdateSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbUpdateSettings();
            new GrypeDbUpdater(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Update(settings);
        }

        /// <summary>
        /// Imports a Grype vulnerability database archive from disk, for offline or air-gapped builds.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="archive">The archive, for example downloaded from <c>https://grype.anchore.io/databases</c>.</param>
        /// <example>
        /// <code>
        /// GrypeDbImport("./cache/vulnerability-db.tar.zst");
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbImport(this ICakeContext context, FilePath archive)
        {
            context.GrypeDbImport(archive, null);
        }

        /// <summary>
        /// Imports a Grype vulnerability database archive from disk using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="archive">The archive.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbImport(this ICakeContext context, FilePath archive, GrypeDbImportSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbImportSettings();
            new GrypeDbImporter(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Import(archive, settings);
        }

        /// <summary>
        /// Imports a Grype vulnerability database archive from a URL. A <c>checksum=sha256:…</c> query parameter is
        /// verified by Grype.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="url">The absolute archive URL.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbImport(this ICakeContext context, Uri url)
        {
            context.GrypeDbImport(url, null);
        }

        /// <summary>
        /// Imports a Grype vulnerability database archive from a URL using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="url">The absolute archive URL.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbImport(this ICakeContext context, Uri url, GrypeDbImportSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbImportSettings();
            new GrypeDbImporter(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Import(url, settings);
        }

        /// <summary>
        /// Deletes the local Grype vulnerability database, for example to start a test from a clean cache.
        /// </summary>
        /// <param name="context">The context.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbDelete(this ICakeContext context)
        {
            context.GrypeDbDelete(null);
        }

        /// <summary>
        /// Deletes the local Grype vulnerability database using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="settings">The settings.</param>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static void GrypeDbDelete(this ICakeContext context, GrypeDbDeleteSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbDeleteSettings();
            new GrypeDbDeleter(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Delete(settings);
        }
    }
}
