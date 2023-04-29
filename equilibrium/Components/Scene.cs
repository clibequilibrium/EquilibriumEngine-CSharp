using System.Numerics;
using System.Runtime.InteropServices;
using static Bgfx.bgfx;

namespace Equilibrium.Components;

public struct Camera
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Quaternion InverseRotation;
    public Vector3 Up;
    public float Fov;
    public float Near;
    public float Far;
    public bool Ortho;
    public Matrix4x4 View;
    public Matrix4x4 Proj;
}

public struct PointLight
{
    public Vector3 Position;
    public Vector3 Flux;
}

public struct AmbientLight
{
    public Vector3 Irradiance;
}

public struct Material
{
    public bool Blend;
    public bool DoubleSided;
    public TextureHandle BaseColorTexture;
    public Vector4 BaseColorFactor;

    public TextureHandle MetallicRoughnessTexture; // blue = metallic, green = roughness
    public float MetallicFactor;
    public float RoughnessFactor;

    public TextureHandle NormalTexture;
    public float NormalScale;

    public TextureHandle OcclusionTexture;
    public float OcclusionStrength;

    public TextureHandle EmissiveTexture;
    public Vector3 EmissiveFactor;

    public Material()
    {
        TextureHandle invalid = new TextureHandle { idx = BgfxConstants.InvalidHandle };

        Blend = false;
        DoubleSided = false;

        BaseColorTexture = invalid;
        BaseColorFactor = Vector4.One;

        MetallicRoughnessTexture = invalid; // blue = metallic; green = roughness
        MetallicFactor = 0.0f;
        RoughnessFactor = 1.0f;

        NormalTexture = invalid;
        NormalScale = 1.0f;

        OcclusionTexture = invalid;
        OcclusionStrength = 1.0f;

        EmissiveTexture = invalid;
        EmissiveFactor = Vector3.Zero;
    }
}

public struct Mesh
{
    public VertexBufferHandle VertexBuffer;
    public IndexBufferHandle IndexBuffer;
}