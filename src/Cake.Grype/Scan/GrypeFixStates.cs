using System;

namespace Cake.Grype.Scan
{
    /// <summary>
    /// Fix states whose matches Grype ignores (<c>--ignore-states</c>).
    /// </summary>
    [Flags]
    public enum GrypeFixStates
    {
        /// <summary>No fix states are ignored.</summary>
        None = 0,

        /// <summary>A fix is available (<c>fixed</c>).</summary>
        Fixed = 1,

        /// <summary>No fix is available yet (<c>not-fixed</c>).</summary>
        NotFixed = 2,

        /// <summary>The fix state is unknown (<c>unknown</c>).</summary>
        Unknown = 4,

        /// <summary>The vendor will not fix it (<c>wont-fix</c>).</summary>
        WontFix = 8,
    }
}
