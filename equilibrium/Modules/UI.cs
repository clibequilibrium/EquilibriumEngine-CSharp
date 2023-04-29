using Arch.Core;
using Equilibrium.Systems.UI.Docking;
using Equilibrium.Systems.UI.Tools;

namespace Equilibrium.Modules;

public class UI : IModule
{
    List<ISystem> IModule.Initialize(World world)
    {
        return new List<ISystem>
            {
                new OverlaySystem(world),
                new EntityInspectorSystem(world),
                new DockspaceSystem(world),
            };
    }
}