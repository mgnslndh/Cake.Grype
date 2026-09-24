using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Build
{
    public sealed class BuildLifetime : FrostingLifetime<BuildContext>
    {
        public override void Setup(BuildContext context, ISetupContext info)
        {
            context.Information("Package version: {0}", ThisAssembly.PackageVersion);
        }

        public override void Teardown(BuildContext context, ITeardownContext info)
        {
        }
    }
}
