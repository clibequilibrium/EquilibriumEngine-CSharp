using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Utils;
using Arch.System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Equilibrium.Systems.Rendering;

public partial class ShaderHotReloadingSystem : BaseSystem<World, float>, IRenderSystem
{
    private FileSystemWatcher shaderFolderWatcher = null!;
    private ConcurrentQueue<string> reloadedShadersQueue = new ConcurrentQueue<string>();
    private QueryDescription shadersQueryDescription;

    public ShaderHotReloadingSystem(World world) : base(world)
    {
        shaderFolderWatcher = new FileSystemWatcher(@$"{AppContext.BaseDirectory}/content/shaders");

        shaderFolderWatcher.NotifyFilter = NotifyFilters.CreationTime
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.FileName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Size;

        shaderFolderWatcher.Changed += new FileSystemEventHandler(OnShaderRecompiled);
        shaderFolderWatcher.Created += new FileSystemEventHandler(OnShaderRecompiled);
        shaderFolderWatcher.Deleted += new FileSystemEventHandler(OnShaderRecompiled);
        shaderFolderWatcher.Renamed += new RenamedEventHandler(OnShaderRecompiled);

        shaderFolderWatcher.IncludeSubdirectories = true;
        shaderFolderWatcher.EnableRaisingEvents = true;

        shadersQueryDescription = new QueryDescription().WithAll<HotReloadableShader>();
    }

    public override void BeforeUpdate(in float t)
    {
        base.BeforeUpdate(t);

        while (reloadedShadersQueue.TryDequeue(out var dequeuedShaderName))
        {
            World.Query(in shadersQueryDescription, (in Entity entity, ref HotReloadableShader shader) =>
            {
                var component = World.Get(shader.Entity, shader.ComponentType);

                if (BgfxUtils.HotReloadShaders(shader.Entity, shader.ComponentType, dequeuedShaderName, ref component))
                {
                    World.Set(shader.Entity, component);
                }
            });
        }
    }

    private void OnShaderRecompiled(object source, FileSystemEventArgs e)
    {
        try
        {
            reloadedShadersQueue.Enqueue(Path.GetFileNameWithoutExtension(e.FullPath));
        }
        finally
        {
            shaderFolderWatcher.EnableRaisingEvents = true;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        shaderFolderWatcher?.Dispose();
    }
}