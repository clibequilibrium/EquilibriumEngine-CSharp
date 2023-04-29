using System.Runtime.CompilerServices;
using static Bgfx.bgfx;

public static class BgfxConstants
{
    public const ushort InvalidHandle = UInt16.MaxValue;
}

public static class BgfxExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Destroy(this TextureHandle handle) => destroy_texture(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Destroy(this ProgramHandle handle) => destroy_program(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Destroy(this ShaderHandle handle) => destroy_shader(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Destroy(this UniformHandle handle) => destroy_uniform(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Destroy(this FrameBufferHandle handle) => destroy_frame_buffer(handle);

    public static VertexLayout Begin(this VertexLayout vertexLayout)
    {
        unsafe
        {
            vertex_layout_begin(&vertexLayout, get_renderer_type());
            return vertexLayout;
        }
    }

    public static VertexLayout Add(this VertexLayout vertexLayout, Attrib atrib, byte num, AttribType type, bool normalized = false, bool asInt = false)
    {
        unsafe
        {
            vertex_layout_add(&vertexLayout, atrib, num, type, normalized, asInt);
            return vertexLayout;
        }
    }


    public static VertexLayout End(this VertexLayout vertexLayout)
    {
        unsafe
        {
            vertex_layout_end(&vertexLayout);
            return vertexLayout;
        }
    }

    public static string BytesToString(long byteCount)
    {
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
        if (byteCount == 0)
            return "0" + suf[0];
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString() + suf[place];
    }
}
