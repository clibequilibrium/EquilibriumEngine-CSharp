using System.Runtime.InteropServices;

namespace Engine.Utils;

public static class NativeUtils
{
    [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    public static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

    [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, EntryPoint = "vsnprintf")]
    private static extern int vsnprintfWindows(byte[] buffer, int size, string format, IntPtr args);

    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "vsnprintf")]
    private static extern int vsnprintfLinux(byte[] buffer, int size, string format, IntPtr args);

    [DllImport("libSystem.dylib", CallingConvention = CallingConvention.Cdecl, EntryPoint = "vsnprintf")]
    private static extern int vsnprintfOSX(byte[] buffer, int size, string format, IntPtr args);

    public static int vsnprintf(byte[] buffer, int size, string format, IntPtr args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return vsnprintfWindows(buffer, size, format, args);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return vsnprintfLinux(buffer, size, format, args);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return vsnprintfOSX(buffer, size, format, args);
        }
        else
        {
            return -1;
        }
    }
}