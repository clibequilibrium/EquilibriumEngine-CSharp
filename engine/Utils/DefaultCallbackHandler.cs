using System.Diagnostics;
using System.Runtime.InteropServices;
using Bgfx;
using static Bgfx.bgfx;

namespace Engine.Utils;

public class DefaultCallbackHandler : ICallbackHandler
{
    public void ProfilerBegin(string name, uint color, string filePath, uint line) { }
    public void ProfilerEnd() { }
    public void CaptureStarted(uint width, uint height, uint pitch, TextureFormat format, bool flipVertical) { }
    public void CaptureFrame(IntPtr data, uint size) { }
    public void CaptureFinished() { }
    public int GetCachedSize(ulong id) { return 0; }
    public bool GetCacheEntry(ulong id, IntPtr data, uint size) { return false; }
    public void SetCacheEntry(ulong id, IntPtr data, uint size) { }
    public void SaveScreenShot(string path, uint width, uint height, uint pitch, IntPtr data, uint size, bool flipVertical) { }

    public unsafe void ReportDebug(string fileName, uint line, string format, IntPtr args)
    {
        // Uncomment in the future and use a log file
        byte[] buffer = new byte[1024];
        NativeUtils.vsnprintf(buffer, 1024, format, args);

        fixed (byte* ptr = buffer)
        {
            // Debug.Write(Marshal.PtrToStringUTF8(new nint(ptr)));
        }
    }

    public void ReportError(string fileName, uint line, Fatal errorType, string message)
    {
        if (errorType == Fatal.DebugCheck)
            Debug.Write(message);
        else
        {
            Debug.Write(string.Format("{0} ({1})  {2}: {3}", fileName, line, errorType, message));
            Debugger.Break();
            Environment.Exit(1);
        }
    }
}