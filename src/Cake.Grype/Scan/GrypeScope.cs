namespace Cake.Grype.Scan
{
    /// <summary>
    /// Which container image layers Grype analyzes (<c>-s</c>).
    /// </summary>
    public enum GrypeScope
    {
        /// <summary>The squashed final image (<c>squashed</c>), Grype's default.</summary>
        Squashed,

        /// <summary>All layers (<c>all-layers</c>).</summary>
        AllLayers,

        /// <summary>Deep squashed (<c>deep-squashed</c>).</summary>
        DeepSquashed,
    }
}
