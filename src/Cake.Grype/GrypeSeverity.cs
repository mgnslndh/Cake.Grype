namespace Cake.Grype
{
    /// <summary>
    /// A vulnerability severity as Grype reports it. Values are ordered, so <c>severity &gt;= GrypeSeverity.High</c> works.
    /// </summary>
    /// <remarks>
    /// <see cref="Unknown"/> means "not assessed yet" (for example a reserved CVE without analysis), not "low".
    /// It ranks below every assessed severity, so severity thresholds, including Grype's own <c>--fail-on</c>,
    /// never match it. Check for it explicitly if unassessed findings should block a build.
    /// </remarks>
    public enum GrypeSeverity
    {
        /// <summary>No severity has been assessed.</summary>
        Unknown = 0,

        /// <summary>Negligible.</summary>
        Negligible = 1,

        /// <summary>Low.</summary>
        Low = 2,

        /// <summary>Medium.</summary>
        Medium = 3,

        /// <summary>High.</summary>
        High = 4,

        /// <summary>Critical.</summary>
        Critical = 5,
    }
}
