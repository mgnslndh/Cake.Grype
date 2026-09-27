using System.Runtime.CompilerServices;

// Namespaces cannot be documented in the XML documentation file format, so each documented namespace
// has an internal NamespaceDoc class, following the Cake documentation guidelines.
namespace Cake.Grype
{
    /// <summary>
    /// Contains aliases for running Anchore Grype, a vulnerability scanner for SBOMs, container images and file
    /// systems, and the types shared by all Grype commands: the global settings, sources and severities.
    /// </summary>
    [CompilerGenerated]
    internal class NamespaceDoc
    {
    }
}

namespace Cake.Grype.Scan
{
    /// <summary>
    /// Contains the settings and tool for scanning a source for vulnerabilities with Grype, and the output formats a
    /// scan can write.
    /// </summary>
    [CompilerGenerated]
    internal class NamespaceDoc
    {
    }
}

namespace Cake.Grype.Db
{
    /// <summary>
    /// Contains the settings, tools and results for managing Grype's vulnerability database: update, import, delete,
    /// status and check.
    /// </summary>
    [CompilerGenerated]
    internal class NamespaceDoc
    {
    }
}

namespace Cake.Grype.Json
{
    /// <summary>
    /// Contains the model of Grype's JSON report, read with <c>GrypeReadJson</c>, for gating a build in C# on
    /// severity, KEV, EPSS, CVSS or risk.
    /// </summary>
    [CompilerGenerated]
    internal class NamespaceDoc
    {
    }
}
