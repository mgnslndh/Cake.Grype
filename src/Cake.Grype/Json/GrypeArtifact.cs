using System;
using System.Collections.Generic;

namespace Cake.Grype.Json
{
    /// <summary>
    /// The package a vulnerability was found in.
    /// </summary>
    public sealed class GrypeArtifact
    {
        /// <summary>Gets Grype's package id.</summary>
        public string Id { get; init; }

        /// <summary>Gets the package name.</summary>
        public string Name { get; init; }

        /// <summary>Gets the installed version.</summary>
        public string Version { get; init; }

        /// <summary>Gets the package type, for example <c>deb</c>, <c>npm</c> or <c>nuget</c>.</summary>
        public string Type { get; init; }

        /// <summary>Gets the package language, if any.</summary>
        public string Language { get; init; }

        /// <summary>Gets the package URL.</summary>
        public string Purl { get; init; }

        /// <summary>Gets the declared licenses.</summary>
        public IReadOnlyList<string> Licenses { get; init; } = Array.Empty<string>();

        /// <summary>Gets the CPEs used for matching.</summary>
        public IReadOnlyList<string> Cpes { get; init; } = Array.Empty<string>();
    }
}
