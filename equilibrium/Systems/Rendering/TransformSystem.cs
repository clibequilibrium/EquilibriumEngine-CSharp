using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Bgfx;
using Equilibrium.Components;
using static Bgfx.bgfx;
using Transform = Equilibrium.Components.Transform;

namespace Equilibrium.Systems.Rendering;

public partial class TransformSystem : BaseSystem<World, float>, IGameSystem
{
    public TransformSystem(World world) : base(world)
    {

    }

    [Query]
    [Any<Position, Rotation, Scale>, None<Transform>()]
    private void AddTransform(in Entity entity)
    {
        entity.Add(new Transform());
    }

    [Query]
    [Any<Position, Rotation, Scale>, All<Transform>()]
    private void UpdateTransform(in Entity entity, ref Transform transform)
    {
        transform.Value = Matrix4x4.Identity;

        if (entity.Has<Position>())
            transform.Value.Translation = entity.Get<Position>().Value;

        if (entity.Has<Rotation>())
            transform.Value = Matrix4x4.Transform(transform.Value, entity.Get<Rotation>().Value);

        if (entity.Has<Scale>())
            transform.Value = Matrix4x4.CreateScale(entity.Get<Scale>().Value) * transform.Value;
    }
}