
using System.Numerics;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Equilibrium.Components;
using MathNet.Numerics;

namespace Equilibrium.Systems;

public partial class MoveLightsSystem : BaseSystem<World, float>, IGameSystem
{
    public MoveLightsSystem(World world) : base(world) { }

    [Query]
    [All<PointLight>]
    private void MoveLights(ref PointLight light)
    {
        float angularVelocity = (float)Trig.DegreeToRadian(0.0f);
        float angle = angularVelocity * Data;

        Matrix4x4 matrix = Matrix4x4.Identity;
        matrix *= Matrix4x4.CreateFromAxisAngle(new Vector3(0, 1, 0), angle);
        Vector4 position = new Vector4(light.Position, 1.0f);
        position = Vector4.Transform(position, matrix);

        light.Position = new Vector3(position.X, position.Y, position.Z);
        // light.Flux = new Vector3(255, 0, 0);
    }
}