using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Bgfx;
using Equilibrium.Components;
using static Bgfx.bgfx;
using Transform = Equilibrium.Components.Transform;

namespace Equilibrium.Systems.Rendering;

public partial class GfxDestroyResourceSystem : BaseSystem<World, float>, IRenderSystem
{
    public GfxDestroyResourceSystem(World world) : base(world)
    {
    }

    public override void Dispose()
    {
        base.Dispose();

        var quitEventQuery = new QueryDescription().WithAll<QuitEvent>();

        if (World.CountEntities(quitEventQuery) == 0)
            return;

        var query = new QueryDescription().WithAll<GfxResource>();

        World.Query(in query, (ref GfxResource gfxResource) =>
        {
            if (gfxResource.Handle == BgfxConstants.InvalidHandle)
            {
                Console.Error.WriteLine("Invalid GfxResource handle");
                return;
            }

            switch (gfxResource.Type)
            {
                case ResourceType.Texture:
                    bgfx.destroy_texture(new TextureHandle { idx = gfxResource.Handle });
                    break;

                case ResourceType.VertexBuffer:
                    bgfx.destroy_vertex_buffer(new VertexBufferHandle { idx = gfxResource.Handle });
                    break;

                case ResourceType.DynamicVertexBuffer:
                    bgfx.destroy_dynamic_vertex_buffer(new DynamicVertexBufferHandle { idx = gfxResource.Handle });
                    break;

                case ResourceType.IndexBuffer:
                    bgfx.destroy_index_buffer(new IndexBufferHandle { idx = gfxResource.Handle });
                    break;

                case ResourceType.Program:
                    bgfx.destroy_program(new ProgramHandle { idx = gfxResource.Handle });
                    break;

                case ResourceType.FrameBuffer:
                    bgfx.destroy_frame_buffer(new FrameBufferHandle { idx = gfxResource.Handle });
                    break;

                case ResourceType.Uniform:
                    bgfx.destroy_uniform(new UniformHandle { idx = gfxResource.Handle });
                    break;

                case ResourceType.Invalid:
                default:
                    Console.Error.WriteLine("Unsupported resource type");
                    break;
            }
        });
    }
}