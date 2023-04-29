
using Arch.Core;
using Arch.System;
using Equilibrium.Components;

namespace Equilibrium.Systems;

public partial class BootstrapSystem : BaseSystem<World, float>, IGameSystem
{
    public BootstrapSystem(World world) : base(world) { }

    public override void Initialize()
    {
        base.Initialize();

        AssimpUtils.LoadScene(World, "Sponza");

        World.Create(new PointLight { Position = new(-5.0f, 1.3f, 0.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(-5.0f, 1.3f, 0.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(0.0f, 1.3f, 0.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(5.0f, 1.3f, 0.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(10.0f, 1.3f, 0.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(-10.0f, 1.3f, 0.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(-5.0f, 5, -3.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(0.0f, 5, -3.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(5.0f, 5, -3.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(10.0f, 5, -3.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(-10.0f, 5, -3.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(-5.0f, 5, 3.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(0.0f, 5, 3.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(5.0f, 5, 3.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(10.0f, 5, 3.0f), Flux = new(100, 100, 100) });
        World.Create(new PointLight { Position = new(-10.0f, 5, 3.0f), Flux = new(100, 100, 100) });
    }
}