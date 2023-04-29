using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace Engine;

public class ShaderCompilerTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] ShaderFiles { get; set; } = null!;

    [Required]
    public ITaskItem BgfxIncludeDirectory { get; set; } = null!;

    [Required]
    public ITaskItem OutputDirectory { get; set; } = null!;

    public override bool Execute()
    {
        // string glslVersion = "430";
        // string glslsComputeVersion = "430";

        // DX9/11 shaders can only be compiled on Windows
        string shaderPlatforms = "glsl spirv";
        string dxModel = string.Empty;
        string shaderPlatformParentDirectory = string.Empty;

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            UseShellExecute = false
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            dxModel = "5_0";
            shaderPlatforms = shaderPlatforms + " dx11";
            shaderPlatformParentDirectory = "dx11";
            processStartInfo.FileName = $@"{BgfxIncludeDirectory.ItemSpec}\tools\win\shaderc.exe";
        }

        var shaderDirectory = Path.Combine(OutputDirectory.ItemSpec, shaderPlatformParentDirectory);
        if (!Directory.Exists(shaderDirectory))
        {
            Directory.CreateDirectory(shaderDirectory);
        }

        foreach (var shaderFile in ShaderFiles)
        {
            var shaderAbsolutePath = shaderFile.ItemSpec;
            var fileName = Path.GetFileNameWithoutExtension(shaderAbsolutePath);

            if (fileName.Contains("varying"))
                continue;


            string shaderProfile = string.Empty;

            if (fileName.Contains("vs_"))
            {
                shaderProfile = $"vs_{dxModel}";
            }

            else if (fileName.Contains("fs_"))
            {
                shaderProfile = $"ps_{dxModel}";
            }

            else if (fileName.Contains("cs_"))
            {
                shaderProfile = $"cs_{dxModel}";
            }

            var outputPath = Path.Combine(OutputDirectory.ItemSpec, shaderPlatformParentDirectory, $@"{fileName}.bin");

            // Check if source file was modified if not, skip it
            if (File.Exists(outputPath))
            {
                if (File.GetLastWriteTimeUtc(outputPath) > File.GetLastWriteTimeUtc(shaderAbsolutePath))
                {
                    continue;
                }
            }

            string shaderType = string.Empty;

            if (Regex.Match(fileName, "^(vs_)").Success)
            {
                shaderType = "VERTEX";
            }
            else if (Regex.Match(fileName, "^(fs_)").Success)
            {
                shaderType = "FRAGMENT";
            }
            else if (Regex.Match(fileName, "^(cs_)").Success)
            {
                shaderType = "COMPUTE";
            }
            else
            {
                Log.LogError($"Unknown shader type {fileName}");
                continue;
            }

            processStartInfo.Arguments = $@"-i {BgfxIncludeDirectory.ItemSpec}\include\ --type {shaderType} --platform {shaderPlatforms} -f {shaderAbsolutePath} -o {outputPath} -p {shaderProfile} --verbose";
            Log.LogMessage(MessageImportance.High, $"Compiling {fileName}");

            using var process = Process.Start(processStartInfo);
            process?.WaitForExit();

            if (process?.ExitCode != 0)
            {
                return false;
            }
        }

        return true;
    }
}