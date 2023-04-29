using Engine.Systems.Rendering;
using Engine.Systems.UI;
using Engine.Systems.Windowing;

namespace Engine;

internal class EntryPoint
{
    internal static void Main(string[] args)
    {
        // Built-in systems that are not hot reloadable
        Engine.InputSystems.Add(new SdlSystem(Engine.World, new AppWindow { Width = 1366, Height = 768 }, true));
        Engine.RenderSystems.Add(new BgfxSystem(Engine.World), new ImGuiBgfxSdlSystem(Engine.World));

        Engine.Initialize();
        HotReloadManager.Initialize();
        ModuleManager.Initialize();

        // Starts the engine and blocks main thread
        Engine.Start();
        Engine.Shutdown();
        ModuleManager.DestroyModules();

        Environment.Exit(0);
    }
}