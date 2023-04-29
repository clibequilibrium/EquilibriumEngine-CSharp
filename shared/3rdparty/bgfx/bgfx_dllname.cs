/*
 * Copyright 2011-2023 Branimir Karadzic. All rights reserved.
 * License: https://github.com/bkaradzic/bgfx/blob/master/LICENSE
 */

/*
 *
 * AUTO GENERATED! DO NOT EDIT!
 *
 * Include this file in your build if you want to use the default DllImport
 * names of bgfx.dll and bgfx_debug.dll.  Otherwise, define your own
 * partial class like the below with a const DllName for your use.
 *
 */

namespace Bgfx
{
#pragma warning disable 8981
public static partial class bgfx
{
#pragma warning restore 8981

#if DEBUG
       const string DllName = "bgfx-shared-libDebug.dll";
#else
       const string DllName = "bgfx-shared-libRelease.dll";
#endif
}
}
