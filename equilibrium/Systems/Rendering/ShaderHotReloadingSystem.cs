using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Bgfx;
using Equilibrium.Components;
using static Bgfx.bgfx;
using Transform = Equilibrium.Components.Transform;

namespace Equilibrium.Systems.Rendering;

public partial class ShaderHotReloadingSystem : BaseSystem<World, float>, IRenderSystem
{
    private FileSystemWatcher shaderSourceWatcher = null!;

    private string GetThisFilePath([CallerFilePath] string path = null!)
    {
        return path;
    }

    public ShaderHotReloadingSystem(World world) : base(world)
    {
        var path = GetThisFilePath();

        var rootDirectory = Path.GetDirectoryName(path)!; // directory = @"path\to\your\source\code"
        var sourceCodeDirectory = Path.Combine(Directory.GetParent(path)?.Parent?.Parent?.Parent?.FullName!, @"content\shaders\");

        if (!Directory.Exists(sourceCodeDirectory))
        {
            return;
        }

        shaderSourceWatcher = new FileSystemWatcher(sourceCodeDirectory);

        shaderSourceWatcher.NotifyFilter = NotifyFilters.CreationTime
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.FileName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Size;

        shaderSourceWatcher.Changed += new FileSystemEventHandler(OnContentChanged);
        shaderSourceWatcher.Created += new FileSystemEventHandler(OnContentChanged);
        shaderSourceWatcher.Deleted += new FileSystemEventHandler(OnContentChanged);
        shaderSourceWatcher.Renamed += new RenamedEventHandler(OnContentChanged);

        shaderSourceWatcher.IncludeSubdirectories = true;
        shaderSourceWatcher.EnableRaisingEvents = true;
    }

    private void OnContentChanged(object source, FileSystemEventArgs e)
    {
        try
        {
            shaderSourceWatcher.EnableRaisingEvents = false;
            Console.WriteLine("Changed!" + e.FullPath);
        }
        finally
        {
            shaderSourceWatcher.EnableRaisingEvents = true;
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        shaderSourceWatcher?.Dispose();
    }
}