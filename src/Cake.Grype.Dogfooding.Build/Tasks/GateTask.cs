using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;
using Cake.Grype.Json;

namespace Cake.Grype.Dogfooding.Build.Tasks
{
    /// <summary>
    /// Fails the build on known-exploited findings, or High/Critical findings that have a fix.
    /// Unknown (unassessed) findings are reported but never block.
    /// </summary>
    [TaskName("Gate")]
    [IsDependentOn(typeof(ScanTask))]
    public sealed class GateTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var report = context.GrypeReadJson(context.ReportFile);

            foreach (var count in report.CountBySeverity())
            {
                context.Information("{0}: {1}", count.Key, count.Value);
            }

            context.Information(
                "Unassessed (Unknown severity, not gated): {0}",
                report.Matches.Count(match => match.Severity == GrypeSeverity.Unknown));

            var blocking = report.Matches.Where(IsBlocking).ToList();
            foreach (var match in blocking)
            {
                context.Error(
                    "{0} {1}: {2} ({3}, risk {4:0.0}{5})",
                    match.Artifact?.Name,
                    match.Artifact?.Version,
                    match.Vulnerability?.Id,
                    match.Severity,
                    match.Risk,
                    match.IsKnownExploited ? ", known exploited" : string.Empty);
            }

            if (blocking.Count > 0)
            {
                throw new CakeException($"{blocking.Count} blocking vulnerabilities in Cake.Grype's dependencies");
            }

            context.Information("No blocking vulnerabilities.");
        }

        private static bool IsBlocking(GrypeMatch match)
        {
            return match.IsKnownExploited
                || (match.Severity >= GrypeSeverity.High && match.Vulnerability?.Fix?.State == GrypeFixState.Fixed);
        }
    }
}
