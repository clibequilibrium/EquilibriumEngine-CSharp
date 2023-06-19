using System.Reflection;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Bgfx;
using static Bgfx.bgfx;

public static class BgfxUtils
{
    public static VertexBufferHandle CreateVertexBuffer(this World world, nint data, int size, VertexLayout vertexLayout, BufferFlags flags = BufferFlags.None)
    {
        unsafe
        {
            var handle = bgfx.create_vertex_buffer(bgfx.copy(data.ToPointer(), (uint)size), &vertexLayout, (ushort)flags);
            CreateGfxResource(world, ResourceType.VertexBuffer, handle.idx);
            return handle;
        }
    }

    public unsafe static VertexBufferHandle CreateVertexBuffer(this World world, bgfx.Memory* memory, VertexLayout vertexLayout, BufferFlags flags = BufferFlags.None)
    {
        var handle = bgfx.create_vertex_buffer(memory, &vertexLayout, (ushort)flags);
        CreateGfxResource(world, ResourceType.VertexBuffer, handle.idx);
        return handle;
    }

    public static DynamicVertexBufferHandle CreateDynamicVertexBuffer(this World world, VertexLayout vertexLayout, BufferFlags flags = BufferFlags.None, int num = 1)
    {
        unsafe
        {
            var handle = bgfx.create_dynamic_vertex_buffer((uint)num, &vertexLayout, (ushort)flags);
            CreateGfxResource(world, ResourceType.DynamicVertexBuffer, handle.idx);
            return handle;
        }
    }


    public static IndexBufferHandle CreateIndexBuffer(this World world, nint data, int size, BufferFlags flags = BufferFlags.None)
    {
        unsafe
        {
            var handle = bgfx.create_index_buffer(bgfx.copy(data.ToPointer(), (uint)size), (ushort)flags);
            CreateGfxResource(world, ResourceType.IndexBuffer, handle.idx);
            return handle;
        }
    }

    public unsafe static IndexBufferHandle CreateIndexBuffer(this World world, bgfx.Memory* memory, BufferFlags flags = BufferFlags.None)
    {
        var handle = bgfx.create_index_buffer(memory, (ushort)flags);
        CreateGfxResource(world, ResourceType.IndexBuffer, handle.idx);
        return handle;
    }

    public static UniformHandle CreateUniform(this World world, string name, UniformType type, int num = 1)
    {
        unsafe
        {
            var handle = bgfx.create_uniform(name, type, (ushort)num);
            CreateGfxResource(world, ResourceType.Uniform, handle.idx);
            return handle;
        }
    }


    public static TextureHandle CreateTexture2dScaled(this World world, BackbufferRatio ratio, bool hasMips, ushort numLayers, TextureFormat format, ulong flags)
    {
        unsafe
        {
            var handle = bgfx.create_texture_2d_scaled(ratio, hasMips, numLayers, format, flags);
            CreateGfxResource(world, ResourceType.Texture, handle.idx);
            return handle;
        }
    }

    public unsafe static TextureHandle CreateTexture(this World world, Memory* memory, ulong flags)
    {
        unsafe
        {
            var handle = bgfx.create_texture(memory, flags, 0, null);
            CreateGfxResource(world, ResourceType.Texture, handle.idx);
            return handle;
        }
    }

    public static TextureHandle CreateTexture2d(this World world, int width, int height, bool hasMips, ushort numLayers, TextureFormat format, ulong flags, nint memory = default)
    {
        unsafe
        {
            var handle = bgfx.create_texture_2d((ushort)width, (ushort)height, hasMips, numLayers, format, flags, memory == default ? null : (Memory*)memory.ToPointer());
            CreateGfxResource(world, ResourceType.Texture, handle.idx);
            return handle;
        }
    }

    public static FrameBufferHandle CreateFrameBufferFromHandles(this World world, byte num, nint handles)
    {
        unsafe
        {
            var handle = bgfx.create_frame_buffer_from_handles(num, (TextureHandle*)handles.ToPointer(), false);
            CreateGfxResource(world, ResourceType.FrameBuffer, handle.idx);
            return handle;
        }
    }

    private static Entity CreateGfxResource(World world, ResourceType type, ushort handle)
    {
        return world.Create(new GfxResource { Type = type, Handle = handle });
    }

    // public static void LoadMesh(string name)
    // {
    //     string filePath = string.Empty;
    //     string modelPath = @$"{AppContext.BaseDirectory}/content/models";
    //     name = $"{name}.gltf";

    //     filePath = Path.Combine(modelPath, name);

    //     var model = SharpGLTF.Schema2.ModelRoot.Load(filePath);

    //     // Parse materials
    //     for (int i = 0; i < model.LogicalMaterials.Count; i++)
    //     {
    //         var material = model.LogicalMaterials[i];

    //         Console.WriteLine(material.Name);
    //     }

    //     Console.WriteLine(model.DefaultScene);
    // }

    public static ShaderHandle LoadShader(string name)
    {
        string filePath = string.Empty;
        string shaderPath = @$"{AppContext.BaseDirectory}/content/shaders";
        name = $"{name}.bin";

        ShaderHandle invalid = new ShaderHandle { idx = BgfxConstants.InvalidHandle };

        switch (get_renderer_type())
        {
            case RendererType.Noop:
            case RendererType.Direct3D9:
                shaderPath = $"{shaderPath}/dx9/";
                break;

            case RendererType.Direct3D11:
            case RendererType.Direct3D12:
                shaderPath = $"{shaderPath}/dx11/";
                break;

            case RendererType.Gnm:
                shaderPath = $"{shaderPath}/pssl/";
                break;

            case RendererType.Metal:
                shaderPath = $"{shaderPath}/metal/";
                break;

            case RendererType.OpenGL:
                shaderPath = $"{shaderPath}/glsl/";
                break;

            case RendererType.OpenGLES:
                shaderPath = $"{shaderPath}/essl/";
                break;

            case RendererType.Vulkan:
                shaderPath = $"{shaderPath}/spirv/";
                break;

            default:
                return invalid;
        };


        filePath = Path.Combine(shaderPath, name);

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Shader file {name} not found.");
            return invalid;
        }

        long fileSize = new FileInfo(filePath).Length;

        if (fileSize == 0)
        {
            return invalid;
        }

        byte[] shaderBytes = File.ReadAllBytes(filePath);

        unsafe
        {
            fixed (void* ptr = shaderBytes)
            {
                Memory* memory = bgfx.copy(ptr, (uint)fileSize);

                ShaderHandle handle = bgfx.create_shader(memory);

                if (!handle.Valid)
                {
                    Console.Error.WriteLine($"Shader model not supported for {name}");
                    return invalid;
                }

                return handle;
            }
        }
    }

    public static ProgramHandle CreateProgram(this World world, string vertexShaderName, string fragmentShaderName, string name)
    {
        ShaderHandle vertexHandle = LoadShader(vertexShaderName);
        ShaderHandle fragmentHandle = LoadShader(fragmentShaderName);

        var handle = bgfx.create_program(vertexHandle, fragmentHandle, true);
        Entity programEntity = CreateGfxResource(world, ResourceType.Program, handle.idx);
        programEntity.Add(new Name { Value = name });

        return handle;
    }

    public static void LoadShaders<T>(this World world, in Entity entity, ref T component) where T : struct
    {
        var shaderAttributes = GetAttributeListRecursive<ShaderAttribute>(typeof(T));

        foreach (var attribute in shaderAttributes)
        {
            var handle = new ProgramHandle { idx = BgfxConstants.InvalidHandle };

            if (attribute.Item2.ComputeShaderName == null)
            {
                ShaderHandle vertexHandle = LoadShader(attribute.Item2.VertexShaderName!);
                ShaderHandle fragmentHandle = LoadShader(attribute.Item2.FragmentShaderName!);

                handle = bgfx.create_program(vertexHandle, fragmentHandle, true);
            }
            else
            {
                ShaderHandle shaderHandle = LoadShader(attribute.Item2.ComputeShaderName);
                handle = bgfx.create_compute_program(shaderHandle, true);
            }

            Entity programEntity = CreateGfxResource(world, ResourceType.Program, handle.idx);
            programEntity.Add(new Name { Value = typeof(T).Name + "." + attribute.Item1.Name });
            programEntity.Add(new HotReloadableShader { ComponentType = (ComponentType)typeof(T), Entity = entity });

            // Set field value
            attribute.Item1.SetValueDirect(__makeref(component), handle);
        }
    }

    public static bool HotReloadShaders(this Entity entity, ComponentType componentType, string shaderName, ref object component)
    {
        var shaderAttributes = GetAttributeListRecursive<ShaderAttribute>(componentType.Type);
        bool reload = false;

        foreach (var attribute in shaderAttributes)
        {
            if (attribute.Item2.ComputeShaderName == null)
            {
                if (attribute.Item2.VertexShaderName != null && attribute.Item2.VertexShaderName.Equals(shaderName))
                {
                    reload = true;
                    break;
                }

                if (attribute.Item2.FragmentShaderName != null && attribute.Item2.FragmentShaderName.Equals(shaderName))
                {
                    reload = true;
                    break;
                }
            }
            else if (attribute.Item2.ComputeShaderName.Equals(shaderName))
            {
                reload = true;
                break;
            }
        }

        if (!reload)
        {
            return false;
        }

        foreach (var attribute in shaderAttributes)
        {
            var handle = new ProgramHandle { idx = BgfxConstants.InvalidHandle };

            if (attribute.Item2.ComputeShaderName == null)
            {
                ShaderHandle vertexHandle = LoadShader(attribute.Item2.VertexShaderName!);
                ShaderHandle fragmentHandle = LoadShader(attribute.Item2.FragmentShaderName!);

                handle = bgfx.create_program(vertexHandle, fragmentHandle, true);
            }
            else
            {
                ShaderHandle computeShaderHandle = LoadShader(attribute.Item2.ComputeShaderName);
                handle = bgfx.create_compute_program(computeShaderHandle, true);
            }

            // Dealocate memory
            ProgramHandle shaderHandle = (ProgramHandle)attribute.Item1.GetValueDirect(__makeref(component))!;
            bgfx.destroy_program(shaderHandle);

            // Set field value
            attribute.Item1.SetValueDirect(__makeref(component), handle);
        }

        return true;
    }

    private static IEnumerable<(FieldInfo, T)> GetAttributeListRecursive<T>(Type type, HashSet<Type> hashSet = null!) where T : Attribute
    {
        hashSet = hashSet ?? new HashSet<Type>();

        if (!hashSet.Add(type))
        {
            yield break;
        }

        foreach (var field in type.GetFields())
        {
            IEnumerable<T> attributes = field.GetCustomAttributes<T>(true);

            if (attributes.Any())
            {
                yield return (field, attributes.FirstOrDefault()!);
            }

            foreach (var result in GetAttributeListRecursive<T>(field.FieldType, hashSet))
            {
                yield return result;
            }
        }
    }

    public static StateFlags BlendAlpha()
    {
        return BgfxUtils.StateBlendFunc((ulong)StateFlags.BlendSrcAlpha, (ulong)StateFlags.BlendInvSrcAlpha);
    }

    public static StateFlags StateBlendFunc(ulong _src, ulong _dst) => (StateFlags)StateBlendFuncSeparate(_src, _dst, _src, _dst);

    private static ulong StateBlendFuncSeparate(ulong _srcRGB, ulong _dstRGB, ulong _srcA, ulong _dstA)
    {
        return 0 | ((_srcRGB | (_dstRGB << 4))) | ((_srcA | (_dstA << 4)) << 8);
    }

    public unsafe static int CountOf<T>(T* array, int length) where T : unmanaged
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        var elementSize = IntPtr.Size == 4 ? 4 : 8;
        var size = (uint)(length * elementSize);
        var result = (int)(size / elementSize);

        if (size % elementSize != 0)
        {
            throw new ArgumentException("Invalid array size");
        }

        return result;
    }
}