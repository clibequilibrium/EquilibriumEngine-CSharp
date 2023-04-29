using System.Runtime.InteropServices;
using static Bgfx.bgfx;

namespace Equilibrium.Components;

public struct PosVertex
{
    public float X;
    public float Y;
    public float Z;

    public PosVertex(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public struct TextureBuffer
{
    public TextureHandle Handle;
}

public struct LightShader
{
    public UniformHandle LightCountVecUniform;
    public UniformHandle AmbientLightIrradianceUniform;
    public DynamicVertexBufferHandle BufferHandle;
    public VertexLayout VertexLayout;
}

public struct ForwardRenderer
{
    [Shader("vs_forward", "fs_forward")]
    public ProgramHandle Program;
}

[StructLayout(LayoutKind.Sequential)]
public struct DeferredRenderer
{
    public VertexBufferHandle PointLightVertexBuffer;
    public IndexBufferHandle PointLightIndexBuffer;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)GBufferAttachment.GBufferAttachmentCount + 1)]
    public TextureBuffer[] GBufferTextures;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)GBufferAttachment.GBufferAttachmentCount)]
    public byte[] GBufferTextureUnits;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)GBufferAttachment.GBufferAttachmentCount)]
    public string[] GBufferSamplerNames;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)GBufferAttachment.GBufferAttachmentCount)]
    public UniformHandle[] GBufferSamplers;

    public FrameBufferHandle GBuffer;

    public TextureHandle LightDepthTexture;
    public FrameBufferHandle AccumFrameBuffer;

    public UniformHandle LightIndexVecUniform;

    public ProgramHandle GeometryProgram;
    public ProgramHandle FullscreenProgram;
    public ProgramHandle PointLightProgram;
    public ProgramHandle TransparencyProgram;
}

public struct PBRShader
{
    public UniformHandle BaseColorFactorUniform;
    public UniformHandle MetallicRoughnessNormalOcclusionFactorUniform;
    public UniformHandle EmissiveFactorUniform;
    public UniformHandle HasTexturesUniform;
    public UniformHandle MultipleScatteringUniform;
    public UniformHandle AlbedoLutSampler;
    public UniformHandle BaseColorSampler;
    public UniformHandle MetallicRoughnessSampler;
    public UniformHandle NormalSampler;
    public UniformHandle OcclusionSampler;
    public UniformHandle EmissiveSampler;

    public TextureHandle AlbedoLutTexture;
    public TextureHandle DefaultTexture;

    [Shader("cs_multiple_scattering_lut")]
    public ProgramHandle AlbedoLutProgram;
}

public struct FrameData
{
    public nint TextureBuffers;
    public FrameBufferHandle FrameBuffer;
    public VertexBufferHandle BlitTriangleBuffer;
    
    [Shader("vs_tonemap", "fs_tonemap")]
    public ProgramHandle BlitProgram;
    public UniformHandle BlitSampler;
    public UniformHandle CamPosUniform;
    public UniformHandle NormalMatrixUniform;
    public UniformHandle ExposureVecUniform;
    public UniformHandle TonemappingModeVecUniform;
}

// public struct FrameDataInitialized
// {
// }

public enum GBufferAttachment
{
    // no world position
    // gl_Fragcoord is enough to unproject

    // RGB = diffuse
    // A = a (remapped roughness)
    Diffuse_A,

    // RG = encoded normal
    Normal,

    // RGB = F0 (Fresnel at normal incidence)
    // A = metallic
    // TODO? don't use F0, calculate from diffuse and metallic in shader
    //       where do we store metallic?
    F0_Metallic,

    // RGB = emissive radiance
    // A = occlusion multiplier
    EmissiveOcclusion,

    G_Depth,

    GBufferAttachmentCount
}

public class RendererConstants
{
    public const byte PBR_ALBEDO_LUT = 0;

    public const byte PBR_BASECOLOR = 1;
    public const byte PBR_METALROUGHNESS = 2;
    public const byte PBR_NORMAL = 3;
    public const byte PBR_OCCLUSION = 4;
    public const byte PBR_EMISSIVE = 5;

    public const byte LIGHTS_POINTLIGHTS = 6;

    public const byte CLUSTERS_CLUSTERS = 7;
    public const byte CLUSTERS_LIGHTINDICES = 8;
    public const byte CLUSTERS_LIGHTGRID = 9;
    public const byte CLUSTERS_ATOMICINDEX = 10;

    public const byte DEFERRED_DIFFUSE_A = 7;
    public const byte DEFERRED_NORMAL = 8;
    public const byte DEFERRED_F0_METALLIC = 9;
    public const byte DEFERRED_EMISSIVE_OCCLUSION = 10;
    public const byte DEFERRED_DEPTH = 11;

    public const byte ALBEDO_LUT_SIZE = 32;
    public const byte ALBEDO_LUT_THREADS = 32;

    public static readonly TextureFormat[] BufferAttachmentFormats = new TextureFormat[(int)GBufferAttachment.GBufferAttachmentCount - 1]
    { TextureFormat.BGRA8, TextureFormat.RG16F, TextureFormat.BGRA8, TextureFormat.BGRA8 };

    public static readonly SamplerFlags BufferSamplerFlags = SamplerFlags.MinPoint | SamplerFlags.MagPoint |
                                          SamplerFlags.MipPoint | SamplerFlags.UClamp |
                                          SamplerFlags.VClamp;
}