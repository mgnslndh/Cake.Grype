using Cake.Frosting;
using Cake.Grype.Scan;

namespace Cake.Grype.Dogfooding.Build.Tasks
{
    /// <summary>
    /// Scans the SBOM with Grype: the table goes to the log, the JSON report to a file (the CI artifact).
    /// </summary>
    [TaskName("Scan")]
    [IsDependentOn(typeof(GenerateSbomTask))]
    public sealed class ScanTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.GrypeScanSbom(context.SbomFile, new GrypeScanSettings
            {
                ToolPath = context.GrypePath,
                Outputs = { GrypeOutput.Table(), GrypeOutput.Json(context.ReportFile) },
                SortBy = GrypeSortBy.Risk,
            });
        }
    }
}
