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

public partial class LightSystem : BaseSystem<World, float>, IRenderSystem
{
    private QueryDescription lightsQuery;

    public LightSystem(World world) : base(world)
    {
        lightsQuery = new QueryDescription().WithAll<PointLight>();
    }

    [Query]
    [All<LightShader>()]
    private void BindPointLights(in LightShader lightShader)
    {
        int lightsCount = World.CountEntities(in lightsQuery);

        // TODO: ambient light entity
        Vector4 ambient_light_irradiance = new(0.25f, 0.25f, 0.25f, 1.0f);

        unsafe
        {
            bgfx.set_uniform(lightShader.AmbientLightIrradianceUniform, &ambient_light_irradiance, ushort.MaxValue);

            // a 32-bit IEEE 754 float can represent all integers up to 2^24 (~16.7
            // million) correctly should be enough for this use case (comparison in
            // for loop)
            float* lightCountVec = stackalloc[] { (float)lightsCount, 0, 0, 0 };

            bgfx.set_uniform(lightShader.LightCountVecUniform, lightCountVec, ushort.MaxValue);
            bgfx.set_compute_dynamic_vertex_buffer(RendererConstants.LIGHTS_POINTLIGHTS, lightShader.BufferHandle, Access.Read);
        }
    }

    [Query]
    [All<FrameData>, None<LightShader>]
    private void InitializeLightShader(in Entity entity)
    {
        LightShader lightShader = default;
        lightShader.VertexLayout = lightShader.VertexLayout.Begin().Add(Attrib.TexCoord0, 4, AttribType.Float).Add(Attrib.TexCoord1, 4, AttribType.Float).End();


        lightShader.LightCountVecUniform = World.CreateUniform("u_lightCountVec", UniformType.Vec4);
        lightShader.AmbientLightIrradianceUniform = World.CreateUniform("u_ambientLightIrradiance", UniformType.Vec4);
        lightShader.BufferHandle = World.CreateDynamicVertexBuffer(lightShader.VertexLayout, BufferFlags.ComputeRead | BufferFlags.AllowResize);

        entity.Add(lightShader);

        // finish any queued precomputations before rendering the scene
        bgfx.frame(false);
        Console.WriteLine("Light System initialized.");
    }
}