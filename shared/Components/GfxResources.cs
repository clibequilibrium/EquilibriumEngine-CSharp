using System.Reflection;
using Arch.Core;

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
    public int ComponentId;
    public string FieldName;
}

public struct ReloadShader
{ }