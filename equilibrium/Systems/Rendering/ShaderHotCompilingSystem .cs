using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Equilibrium.Systems.Rendering;

public partial class ShaderHotCompilingSystem : BaseSystem<World, float>, IRenderSystem
{
    private string rootProjectPath;
    private FileSystemWatcher shaderSourceWatcher = null!;

    private string GetThisFilePath([CallerFilePath] string path = null!)
    {
        return path;
    }

    public ShaderHotCompilingSystem(World world) : base(world)
    {
        var path = GetThisFilePath();

        var rootDirectory = Path.GetDirectoryName(path)!; // directory = @"path\to\your\source\code"
        rootProjectPath = Directory.GetParent(path)?.Parent?.Parent?.Parent?.FullName!;
        var sourceCodeDirectory = Path.Combine(rootProjectPath, @"content\shaders\");

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

        shaderSourceWatcher.Changed += new FileSystemEventHandler(OnShaderSourceChanged);
        shaderSourceWatcher.Created += new FileSystemEventHandler(OnShaderSourceChanged);
        shaderSourceWatcher.Deleted += new FileSystemEventHandler(OnShaderSourceChanged);
        shaderSourceWatcher.Renamed += new RenamedEventHandler(OnShaderSourceChanged);

        shaderSourceWatcher.IncludeSubdirectories = true;
        shaderSourceWatcher.EnableRaisingEvents = true;
    }

    private void OnShaderSourceChanged(object source, FileSystemEventArgs e)
    {
        try
        {
            shaderSourceWatcher.EnableRaisingEvents = false;

            ShaderCompilerTask task = new ShaderCompilerTask();
            task.ShaderFiles = new ITaskItem[] { new TaskItem(e.FullPath) };
            task.BgfxIncludeDirectory = new TaskItem(Path.Combine(rootProjectPath, @"shared\3rdparty\bgfx"));
            task.OutputDirectory = new TaskItem(@$"{AppContext.BaseDirectory}/content/shaders");

            task.Execute();
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