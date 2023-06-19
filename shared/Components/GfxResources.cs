using Arch.Core;
using Arch.Core.Utils;

public enum ResourceType
{
    Invalid,
    Texture,
    VertexBuffer,
    DynamicVertexBuffer,
    IndexBuffer,
    Program,
    FrameBuffer,
    Uniform,
}

public struct GfxResource
{
    public ResourceType Type;
    public ushort Handle;
}

public struct HotReloadableShader
{
    public ComponentType ComponentType;
    public Entity Entity;
}