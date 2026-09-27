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
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">Grype could not be found, or it exited with a non-zero exit code.</exception>
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
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">Grype could not be found, or it exited with a non-zero exit code.</exception>
        /// <example>
        /// <code>
        /// GrypeDbUpdate(new GrypeDbUpdateSettings
        /// {
        ///     EnvironmentVariables = { ["GRYPE_DB_CACHE_DIR"] = MakeAbsolute(Directory("./.cache/grype")).FullPath },
        /// });
        /// </code>
        /// </example>
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
        /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="archive"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">Grype could not be found, or it exited with a non-zero exit code.</exception>
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
        /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="archive"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">Grype could not be found, or it exited with a non-zero exit code.</exception>
        /// <example>
        /// <code>
        /// GrypeDbImport("./cache/vulnerability-db.tar.zst", new GrypeDbImportSettings
        /// {
        ///     Verbosity = GrypeVerbosity.Info,
        /// });
        /// </code>
        /// </example>
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
        /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="url"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="url"/> is not absolute.</exception>
        /// <exception cref="CakeException">Grype could not be found, or it exited with a non-zero exit code.</exception>
        /// <example>
        /// <code>
        /// GrypeDbImport(new Uri("https://grype.anchore.io/databases/...?checksum=sha256:..."));
        /// </code>
        /// </example>
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
        /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="url"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="url"/> is not absolute.</exception>
        /// <exception cref="CakeException">Grype could not be found, or it exited with a non-zero exit code.</exception>
        /// <example>
        /// <code>
        /// GrypeDbImport(new Uri("https://grype.anchore.io/databases/...?checksum=sha256:..."), new GrypeDbImportSettings
        /// {
        ///     EnvironmentVariables = { ["GRYPE_DB_CACHE_DIR"] = MakeAbsolute(Directory("./.cache/grype")).FullPath },
        /// });
        /// </code>
        /// </example>
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
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">Grype could not be found, or it exited with a non-zero exit code.</exception>
        /// <example>
        /// <code>
        /// GrypeDbDelete();
        /// </code>
        /// </example>
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
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">Grype could not be found, or it exited with a non-zero exit code.</exception>
        /// <example>
        /// <code>
        /// GrypeDbDelete(new GrypeDbDeleteSettings
        /// {
        ///     EnvironmentVariables = { ["GRYPE_DB_CACHE_DIR"] = MakeAbsolute(Directory("./.cache/grype")).FullPath },
        /// });
        /// </code>
        /// </example>
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

        /// <summary>
        /// Gets the status of the local Grype vulnerability database. A missing database returns
        /// <c>Valid == false</c> with an <c>Error</c> instead of throwing.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The status.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">
        /// Grype could not be found, it failed for another reason than a missing or invalid database, or its output is
        /// not valid JSON.
        /// </exception>
        /// <example>
        /// <code>
        /// var status = GrypeDbStatus();
        /// if (!status.Valid)
        /// {
        ///     GrypeDbUpdate();
        /// }
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static GrypeDbStatus GrypeDbStatus(this ICakeContext context)
        {
            return context.GrypeDbStatus(null);
        }

        /// <summary>
        /// Gets the status of the local Grype vulnerability database using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The status.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">
        /// Grype could not be found, it failed for another reason than a missing or invalid database, or its output is
        /// not valid JSON.
        /// </exception>
        /// <example>
        /// <code>
        /// var status = GrypeDbStatus(new GrypeDbStatusSettings
        /// {
        ///     EnvironmentVariables = { ["GRYPE_DB_CACHE_DIR"] = MakeAbsolute(Directory("./.cache/grype")).FullPath },
        /// });
        /// Information("Grype DB built {0}, schema {1}", status.Built, status.SchemaVersion);
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static GrypeDbStatus GrypeDbStatus(this ICakeContext context, GrypeDbStatusSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbStatusSettings();
            return new GrypeDbStatusReader(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Read(settings);
        }

        /// <summary>
        /// Checks whether a newer Grype vulnerability database is available.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The result.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">
        /// Grype could not be found, it exited with a non-zero exit code other than the one for an available update, or
        /// its output is not valid JSON.
        /// </exception>
        /// <example>
        /// <code>
        /// if (GrypeDbCheck().UpdateAvailable)
        /// {
        ///     GrypeDbUpdate();
        /// }
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static GrypeDbCheckResult GrypeDbCheck(this ICakeContext context)
        {
            return context.GrypeDbCheck(null);
        }

        /// <summary>
        /// Checks whether a newer Grype vulnerability database is available using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The result.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">
        /// Grype could not be found, it exited with a non-zero exit code other than the one for an available update, or
        /// its output is not valid JSON.
        /// </exception>
        /// <example>
        /// <code>
        /// var check = GrypeDbCheck(new GrypeDbCheckSettings
        /// {
        ///     EnvironmentVariables = { ["GRYPE_DB_CACHE_DIR"] = MakeAbsolute(Directory("./.cache/grype")).FullPath },
        /// });
        /// if (check.UpdateAvailable)
        /// {
        ///     Information("A newer Grype database is available: {0}", check.Candidate?.Built);
        /// }
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        [CakeNamespaceImport("Cake.Grype.Db")]
        public static GrypeDbCheckResult GrypeDbCheck(this ICakeContext context, GrypeDbCheckSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeDbCheckSettings();
            return new GrypeDbChecker(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools).Check(settings);
        }
    }
}
