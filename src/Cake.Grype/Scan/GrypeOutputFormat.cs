namespace Cake.Grype.Scan
{
    /// <summary>
    /// A Grype report format (<c>-o</c>).
    /// </summary>
    public enum GrypeOutputFormat
    {
        /// <summary>The human-readable table (<c>table</c>), Grype's default.</summary>
        Table,

        /// <summary>Grype's native JSON (<c>json</c>); readable with <c>GrypeReadJson</c>.</summary>
        Json,

        /// <summary>CycloneDX XML with vulnerabilities (<c>cyclonedx</c>).</summary>
        CycloneDx,

        /// <summary>CycloneDX JSON with vulnerabilities (<c>cyclonedx-json</c>).</summary>
        CycloneDxJson,

        /// <summary>SARIF (<c>sarif</c>), for code-scanning dashboards.</summary>
        Sarif,

        /// <summary>A Go template (<c>template</c>); requires <see cref="GrypeScanSettings.Template"/>.</summary>
        Template,
    }
}
