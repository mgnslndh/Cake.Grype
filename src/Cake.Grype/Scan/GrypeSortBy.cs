namespace Cake.Grype.Scan
{
    /// <summary>
    /// How Grype sorts matches (<c>--sort-by</c>).
    /// </summary>
    public enum GrypeSortBy
    {
        /// <summary>By package (<c>package</c>).</summary>
        Package,

        /// <summary>By severity (<c>severity</c>).</summary>
        Severity,

        /// <summary>By EPSS score (<c>epss</c>).</summary>
        Epss,

        /// <summary>By Grype's risk score (<c>risk</c>), Grype's default.</summary>
        Risk,

        /// <summary>Known exploited vulnerabilities first (<c>kev</c>).</summary>
        Kev,

        /// <summary>By vulnerability id (<c>vulnerability</c>).</summary>
        Vulnerability,
    }
}
