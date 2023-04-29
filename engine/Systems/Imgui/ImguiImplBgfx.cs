using System.Numerics;
using System.Runtime.InteropServices;
using Arch.Core;
using Bgfx;
using Engine.Utils;
using ImGuiNET;
using static Bgfx.bgfx;
using static SDL2.SDL;

namespace Engine.Systems.UI;

public sealed class ImGuiImplBgfx
{
    unsafe delegate void ImDrawCallback(void* ptr, void* ptr2);

    private struct ImGuiViewportData
    {
        public FrameBufferHandle FrameBufferHandle;
        public ushort ViewId;
        public ushort Width;
        public ushort Height;
    }

    [Flags]
    private enum BgfxTextureFlags : uint
    {
        Opaque = 1u << 31,
        PointSampler = 1u << 30,
        All = Opaque | PointSampler,
    };

    public const byte View = 255;
    private static TextureHandle fontTexture = new TextureHandle { idx = BgfxConstants.InvalidHandle };
    private static ProgramHandle shaderHandle = new ProgramHandle { idx = BgfxConstants.InvalidHandle };
    private static ProgramHandle imageProgram = new ProgramHandle { idx = BgfxConstants.InvalidHandle };
    private static UniformHandle attribLocationTex = new UniformHandle { idx = BgfxConstants.InvalidHandle };
    private static VertexLayout vertexLayout;
    private static UniformHandle imageLodEnabled;

    private static List<ushort> freeViewIds = new List<ushort>();
    private static ushort subViewId = 100;

    private static ushort AllocateViewId()
    {
        if (freeViewIds.Count > 0)
        {
            var id = freeViewIds.Last();
            freeViewIds.RemoveAt(freeViewIds.Count - 1);
            return id;
        }

        return subViewId++;
    }

    private static void FreeViewId(ushort id) => freeViewIds.Add(id);

    private static unsafe bool CheckAvailTransientBuffers(uint numVertices, VertexLayout* layout, uint numIndices)
    {
        return numVertices == bgfx.get_avail_transient_vertex_buffer(numVertices, layout)
        &&
        (0 == numIndices || numIndices == bgfx.get_avail_transient_index_buffer(numIndices, false));
    }

    private static nint GetNativeWindowHandle(nint window)
    {
        SDL_SysWMinfo wmi = default;
        SDL_VERSION(out wmi.version);

        if (SDL_GetWindowWMInfo(window, ref wmi) == SDL_bool.SDL_FALSE)
        {
            return nint.Zero;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return wmi.info.x11.window;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return wmi.info.cocoa.window;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return wmi.info.win.window;
        }

        return nint.Zero;
    }

    private delegate void ImGuiBgfxOnCreateWindowDelegate(nint ptr);
    private static unsafe void ImGuiBgfxOnCreateWindow(nint ptr)
    {
        ImGuiViewport* viewport = (ImGuiViewport*)ptr.ToPointer();

        nint mem = Marshal.AllocHGlobal(Marshal.SizeOf<ImGuiViewportData>());
        ImGuiViewportData viewportData = Marshal.PtrToStructure<ImGuiViewportData>(mem);

        // Setup view id and size
        viewportData.ViewId = AllocateViewId();
        viewportData.Width = Math.Max((ushort)viewport->Size.X, (ushort)1);
        viewportData.Height = Math.Max((ushort)viewport->Size.Y, (ushort)1);


        // Create frame buffer
        viewportData.FrameBufferHandle = bgfx.create_frame_buffer_from_nwh(
            GetNativeWindowHandle(new nint(viewport->PlatformHandle)).ToPointer(), viewportData.Width, viewportData.Height, TextureFormat.Count, TextureFormat.Count);

        Marshal.StructureToPtr(viewportData, mem, true);
        viewport->RendererUserData = mem.ToPointer();
    }


    private delegate void ImGuiBgfxOnDestroyWindowDelegate(nint ptr);
    private static unsafe void ImGuiBgfxOnDestroyWindow(nint ptr)
    {
        unsafe
        {
            ImGuiViewport* viewport = (ImGuiViewport*)ptr.ToPointer();
            ImGuiViewportData* viewportData = (ImGuiViewportData*)viewport->RendererUserData;

            if (viewportData != null)
            {
                viewport->RendererUserData = null;
                FreeViewId(viewportData->ViewId);
                bgfx.destroy_frame_buffer(viewportData->FrameBufferHandle);
                viewportData->FrameBufferHandle.idx = BgfxConstants.InvalidHandle;
                Marshal.FreeHGlobal(new nint(viewportData));
            }
        }
    }

    private delegate void ImGuiBgfxOnSetWindowSizeDelegate(nint ptr, Vector2 size);
    private static void ImGuiBgfxOnSetWindowSize(nint ptr, Vector2 size)
    {
        ImGuiBgfxOnDestroyWindow(ptr);
        ImGuiBgfxOnCreateWindow(ptr);
    }

    private delegate void ImGuiBgfxOnRenderWindowDelegate(nint ptr, nint ptr2);
    private static unsafe void ImGuiBgfxOnRenderWindow(nint viewportPtr, nint ptr2)
    {
        ImGuiViewport* viewport = (ImGuiViewport*)viewportPtr.ToPointer();
        ImGuiViewportData* viewportData = (ImGuiViewportData*)viewport->RendererUserData;

        if (viewportData != null)
        {

            bgfx.set_view_frame_buffer(viewportData->ViewId, viewportData->FrameBufferHandle);
            bgfx.set_view_rect(viewportData->ViewId, 0, 0, viewportData->Width, viewportData->Height);

            bgfx.set_view_clear(viewportData->ViewId, (ushort)ClearFlags.Color, 0xff00ffff, 1.0f, 0);
            bgfx.set_state((ulong)StateFlags.Default, 0);

            ImGui_Implbgfx_RenderDrawLists(viewportData->ViewId, new ImDrawDataPtr(viewport->DrawData));

            bgfx.touch(viewportData->ViewId);
        }
    }

    // This is the main rendering function that you have to implement and call after
    // ImGui::Render(). Pass ImGui::GetDrawData() to this function.
    // Note: If text or lines are blurry when integrating ImGui into your engine,
    // in your Render function, try translating your projection matrix by
    // (0.5f,0.5f) or (0.375f,0.375f)
    public static void ImGui_Implbgfx_RenderDrawLists(ushort viewId, in ImDrawDataPtr draw_data)
    {
        // Avoid rendering when minimized, scale coordinates for retina displays
        // (screen coordinates != framebuffer coordinates)
        int fb_width = (int)(draw_data.DisplaySize.X * draw_data.FramebufferScale.X);
        int fb_height = (int)(draw_data.DisplaySize.Y * draw_data.FramebufferScale.Y);
        if (fb_width <= 0 || fb_height <= 0)
            return;

        unsafe
        {
            bgfx.set_view_name(viewId, "ImGui");
            bgfx.set_view_mode(viewId, ViewMode.Sequential);

            bgfx.Caps* caps = bgfx.get_caps();
            {
                Matrix4x4 ortho;
                float x = draw_data.DisplayPos.X;
                float y = draw_data.DisplayPos.Y;
                float width = draw_data.DisplaySize.X;
                float height = draw_data.DisplaySize.Y;

                ortho = Matrix4x4.CreateOrthographicOffCenter(x, x + width, y + height, y, 0, 1000);

                bgfx.set_view_transform(viewId, null, &ortho);
                bgfx.set_view_rect(viewId, 0, 0, (ushort)width, (ushort)height);
            }


            Vector2 clipPos = draw_data.DisplayPos;       // (0,0) unless using multi-viewports
            Vector2 clipScale = draw_data.FramebufferScale; // (1,1) unless using retina display which
                                                            // are often (2,2)

            // Render command lists
            for (int ii = 0, num = draw_data.CmdListsCount; ii < num; ++ii)
            {
                TransientVertexBuffer tvb;
                TransientIndexBuffer tib;

                ImDrawListPtr drawList = draw_data.CmdListsRange[ii];
                uint numVertices = (uint)drawList.VtxBuffer.Size;
                uint numIndices = (uint)drawList.IdxBuffer.Size;

                fixed (VertexLayout* layout = &vertexLayout)
                {
                    if (!CheckAvailTransientBuffers(numVertices, layout, numIndices))
                    {
                        // not enough space in transient buffer just quit drawing the
                        // rest...
                        break;
                    }
                }

                fixed (VertexLayout* layout = &vertexLayout)
                {
                    bgfx.alloc_transient_vertex_buffer(&tvb, numVertices, layout);
                }

                bgfx.alloc_transient_index_buffer(&tib, numIndices, sizeof(ushort) == 4);

                ImDrawVert* verts = (ImDrawVert*)tvb.data;

                NativeUtils.memcpy(new nint(verts), drawList.VtxBuffer.Data, numVertices * (uint)sizeof(ImDrawVert));

                ushort* indices = (ushort*)tib.data;
                NativeUtils.memcpy(new nint(indices), drawList.IdxBuffer.Data, numIndices * sizeof(ushort));

                Encoder* encoder = bgfx.encoder_begin(false);

                // uint offset = 0;

                for (int i = 0; i < drawList.CmdBuffer.Size; i++)
                {
                    ImDrawCmdPtr cmd = drawList.CmdBuffer[i];

                    if (cmd.UserCallback != nint.Zero)
                    {
                        ImDrawCallback drawCallback = Marshal.GetDelegateForFunctionPointer<ImDrawCallback>(cmd.UserCallback);
                        drawCallback(drawList, cmd);
                    }
                    else if (0 != cmd.ElemCount)
                    {
                        StateFlags state = StateFlags.None | StateFlags.WriteRgb | StateFlags.WriteA | StateFlags.Msaa;
                        SamplerFlags sampler_state = 0;

                        TextureHandle th = fontTexture;
                        ProgramHandle program = shaderHandle;

                        bool alphaBlend = true;
                        if (cmd.TextureId != nint.Zero)
                        {
                            var textureInfo = (nuint)cmd.TextureId;
                            if ((textureInfo & (uint)BgfxTextureFlags.Opaque) != 0)
                            {
                                alphaBlend = false;
                            }
                            if ((textureInfo & (uint)BgfxTextureFlags.PointSampler) != 0)
                            {
                                sampler_state = SamplerFlags.None | SamplerFlags.MinPoint | SamplerFlags.MagPoint | SamplerFlags.MipPoint;
                            }
                            textureInfo &= ~(uint)BgfxTextureFlags.All;
                            th = new TextureHandle { idx = (ushort)textureInfo };
                        }

                        if (alphaBlend)
                        {
                            state |= BgfxUtils.BlendAlpha();
                        }

                        // Project scissor/clipping rectangles into framebuffer space
                        Vector4 clipRect = default;
                        clipRect.X = (cmd.ClipRect.X - clipPos.X) * clipScale.X;
                        clipRect.Y = (cmd.ClipRect.Y - clipPos.Y) * clipScale.Y;
                        clipRect.Z = (cmd.ClipRect.Z - clipPos.X) * clipScale.X;
                        clipRect.W = (cmd.ClipRect.W - clipPos.Y) * clipScale.Y;

                        if (clipRect.X < fb_width && clipRect.Y < fb_height && clipRect.Z >= 0.0f &&
                            clipRect.W >= 0.0f)
                        {
                            ushort xx = (ushort)Math.Max(clipRect.X, 0.0f);
                            ushort yy = (ushort)Math.Max(clipRect.Y, 0.0f);

                            bgfx.encoder_set_scissor(encoder, xx, yy, (ushort)(Math.Min(clipRect.Z, 65535.0f) - xx),
                                                                      (ushort)(Math.Min(clipRect.W, 65535.0f) - yy));

                            bgfx.encoder_set_state(encoder, (ulong)state, 0);
                            bgfx.encoder_set_texture(encoder, 0, attribLocationTex, th, (uint)sampler_state);
                            bgfx.encoder_set_transient_vertex_buffer(encoder, 0, &tvb, 0, numVertices);
                            bgfx.encoder_set_transient_index_buffer(encoder, &tib, cmd.IdxOffset, cmd.ElemCount);
                            bgfx.encoder_submit(encoder, viewId, program, 0, byte.MaxValue);
                        }
                    }

                    // offset += cmd.ElemCount;
                }

                bgfx.encoder_end(encoder);
            }
        }
    }

    static bool ImGui_Implbgfx_CreateFontsTexture()
    {
        // Build texture atlas
        ImGuiIOPtr io = ImGui.GetIO();

        unsafe
        {
            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height);

            // Upload texture to graphics system
            fontTexture = bgfx.create_texture_2d((ushort)width, (ushort)height, false, 1,
                                                  TextureFormat.BGRA8, 0,
                                                  bgfx.copy(pixels, (uint)(width * height * 4)));

            // Store our identifier
            io.Fonts.TexID = new nint(fontTexture.idx);
        }

        return true;
    }

    private static bool ImGui_Implbgfx_CreateDeviceObjects(World world)
    {
        shaderHandle = world.CreateProgram("vs_ocornut_imgui", "fs_ocornut_imgui", "ImGuiShader");

        vertexLayout = vertexLayout.Begin()
            .Add(Attrib.Position, 2, AttribType.Float)
            .Add(Attrib.TexCoord0, 2, AttribType.Float)
            .Add(Attrib.Color0, 4, AttribType.Uint8, true)
            .End();

        imageLodEnabled = bgfx.create_uniform("u_imageLodEnabled", UniformType.Vec4, 1);
        imageProgram = world.CreateProgram("vs_imgui_image", "fs_imgui_image", "ImGuiImageShader");

        attribLocationTex = bgfx.create_uniform("g_AttribLocationTex", UniformType.Sampler, 1);
        ImGui_Implbgfx_CreateFontsTexture();

        return true;
    }

    private static void ImGui_Implbgfx_InvalidateDeviceObjects()
    {
        attribLocationTex.Destroy();
        shaderHandle.Destroy();
        imageProgram.Destroy();
        imageLodEnabled.Destroy();

        if (fontTexture.Valid)
        {
            fontTexture.Destroy();
            ImGui.GetIO().Fonts.TexID = 0;
            fontTexture.idx = BgfxConstants.InvalidHandle;
        }
    }

    public static void ImGui_Implbgfx_Init()
    {
        ImGuiPlatformIOPtr platform_io = ImGui.GetPlatformIO();
        platform_io.Renderer_CreateWindow = Marshal.GetFunctionPointerForDelegate((ImGuiBgfxOnCreateWindowDelegate)ImGuiBgfxOnCreateWindow);
        platform_io.Renderer_DestroyWindow = Marshal.GetFunctionPointerForDelegate((ImGuiBgfxOnDestroyWindowDelegate)ImGuiBgfxOnDestroyWindow);
        platform_io.Renderer_SetWindowSize = Marshal.GetFunctionPointerForDelegate((ImGuiBgfxOnSetWindowSizeDelegate)ImGuiBgfxOnSetWindowSize);
        platform_io.Renderer_RenderWindow = Marshal.GetFunctionPointerForDelegate((ImGuiBgfxOnRenderWindowDelegate)ImGuiBgfxOnRenderWindow);
    }

    public static void ImGui_Implbgfx_Shutdown() { ImGui_Implbgfx_InvalidateDeviceObjects(); }

    public static void ImGui_Implbgfx_NewFrame(World world)
    {
        if (!fontTexture.Valid)
        {
            ImGui_Implbgfx_CreateDeviceObjects(world);
        }
    }
}