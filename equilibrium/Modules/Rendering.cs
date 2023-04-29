using Arch.Core;
using Equilibrium.Systems.Rendering;

namespace Equilibrium.Modules;

public class Rendering : IModule
{
    // Frame flow:
    // * base_rendering_system pre update 
    // * forward rendering system pre update
    // * camera system projection
    // * pbr bind albedo
    // * bind ligts
    // * forward_renderer_system draw meshes
    // * base_rendering_system blit to screen
    List<ISystem> IModule.Initialize(World world)
    {
        return new List<ISystem>
            {
                new GfxDestroyResourceSystem(world),
                new ShaderHotReloadingSystem(world),
                new TransformSystem(world),
                new BaseRenderingSystem(world),
                new CameraSystem(world),
                new PBRSystem(world),
                new LightBufferUpdateSystem(world),
                new LightSystem(world),
                new ForwardRendererSystem(world),
                new SkySystem(world),
            };
    }
}