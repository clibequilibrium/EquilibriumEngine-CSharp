using Arch.Core;
using Equilibrium.Systems;

namespace Equilibrium.Modules;

public class Bootstrap : IModule
{
    List<ISystem> IModule.Initialize(World world)
    {
        return new List<ISystem>
            {
                new BootstrapSystem(world),
            };
    }
}