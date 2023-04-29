using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Bgfx;
using Equilibrium.Components;
using static Bgfx.bgfx;

namespace Equilibrium.Systems.Rendering;

public partial class LightBufferUpdateSystem : BaseSystem<World, float>, IGameSystem
{
    private struct PointLightVertex
    {
        public Vector3 Position;
#pragma warning disable 0169
        private float padding;
#pragma warning restore 0169

        public Vector3 Intensity;
        public float Radius;
    }

    private QueryDescription lightShaderQuery;
    private QueryDescription pointLightsQuery;

    public LightBufferUpdateSystem(World world) : base(world)
    {
        lightShaderQuery = new QueryDescription().WithAll<LightShader>();
        pointLightsQuery = new QueryDescription().WithAll<PointLight>();
    }

    public override void Update(in float t)
    {
        base.Update(t);

        int lightsCount = World.CountEntities(in pointLightsQuery);

        if (lightsCount == 0)
            return;

        World.Query(in lightShaderQuery, (in Entity entity, ref LightShader lightShader) =>
        {
            unsafe
            {
                ushort stride = lightShader.VertexLayout.stride;
                Memory* mem = bgfx.alloc((uint)(stride * lightsCount)); // todo improve max

                int i = 0;
                World.Query(in pointLightsQuery, (in Entity entity, ref PointLight pointLight) =>
                       {
                           PointLightVertex* light = (PointLightVertex*)(mem->data + (i * stride));
                           light->Position = pointLight.Position;

                           // intensity = flux per unit solid angle (steradian)
                           // there are 4*pi steradians in a sphere

                           light->Intensity = pointLight.Flux / (4.0f * MathF.PI);
                           light->Radius = CalculatePointLightRadius(in pointLight);

                           i++;
                       });

                bgfx.update_dynamic_vertex_buffer(lightShader.BufferHandle, 0, mem);
            }
        });
    }

    public static float CalculatePointLightRadius(in PointLight pointLight)
    {
        // radius = where attenuation would lead to an intensity of 1W/m^2
        const float INTENSITY_CUTOFF = 1.0f;
        const float ATTENTUATION_CUTOFF = 0.05f;
        Vector3 intensity = pointLight.Flux / (4.0f * MathF.PI);

        float maxIntensity = intensity.Max();
        float attenuation = Math.Max(INTENSITY_CUTOFF, ATTENTUATION_CUTOFF * maxIntensity) / maxIntensity;

        return (float)(1.0d / Math.Sqrt(attenuation));
    }
}