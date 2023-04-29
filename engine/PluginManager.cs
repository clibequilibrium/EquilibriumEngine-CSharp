using System.Collections.Concurrent;
using System.Reflection;
using Arch.System;
using Engine.Utils;
using Engine.Utils.Extensions;
using McMaster.NETCore.Plugins;

namespace Engine;

internal static class ModuleManager
{
    private static PluginLoader loader = null!;
    private static Dictionary<IModule, List<ISystem>> modules = new Dictionary<IModule, List<ISystem>>();
    private static ConcurrentQueue<PluginReloadedEventArgs> pluginReloadQueue = new ConcurrentQueue<PluginReloadedEventArgs>();

    internal static void Initialize()
    {
        var equilibriumDirectory = Path.Combine(AppContext.BaseDirectory, "equilibrium");
        var dllPath = Path.Combine(equilibriumDirectory, "equilibrium.dll");

        if (File.Exists(dllPath))
        {
            loader = PluginLoader.CreateFromAssemblyFile(
                dllPath,
                sharedTypes: new[] { typeof(IModule) }, config =>
                {
                    config.EnableHotReload = true;
                });

            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + @$"{equilibriumDirectory}");

            loader.Reloaded += OnPluginReloaded;
            LoadModules();
            InitializeModules();

            // Initialization called only once and not across hot reloads
            Engine.InputSystems.Initialize();
            Engine.GameSystems.Initialize();
            Engine.RenderSystems.Initialize();
        }
    }

    internal static void Update()
    {
        while (pluginReloadQueue.TryDequeue(out var eventArgs))
        {
            DestroyModules(true);
            EcsUtils.UpdateComponentRegistry(LoadModules());
            InitializeModules();
        }
    }

    internal static void DestroyModules(bool disposeSystems = false)
    {
        foreach (var kvp in modules)
        {
            IModule module = kvp.Key;
            List<ISystem> systems = kvp.Value;

            systems.ForEach(x =>
            {
                var archSystem = (ISystem<float>)x;

                Engine.InputSystems.TryRemove(archSystem);
                Engine.GameSystems.TryRemove(archSystem);
                Engine.RenderSystems.TryRemove(archSystem);

                if (disposeSystems)
                    archSystem.Dispose();
            });

            systems.Clear();
        }

        modules.Clear();
    }

    private static Assembly LoadModules()
    {
        var assembly = loader.LoadDefaultAssembly();
        foreach (var moduleType in assembly
                        .GetTypes()
                        .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var module = Activator.CreateInstance(moduleType) as IModule;

            if (module != null)
            {
                modules.Add(module, new List<ISystem>());
            }
        }

        return assembly;
    }

    private static void InitializeModules()
    {
        foreach (var kvp in modules)
        {
            var module = kvp.Key;
            var systems = module.Initialize(Engine.World);
            systems.ForEach(x => modules[module].Add(x));

            foreach (var system in systems)
            {
                if (system is IInputSystem)
                {
                    Engine.InputSystems.Add((ISystem<float>)system);
                }
                else if (system is IGameSystem)
                {
                    Engine.GameSystems.Add((ISystem<float>)system);
                }
                else if (system is IRenderSystem)
                {
                    Engine.RenderSystems.Add((ISystem<float>)system);
                }
            }
        }
    }

    // Executed asynchronously so we use a queue to dequeue on main  hread
    private static void OnPluginReloaded(object sender, PluginReloadedEventArgs eventArgs)
    {
        pluginReloadQueue.Enqueue(eventArgs);
    }
}