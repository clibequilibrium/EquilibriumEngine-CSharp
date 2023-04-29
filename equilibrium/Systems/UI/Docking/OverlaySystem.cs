using System.Numerics;
using System.Runtime.InteropServices;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Bgfx;
using ImGuiNET;

namespace Equilibrium.Systems.UI.Docking;

public partial class OverlaySystem : GuiSystem
{
    protected override bool DisabledByDefault => true;

    private const float GRAPH_FREQUENCY = 0.05f;
    private const int GRAPH_HISTORY = 100;

    private static float[] fpsValues = new float[100];
    private static float[] frameTimeValues = new float[100];
    private static float[] gpuMemoryValues = new float[100];
    private static int offset = 0;
    private static float oldTime = 0.0f;
    private static float time;

    public OverlaySystem(World world) : base(world) { }

    static bool DrawBar(float width, float maxWidth, float height, Vector4 color, string name)
    {
        ImGuiStylePtr style = ImGui.GetStyle();

        Vector4 hoveredColor = new(color.X * 1.1f, color.Y * 1.1f, color.Z * 1.1f, color.W * 1.1f);

        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoveredColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, style.ItemSpacing.Y));

        bool itemHovered = false;

        ImGui.Button(name, new Vector2(width, height));
        itemHovered = itemHovered || ImGui.IsItemHovered(0);

        ImGui.SameLine(0, 0);
        ImGui.InvisibleButton(name, new Vector2(Math.Max(1.0f, maxWidth - width), height), 0);
        itemHovered = itemHovered || ImGui.IsItemHovered(0);

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);

        return itemHovered;
    }

    protected unsafe override void Render(float deltaTime)
    {
        time += ImGui.GetIO().DeltaTime;

        float overlayWidth = 150f;

        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        Vector2 padding = new(5.0f, 25.0f);

        // top left, transparent background
        ImGui.SetNextWindowPos(viewport.WorkPos + padding, 0, Vector2.Zero);
        ImGui.SetNextWindowBgAlpha(0.5f);
        ImGui.Begin("Stats",
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration |
                    ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.AlwaysAutoResize |
                    ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav |
                    ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoInputs);

        // title
        // igSeparator();

        // general data
        bgfx.Stats* stats = bgfx.get_stats();
        double toCpuMs = 1000.0 / (double)(stats->cpuTimerFreq);
        double toGpuMs = 1000.0 / (double)(stats->gpuTimerFreq);

        ImGui.Text(string.Format("Backend: {0}", bgfx.get_renderer_type().ToString()));
        ImGui.Text(string.Format("Buffer size: {0} x {1} px", stats->width, stats->height));
        ImGui.Text(string.Format("Triangles: {0}", stats->numPrims[(int)bgfx.Topology.TriList]));
        ImGui.Text(string.Format("Draw calls: {0}", stats->numDraw));
        ImGui.Text(string.Format("Compute calls: {0}", stats->numCompute));

        // plots
        bool showFps = true;
        bool showFrameTime = true;
        bool showProfiler = true;
        bool showGpuMemory = true;

        if (showFps)
        {
            ImGui.Separator();
            ImGui.Text(string.Format("FPS: {0:0.##}", fpsValues[offset]));
        }
        if (showFrameTime)
        {
            ImGui.Separator();
            ImGui.Text(string.Format("CPU: {0:0.##} ms", (float)(stats->cpuTimeEnd - stats->cpuTimeBegin) * toCpuMs));
            ImGui.Text(string.Format("GPU: {0:0.##} ms", (float)(stats->gpuTimeEnd - stats->gpuTimeBegin) * toGpuMs));
            ImGui.Text(string.Format("Total: {0:0.##} ms", frameTimeValues[offset]));
        }

        if (showProfiler)
        {
            ImGui.Separator();
            ImGui.Text("View stats");
            if (stats->numViews > 0)
            {
                Vector4 cpuColor = new(0.5f, 1.0f, 0.5f, 1.0f);
                Vector4 gpuColor = new(0.5f, 0.5f, 1.0f, 1.0f);

                float itemHeight = ImGui.GetTextLineHeightWithSpacing();
                float itemHeightWithSpacing = ImGui.GetFrameHeightWithSpacing();
                float scale = 2.0f;

                if (ImGui.BeginListBox("##Stats", new Vector2(overlayWidth, stats->numViews * itemHeightWithSpacing)))
                {
                    ImGuiListClipper clip = new ImGuiListClipper { DisplayStart = stats->numViews, DisplayEnd = (int)itemHeight };
                    ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(&clip);
                    clipper.Begin(stats->numViews, itemHeight);

                    while (clipper.Step())
                    {
                        for (int pos = clipper.DisplayStart; pos < clipper.DisplayEnd; ++pos)
                        {
                            bgfx.ViewStats viewStats = stats->viewStats[pos];
                            float cpuElapsed =
                                (float)((viewStats.cpuTimeEnd - viewStats.cpuTimeBegin) * toCpuMs);
                            float gpuElapsed =
                                (float)((viewStats.gpuTimeEnd - viewStats.gpuTimeBegin) * toGpuMs);

                            ImGui.Text(viewStats.view.ToString());

                            float maxWidth = overlayWidth * 0.35f;
                            float cpuWidth = Math.Clamp(cpuElapsed * scale, 1.0f, maxWidth);
                            float gpuWidth = Math.Clamp(gpuElapsed * scale, 1.0f, maxWidth);

                            ImGui.SameLine(overlayWidth * 0.3f, 0);

                            string cpu = $"cpu {viewStats.view} {pos}";
                            string gpu = $"gpu {viewStats.view} {pos}";

                            string viewName = Marshal.PtrToStringUTF8(new nint(viewStats.name))!;

                            if (DrawBar(cpuWidth, maxWidth, itemHeight, cpuColor, cpu))
                            {
                                ImGui.SetTooltip(string.Format("{0} -- CPU: {1:1.##} ms", viewName, cpuElapsed));
                            }

                            ImGui.SameLine(0, 0);

                            if (DrawBar(gpuWidth, maxWidth, itemHeight, gpuColor, gpu))
                            {
                                ImGui.SetTooltip(string.Format("{0} -- GPU: {1:1.##} ms", viewName, gpuElapsed));
                            }
                        }
                    }

                    ImGui.EndListBox();
                }
            }
        }

        if (showGpuMemory)
        {
            var used = stats->gpuMemoryUsed;
            var max = stats->gpuMemoryMax;

            ImGui.Separator();
            if (used > 0 && max > 0)
            {
                ImGui.Text("GPU memory");
                ImGui.Text(string.Format("{0} / {1}", BgfxExtensions.BytesToString(stats->gpuMemoryUsed), BgfxExtensions.BytesToString(stats->gpuMemoryMax)));
            }

            // update after drawing so offset is the current value

            if (time - oldTime > GRAPH_FREQUENCY)
            {
                offset = (offset + 1) % GRAPH_HISTORY;
                ImGuiIOPtr io = ImGui.GetIO();
                fpsValues[offset] = io.Framerate;
                frameTimeValues[offset] = 1000.0f / io.Framerate;
                gpuMemoryValues[offset] = (float)(stats->gpuMemoryUsed) / 1024 / 1024;

                oldTime = time;
            }

            ImGui.End();
        }
    }
}