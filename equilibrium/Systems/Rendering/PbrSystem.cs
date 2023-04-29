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

public partial class PBRSystem : BaseSystem<World, float>, IRenderSystem
{
    const bool multipleScatteringEnabled = true;
    const bool whiteFurnaceEnabled = false;

    const float WHITE_FURNACE_RADIANCE = 1.0f;

    public PBRSystem(World world) : base(world)
    {
    }

    [Query]
    [All<PBRShader>()]
    private void UpdatePBRShader(in PBRShader pbrShader)
    {
        BindAlbedoLut(in pbrShader, false);
    }

    [Query]
    [All<BgfxComponent>, None<PBRShader>]
    private void InitializePBRShader(in Entity entity)
    {
        PBRShader pbrShader = default;

        pbrShader.BaseColorFactorUniform = World.CreateUniform("u_baseColorFactor", UniformType.Vec4);
        pbrShader.MetallicRoughnessNormalOcclusionFactorUniform = World.CreateUniform("u_metallicRoughnessNormalOcclusionFactor", UniformType.Vec4);
        pbrShader.EmissiveFactorUniform = World.CreateUniform("u_emissiveFactorVec", UniformType.Vec4);
        pbrShader.HasTexturesUniform = World.CreateUniform("u_hasTextures", UniformType.Vec4);
        pbrShader.MultipleScatteringUniform = World.CreateUniform("u_multipleScatteringVec", UniformType.Vec4);
        pbrShader.AlbedoLutSampler = World.CreateUniform("s_texAlbedoLUT", UniformType.Sampler);
        pbrShader.BaseColorSampler = World.CreateUniform("s_texBaseColor", UniformType.Sampler);
        pbrShader.MetallicRoughnessSampler = World.CreateUniform("s_texMetallicRoughness", UniformType.Sampler);
        pbrShader.NormalSampler = World.CreateUniform("s_texNormal", UniformType.Sampler);
        pbrShader.OcclusionSampler = World.CreateUniform("s_texOcclusion", UniformType.Sampler);
        pbrShader.EmissiveSampler = World.CreateUniform("s_texEmissive", UniformType.Sampler);

        pbrShader.DefaultTexture = World.CreateTexture2d(1, 1, false, 1, TextureFormat.RGBA8, 0);
        pbrShader.AlbedoLutTexture = World.CreateTexture2d(RendererConstants.ALBEDO_LUT_SIZE, RendererConstants.ALBEDO_LUT_SIZE, false, 1, TextureFormat.RGBA32F,
            (ulong)SamplerFlags.UvwClamp | (ulong)TextureFlags.ComputeWrite);

        World.LoadShaders(ref pbrShader);
        GenerateAlbedoLut(in pbrShader);

        entity.Add(pbrShader);

        // finish any queued precomputations before rendering the scene
        bgfx.frame(false);
        Console.WriteLine("PBR System initialized.");
    }

    static bool set_texture_or_default(in PBRShader pbrShader, byte stage,
                                   UniformHandle uniform, TextureHandle texture)
    {
        bool valid = texture.Valid;

        if (!valid)
        {
            bgfx.set_texture(stage, uniform, pbrShader.DefaultTexture, uint.MaxValue);
        }
        else
        {
            bgfx.set_texture(stage, uniform, texture, uint.MaxValue);
        }

        return valid;
    }

    public unsafe static ulong BindMaterial(in PBRShader pbrShader, in Material material)
    {
        float* factor_values = stackalloc[] {
            material.MetallicFactor,
            material.RoughnessFactor,
            material.NormalScale,
            material.OcclusionStrength
        };

        Vector4 baseColorFactor = material.BaseColorFactor;
        bgfx.set_uniform(pbrShader.BaseColorFactorUniform, &baseColorFactor, ushort.MaxValue);
        bgfx.set_uniform(pbrShader.MetallicRoughnessNormalOcclusionFactorUniform, factor_values, ushort.MaxValue);
        Vector4 emissive_factor = new(material.EmissiveFactor.X, material.EmissiveFactor.Y,
                            material.EmissiveFactor.Z, 0.0f);
        bgfx.set_uniform(pbrShader.EmissiveFactorUniform, &emissive_factor, ushort.MaxValue);

        float* has_textures_values = stackalloc[] { 0.0f, 0.0f, 0.0f, 0.0f };

        int has_texture_mask =
            0 |
            ((set_texture_or_default(in pbrShader, RendererConstants.PBR_BASECOLOR, pbrShader.BaseColorSampler,
                                     material.BaseColorTexture)
                  ? 1
                  : 0)
             << 0) |
            ((set_texture_or_default(in pbrShader, RendererConstants.PBR_METALROUGHNESS,
                                     pbrShader.MetallicRoughnessSampler,
                                     material.MetallicRoughnessTexture)
                  ? 1
                  : 0)
             << 1) |
            ((set_texture_or_default(in pbrShader, RendererConstants.PBR_NORMAL, pbrShader.NormalSampler,
                                     material.NormalTexture)
                  ? 1
                  : 0)
             << 2) |
            ((set_texture_or_default(in pbrShader, RendererConstants.PBR_OCCLUSION, pbrShader.OcclusionSampler,
                                     material.OcclusionTexture)
                  ? 1
                  : 0)
             << 3) |
            ((set_texture_or_default(in pbrShader, RendererConstants.PBR_EMISSIVE, pbrShader.EmissiveSampler,
                                     material.EmissiveTexture)
                  ? 1
                  : 0)
             << 4);

        has_textures_values[0] = (float)((uint)has_texture_mask);

        bgfx.set_uniform(pbrShader.HasTexturesUniform, has_textures_values, ushort.MaxValue);

        float* multiple_scattering_values = stackalloc[] {
            multipleScatteringEnabled ? 1.0f : 0.0f,
            whiteFurnaceEnabled ? WHITE_FURNACE_RADIANCE : 0.0f,
            0.0f,
            0.0f
        };

        bgfx.set_uniform(pbrShader.MultipleScatteringUniform, multiple_scattering_values, ushort.MaxValue);

        ulong state = 0;
        if (material.Blend)
            state |= (ulong)BgfxUtils.BlendAlpha();
        if (!material.DoubleSided)
            state |= (ulong)StateFlags.CullCw;

        return state;
    }

    static void BindAlbedoLut(in PBRShader pbrShader, bool compute)
    {
        if (compute)
        {
            bgfx.set_image(RendererConstants.PBR_ALBEDO_LUT, pbrShader.AlbedoLutTexture, 0, Access.Write, TextureFormat.Count);
        }
        else
        {
            bgfx.set_texture(RendererConstants.PBR_ALBEDO_LUT, pbrShader.AlbedoLutSampler, pbrShader.AlbedoLutTexture, uint.MaxValue);
        }
    }

    private static void GenerateAlbedoLut(in PBRShader pbrShader)
    {
        BindAlbedoLut(in pbrShader, true);
        bgfx.dispatch(0, pbrShader.AlbedoLutProgram, RendererConstants.ALBEDO_LUT_SIZE / RendererConstants.ALBEDO_LUT_THREADS,
                    RendererConstants.ALBEDO_LUT_SIZE / RendererConstants.ALBEDO_LUT_THREADS, 1, byte.MaxValue);
    }
}