namespace Cake.Grype
{
    /// <summary>
    /// Grype's log verbosity (<c>-v</c>, <c>-vv</c>). Logs are written to standard error.
    /// </summary>
    public enum GrypeVerbosity
    {
        /// <summary>Grype's default verbosity; no flag is passed.</summary>
        Default,

        /// <summary>Informational logging (<c>-v</c>).</summary>
        Info,

        /// <summary>Debug logging (<c>-vv</c>).</summary>
        Debug,
    }
}
