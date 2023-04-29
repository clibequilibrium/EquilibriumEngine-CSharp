using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Engine;

internal static class HotReloadManager
{
    private static FileSystemWatcher watcher = null!;
    private static string sourceCodeDirectory = null!;
    private static string rootDirectory = null!;
    private static string pluginsDirectory = null!;
    private const int maxRetryCount = 3;

    private static string GetThisFilePath([CallerFilePath] string path = null!)
    {
        return path;
    }

    internal static void Initialize()
    {
        var path = GetThisFilePath(); // path = @"path\to\your\source\code\file.cs"
        rootDirectory = Path.GetDirectoryName(path)!; // directory = @"path\to\your\source\code"
        sourceCodeDirectory = Path.Combine(Directory.GetParent(rootDirectory)?.FullName!, "equilibrium");
        pluginsDirectory = Path.Combine(AppContext.BaseDirectory, "equilibrium");

        if (!Directory.Exists(sourceCodeDirectory))
        {
            return;
        }

        watcher = new FileSystemWatcher(sourceCodeDirectory);

        watcher.NotifyFilter = NotifyFilters.CreationTime
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.FileName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Size;

        watcher.Changed += new FileSystemEventHandler(OnContentChanged);
        watcher.Created += new FileSystemEventHandler(OnContentChanged);
        watcher.Deleted += new FileSystemEventHandler(OnContentChanged);
        watcher.Renamed += new RenamedEventHandler(OnContentChanged);

        watcher.Filter = "*.cs";
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
    }

    private static void OnContentChanged(object source, FileSystemEventArgs e)
    {
        try
        {
            int retryCount = 0;
            watcher.EnableRaisingEvents = false;

            Console.WriteLine("Attempting to hot-reload...");

        build:
            retryCount++;

            if (retryCount > maxRetryCount)
                return;

            // Specify the command to be executed
            string command = "dotnet build /restore:false";

            // Choose the shell based on the operating system
            string shell;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shell = "cmd.exe";
                command = $"/C {command}";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                shell = "/bin/bash";
                command = $"-c \"{command}\"";
            }
            else
            {
                Console.Error.WriteLine("Unsupported operating system");
                return;
            }

            // Start the process
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = shell,
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = sourceCodeDirectory
                }
            };

            process.ErrorDataReceived += OnOuputDataReceived;
            process.OutputDataReceived += OnOuputDataReceived;
            process.EnableRaisingEvents = true;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            int exitCode = process.ExitCode;

            if (exitCode != 0)
            {
                goto build;
            }
            else
            {
                // Copy and replace dll libraries
                foreach (var file in Directory.EnumerateFiles(Path.Combine(sourceCodeDirectory, "bin"), "equilibrium.*", SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(file);
                    fileInfo.CopyTo(Path.Combine(pluginsDirectory, fileInfo.Name), true);
                }

                Console.WriteLine($"Succesfully hot-reloaded {e.Name}");
            }
        }
        finally
        {
            watcher.EnableRaisingEvents = true;
        }
    }

    private static void OnOuputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e?.Data != null)
        {
            if (!e.Data.Contains("error") || e.Data.Contains(".pdb"))
                return;

            Console.WriteLine(e.Data);
        }
    }
}