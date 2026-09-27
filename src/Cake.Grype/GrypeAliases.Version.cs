using System;
using Cake.Core;
using Cake.Core.Annotations;

namespace Cake.Grype
{
    /// <summary>
    /// Contains functionality for running Anchore Grype, a vulnerability scanner for SBOMs, container images and
    /// file systems. Grype must be installed (for example <c>winget install Anchore.Grype</c>) or
    /// <see cref="Cake.Core.Tooling.ToolSettings.ToolPath"/> must be set.
    /// </summary>
    [CakeAliasCategory("Grype")]
    public static partial class GrypeAliases
    {
        /// <summary>
        /// Gets Grype's version information.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The version information.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">
        /// Grype could not be found, it exited with a non-zero exit code, or its output is not valid JSON.
        /// </exception>
        /// <example>
        /// <code>
        /// var version = GrypeVersion();
        /// Information("Grype {0} (DB schema {1})", version.Version, version.SupportedDbSchema);
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        public static GrypeVersion GrypeVersion(this ICakeContext context)
        {
            return context.GrypeVersion(null);
        }

        /// <summary>
        /// Gets Grype's version information using the specified settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The version information.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="CakeException">
        /// Grype could not be found, it exited with a non-zero exit code, or its output is not valid JSON.
        /// </exception>
        /// <example>
        /// <code>
        /// var version = GrypeVersion(new GrypeVersionSettings { ToolPath = "./tools/grype/grype.exe" });
        /// Information("Grype {0}", version.Version);
        /// </code>
        /// </example>
        [CakeMethodAlias]
        [CakeAliasCategory("Grype")]
        [CakeNamespaceImport("Cake.Grype")]
        public static GrypeVersion GrypeVersion(this ICakeContext context, GrypeVersionSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);

            settings ??= new GrypeVersionSettings();
            return new GrypeVersionReader(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools)
                .Read(settings);
        }
    }
}
