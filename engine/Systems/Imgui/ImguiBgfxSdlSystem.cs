using System.Numerics;
using System.Runtime.InteropServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using ImGuiNET;

namespace Engine.Systems.UI;

public partial class ImGuiBgfxSdlSystem : BaseSystem<World, float>
{
    public ImGuiBgfxSdlSystem(World world) : base(world) { }

    public override void Dispose()
    {
        base.Dispose();

        ImGui.ImGui_ImplSDL2_Shutdown();
        ImGuiImplBgfx.ImGui_Implbgfx_Shutdown();

        Console.WriteLine("ImGuiBgfx shutdown");
    }

    static void ApplyImGuiDarkStyle()
    {
        ImGuiStylePtr style = ImGui.GetStyle();

        style.Colors[(int)ImGuiCol.Text] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
        style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.13f, 0.14f, 0.15f, 1.00f);
        style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.13f, 0.14f, 0.15f, 1.00f);
        style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.13f, 0.14f, 0.15f, 1.00f);
        style.Colors[(int)ImGuiCol.Border] = new Vector4(0.43f, 0.43f, 0.50f, 0.50f);
        style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.38f, 0.38f, 0.38f, 1.00f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.67f, 0.67f, 0.67f, 0.39f);
        style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.08f, 0.08f, 0.09f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.08f, 0.08f, 0.09f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.00f, 0.00f, 0.00f, 0.51f);
        style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.14f, 0.14f, 0.14f, 1.00f);
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.02f, 0.02f, 0.02f, 0.53f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.31f, 0.31f, 0.31f, 1.00f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.41f, 0.41f, 0.41f, 1.00f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.51f, 0.51f, 0.51f, 1.00f);
        style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.11f, 0.64f, 0.92f, 1.00f);
        style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.11f, 0.64f, 0.92f, 1.00f);
        style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.08f, 0.50f, 0.72f, 1.00f);
        style.Colors[(int)ImGuiCol.Button] = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.38f, 0.38f, 0.38f, 1.00f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.67f, 0.67f, 0.67f, 0.39f);
        style.Colors[(int)ImGuiCol.Header] = new Vector4(0.22f, 0.22f, 0.22f, 1.00f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
        style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.67f, 0.67f, 0.67f, 0.39f);
        style.Colors[(int)ImGuiCol.Separator] = style.Colors[(int)ImGuiCol.Border];
        style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.41f, 0.42f, 0.44f, 1.00f);
        style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
        style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.29f, 0.30f, 0.31f, 0.67f);
        style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
        style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.08f, 0.08f, 0.09f, 0.83f);
        style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.33f, 0.34f, 0.36f, 0.83f);
        style.Colors[(int)ImGuiCol.TabActive] = new Vector4(0.23f, 0.23f, 0.24f, 1.00f);
        style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.08f, 0.08f, 0.09f, 1.00f);
        style.Colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.13f, 0.14f, 0.15f, 1.00f);
        style.Colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.26f, 0.59f, 0.98f, 0.70f);
        style.Colors[(int)ImGuiCol.DockingEmptyBg] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
        style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.61f, 0.61f, 0.61f, 1.00f);
        style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
        style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
        style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
        style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(0.11f, 0.64f, 0.92f, 1.00f);
        style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
        style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);
        style.GrabRounding = style.FrameRounding = style.WindowRounding = 3f;
    }

    private static void SetupImGuiStyle()
    {
        // Comfy styleGiuseppe from ImThemes
        var style = ImGuiNET.ImGui.GetStyle();

        style.Alpha = 1.0f;
        style.DisabledAlpha = 0.1000000014901161f;
        style.WindowPadding = new Vector2(8.0f, 8.0f);
        style.WindowRounding = 10.0f;
        style.WindowBorderSize = 0.0f;
        style.WindowMinSize = new Vector2(30.0f, 30.0f);
        style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
        style.WindowMenuButtonPosition = ImGuiDir.Right;
        style.ChildRounding = 5.0f;
        style.ChildBorderSize = 1.0f;
        style.PopupRounding = 10.0f;
        style.PopupBorderSize = 0.0f;
        style.FramePadding = new Vector2(5.0f, 3.5f);
        style.FrameRounding = 5.0f;
        style.FrameBorderSize = 0.0f;
        style.ItemSpacing = new Vector2(5.0f, 4.0f);
        style.ItemInnerSpacing = new Vector2(5.0f, 5.0f);
        style.CellPadding = new Vector2(4.0f, 2.0f);
        style.IndentSpacing = 5.0f;
        style.ColumnsMinSpacing = 5.0f;
        style.ScrollbarSize = 15.0f;
        style.ScrollbarRounding = 9.0f;
        style.GrabMinSize = 15.0f;
        style.GrabRounding = 5.0f;
        style.TabRounding = 5.0f;
        style.TabBorderSize = 0.0f;
        style.TabMinWidthForCloseButton = 0.0f;
        style.ColorButtonPosition = ImGuiDir.Right;
        style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
        style.SelectableTextAlign = new Vector2(0.0f, 0.0f);

        style.Colors[(int)ImGuiCol.Text] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(1.0f, 1.0f, 1.0f, 0.3605149984359741f);
        style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.09803921729326248f, 0.09803921729326248f, 0.09803921729326248f, 1.0f);
        style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
        style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.09803921729326248f, 0.09803921729326248f, 0.09803921729326248f, 1.0f);
        style.Colors[(int)ImGuiCol.Border] = new Vector4(0.4235294163227081f, 0.3803921639919281f, 0.572549045085907f, 0.54935622215271f);
        style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.1568627506494522f, 0.1568627506494522f, 0.1568627506494522f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.3803921639919281f, 0.4235294163227081f, 0.572549045085907f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.6196078658103943f, 0.5764706134796143f, 0.7686274647712708f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.09803921729326248f, 0.09803921729326248f, 0.09803921729326248f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.09803921729326248f, 0.09803921729326248f, 0.09803921729326248f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.2588235437870026f, 0.2588235437870026f, 0.2588235437870026f, 0.3f);
        // style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.1568627506494522f, 0.1568627506494522f, 0.1568627506494522f, 0.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.1568627506494522f, 0.1568627506494522f, 0.1568627506494522f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.2352941185235977f, 0.2352941185235977f, 0.2352941185235977f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.294117659330368f, 0.294117659330368f, 0.294117659330368f, 1.0f);
        style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.294117659330368f, 0.294117659330368f, 0.294117659330368f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.6196078658103943f, 0.5764706134796143f, 0.7686274647712708f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.8156862854957581f, 0.772549033164978f, 0.9647058844566345f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.Button] = new Vector4(0.6196078658103943f, 0.5764706134796143f, 0.7686274647712708f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.7372549176216125f, 0.6941176652908325f, 0.886274516582489f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.8156862854957581f, 0.772549033164978f, 0.9647058844566345f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.Header] = new Vector4(0.6196078658103943f, 0.5764706134796143f, 0.7686274647712708f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.7372549176216125f, 0.6941176652908325f, 0.886274516582489f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.8156862854957581f, 0.772549033164978f, 0.9647058844566345f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.6196078658103943f, 0.5764706134796143f, 0.7686274647712708f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.7372549176216125f, 0.6941176652908325f, 0.886274516582489f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.8156862854957581f, 0.772549033164978f, 0.9647058844566345f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.6196078658103943f, 0.5764706134796143f, 0.7686274647712708f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.7372549176216125f, 0.6941176652908325f, 0.886274516582489f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.8156862854957581f, 0.772549033164978f, 0.9647058844566345f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.6196078658103943f, 0.5764706134796143f, 0.7686274647712708f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.7372549176216125f, 0.6941176652908325f, 0.886274516582489f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.TabActive] = new Vector4(0.8156862854957581f, 0.772549033164978f, 0.9647058844566345f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.0f, 0.4509803950786591f, 1.0f, 0.0f);
        style.Colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.1333333402872086f, 0.2588235437870026f, 0.4235294163227081f, 0.0f);
        style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.294117659330368f, 0.294117659330368f, 0.294117659330368f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.7372549176216125f, 0.6941176652908325f, 0.886274516582489f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.6196078658103943f, 0.5764706134796143f, 0.7686274647712708f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.7372549176216125f, 0.6941176652908325f, 0.886274516582489f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.1882352977991104f, 0.1882352977991104f, 0.2000000029802322f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.4235294163227081f, 0.3803921639919281f, 0.572549045085907f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.4235294163227081f, 0.3803921639919281f, 0.572549045085907f, 0.2918455004692078f);
        style.Colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1.0f, 1.0f, 1.0f, 0.03433477878570557f);
        style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.7372549176216125f, 0.6941176652908325f, 0.886274516582489f, 0.5490196347236633f);
        style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.0f, 1.0f, 0.0f, 0.8999999761581421f);
        style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
        style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.0f, 1.0f, 1.0f, 0.699999988079071f);
        style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.2000000029802322f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.3499999940395355f);
    }

    [Query]
    [All<Input, AppWindowHandle, SdlWindow>, None<GuiContext>]
    private void ImGuiInitialize(ref Input input, in AppWindowHandle appWindowHandle, in Entity entity)
    {
        nint context = ImGui.CreateContext();
        ImGuiIOPtr io = ImGui.GetIO();

        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard; // Enable Keyboard Controls
                                                              // io.ConfigFlags |= ImGuiConfigFlags_NavEnableGamepad;    // Enable Gamepad Controls
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;   // Enable Docking
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable; // Enable Multi-Viewport Platform Windows
                                                            // io.ConfigViewportsNoAutoMerge = true;
                                                            // io.ConfigViewportsNoTaskBarIcon = true;
                                                            // io->ConfigFlags |= ImGuiConfigFlags_IsSRGB;
                                                            // io->ConfigFlags |= ImGuiConfigFlags_NoMouseCursorChange;

        io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        io.ConfigFlags |= ImGuiConfigFlags.NoMouseCursorChange;

        ApplyImGuiDarkStyle();

        ImGuiImplBgfx.ImGui_Implbgfx_Init();

        io.Fonts.AddFontFromFileTTF("content/fonts/DroidSans.ttf", 16);
        io.Fonts.AddFontFromFileTTF("content/fonts/Roboto-Medium.ttf", 16);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ImGui.ImGui_ImplSDL2_InitForOpenGL(appWindowHandle.Value, nint.Zero);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ImGui.ImGui_ImplSDL2_InitForMetal(appWindowHandle.Value);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ImGui.ImGui_ImplSDL2_InitForD3D(appWindowHandle.Value);
        }

        unsafe
        {
            input.Callback.Pointer = &ImGui.ImGui_ImplSDL2_ProcessEvent;
        }

        entity.Add<GuiContext>();
        entity.Set(new GuiContext { Value = context, GuiSystemsQuery = new QueryDescription().WithAll<GuiSystemHandle>() });

        string version = ImGui.GetVersion();
        Console.WriteLine($"ImGui {version} initialized");
    }

    private static void UpdatePlatformWindows()
    {
        // Update and Render additional Platform Windows
        if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
        }
    }

    [Query]
    [All<GuiContext>]
    private void ImGuiUpdate(in GuiContext context)
    {
        try
        {

            nint guiContextPtr = context.Value;

            ImGuiImplBgfx.ImGui_Implbgfx_NewFrame(World);
            ImGui.ImGui_ImplSDL2_NewFrame();
            ImGui.NewFrame();

            World.Query(in context.GuiSystemsQuery, (ref GuiSystemHandle guiSystemHandle) =>
            {
                guiSystemHandle.Value?.Render(Data, guiContextPtr);
            });

            ImGui.Render();

            ImDrawDataPtr drawData = ImGui.GetDrawData();
            ImGuiImplBgfx.ImGui_Implbgfx_RenderDrawLists(ImGuiImplBgfx.View, in drawData);

            UpdatePlatformWindows();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }
}