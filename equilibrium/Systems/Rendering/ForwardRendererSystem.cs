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

public partial class ForwardRendererSystem : BaseSystem<World, float>, IRenderSystem
{
    private QueryDescription beginFrameQuery;
    private QueryDescription meshesQuery;

    const int defaultView = 0;

    public ForwardRendererSystem(World world) : base(world)
    {
        beginFrameQuery = new QueryDescription().WithAll<ForwardRenderer, AppWindow, FrameData, Camera>();
        meshesQuery = new QueryDescription().WithAll<Mesh, Material, Equilibrium.Components.Transform>();
    }

    public override void BeforeUpdate(in float t)
    {
        base.BeforeUpdate(t);
        World.Query(in beginFrameQuery, (in Entity entity, ref AppWindow appWindow, ref FrameData frameData, ref Camera camera) =>
        {
            if (!frameData.FrameBuffer.Valid)
                return;

            bgfx.set_view_name(defaultView, "Forward render pass");
            bgfx.set_view_clear(defaultView, (ushort)(ClearFlags.Color | ClearFlags.Depth), 0x303030FF, 1.0f, 0);
            bgfx.set_view_rect(defaultView, 0, 0, (ushort)appWindow.Width, (ushort)appWindow.Height);
            bgfx.set_view_frame_buffer(defaultView, frameData.FrameBuffer);

            bgfx.touch(defaultView);
            BaseRenderingSystem.SetViewProjection(defaultView, ref camera, appWindow.Width, appWindow.Height);
        });
    }


    [Query]
    [All<FrameData, PBRShader, ForwardRenderer>()]
    private void DrawMeshes(in FrameData frameData, in PBRShader pbrShader, in ForwardRenderer forwardRenderer)
    {
        FrameData frameDataCopy = frameData;
        PBRShader pbrShaderCopy = pbrShader;

        ForwardRenderer forwardRendererCopy = forwardRenderer;
        StateFlags state = StateFlags.Default & ~StateFlags.CullMask;

        bool rendered = false;

        World.Query(in meshesQuery, (in Entity entity, ref Mesh mesh, ref Material material, ref Equilibrium.Components.Transform transform) =>
        {
            unsafe
            {
                Matrix4x4 mtx = transform.Value;

                bgfx.set_transform(&mtx, 1);
                BaseRenderingSystem.SetNormalMatrix(in frameDataCopy, in transform.Value);

                bgfx.set_vertex_buffer(0, mesh.VertexBuffer, 0, uint.MaxValue);
                bgfx.set_index_buffer(mesh.IndexBuffer, 0, uint.MaxValue);

                ulong materialState = PBRSystem.BindMaterial(in pbrShaderCopy, in material);
                bgfx.set_state(((ulong)state | materialState), 0);
            }

            var flags = ~DiscardFlags.Bindings | DiscardFlags.IndexBuffer |
                            DiscardFlags.VertexStreams;
            bgfx.submit(defaultView, forwardRendererCopy.Program, 0, (byte)flags);


            rendered = true;
        });

        if (rendered)
        {
            bgfx.discard((byte)DiscardFlags.All);
        }
    }

    [Query]
    [All<BgfxComponent>, None<ForwardRenderer>]
    private void InitializeForwardRenderer(in Entity entity)
    {
        if (!BaseRenderingSystem.RendererSupported(false))
        {
            Console.Error.WriteLine("Forward rendering is not supported on this device");
            return;
        }

        ForwardRenderer forwardRenderer = default;
        World.LoadShaders(in entity, ref forwardRenderer);

        entity.Add(forwardRenderer);

        Console.WriteLine("Forward rendering System initialized.");
    }
}