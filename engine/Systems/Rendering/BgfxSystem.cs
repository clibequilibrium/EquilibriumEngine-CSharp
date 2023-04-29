using System.Runtime.InteropServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Bgfx;
using Engine.Utils;
using static SDL2.SDL;

namespace Engine.Systems.Rendering;

public partial class BgfxSystem : BaseSystem<World, float>, IRenderSystem
{
    public BgfxSystem(World world) : base(world) { }

    static unsafe bool SetPlatformData(nint _window, ref bgfx.Init init)
    {
        SDL_SysWMinfo wmi = default;
        SDL_VERSION(out wmi.version);

        if (SDL_GetWindowWMInfo(_window, ref wmi) == SDL_bool.SDL_FALSE)
        {
            return false;
        }

        bgfx.PlatformData pd;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            pd.ndt = wmi.info.x11.display.ToPointer();
            pd.nwh = wmi.info.x11.window.ToPointer();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            pd.ndt = null;
            pd.nwh = wmi.info.cocoa.window.ToPointer();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            pd.ndt = null;
            pd.nwh = wmi.info.win.window.ToPointer();
        }

        pd.context = null;
        pd.backBuffer = null;
        pd.backBufferDS = null;
        bgfx.set_platform_data(&pd);

        init.platformData.nwh = pd.nwh;
        init.platformData.ndt = pd.ndt;

        return true;
    }

    [Query]
    [All<AppWindow, AppWindowHandle, SdlWindow>, None<BgfxComponent, Renderer>]
    private unsafe void BgfxInitialize(in AppWindow appWindow, ref AppWindowHandle appWindowHandle, in Entity entity)
    {
        bgfx.DebugFlags debug = bgfx.DebugFlags.Profiler;/* bgfx.DebugFlags.Profiler | bgfx.DebugFlags.Stats | bgfx.DebugFlags.Text; */
        bgfx.ResetFlags reset = bgfx.ResetFlags.Maxanisotropy | bgfx.ResetFlags.MsaaX16;

        bgfx.Init init;
        bgfx.init_ctor(&init);

        if (!SetPlatformData(appWindowHandle.Value, ref init))
        {
            Console.WriteLine($"Error: {SDL_GetError()}");
            return;
        }

        init.callback = NativeBgfxCallback.Create(new DefaultCallbackHandler());

        bgfx.init(&init);
        bgfx.reset((uint)appWindow.Width, (uint)appWindow.Height, (uint)reset, init.resolution.format);

        bgfx.set_debug((uint)debug);
        bgfx.set_view_clear(0, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), 0xEEEEEEEE, 1.0f, 0);

        bgfx.set_view_rect(0, 0, 0, (ushort)appWindow.Width, (ushort)appWindow.Height);

        entity.Add<BgfxComponent, Renderer>();
        entity.Set(new BgfxComponent { Data = init, Reset = reset }, new Renderer
        {
            Type = bgfx.get_renderer_type(),
        });
    }

    [Query]
    [All<AppWindow, BgfxComponent>]
    private void OnAppWindowResized(ref AppWindow appWindow, ref BgfxComponent bgfxComponent)
    {
        if (bgfxComponent.Data.resolution.width != appWindow.Width || bgfxComponent.Data.resolution.height != appWindow.Height)
        {
            bgfx.reset((uint)appWindow.Width, (uint)appWindow.Height, (uint)bgfxComponent.Reset,
                           bgfxComponent.Data.resolution.format);
            bgfx.set_view_rect(0, 0, 0, (ushort)appWindow.Width, (ushort)appWindow.Height);

            bgfxComponent.Data.resolution.width = (uint)appWindow.Width;
            bgfxComponent.Data.resolution.height = (uint)appWindow.Height;
        }
    }
}