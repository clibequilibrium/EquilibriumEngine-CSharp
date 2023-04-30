using System.Numerics;
using Arch.Core;
using Equilibrium.Systems.UI.Tools;
using ImGuiNET;

namespace Equilibrium.Systems.UI.Docking;

public partial class DockspaceSystem : GuiSystem
{
    public DockspaceSystem(World world) : base(world) { }

    protected override void Render(float deltaTime)
    {
        ImGuiDockNodeFlags dockspaceFlags = ImGuiDockNodeFlags.PassthruCentralNode;

        // We are using the ImGuiWindowFlags_NoDocking flag to make the parent
        // window not dockable into, because it would be confusing to have two
        // docking targets within each others.
        ImGuiWindowFlags window_flags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
        ImGuiViewportPtr viewport = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(viewport.WorkPos, 0, Vector2.Zero);
        ImGui.SetNextWindowSize(viewport.WorkSize, 0);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);

        window_flags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
                        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        window_flags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        // When using ImGuiDockNodeFlags_PassthruCentralNode, DockSpace() will
        // render our background and handle the pass-thru hole, so we ask Begin() to
        // not render a background.
        if ((dockspaceFlags & ImGuiDockNodeFlags.PassthruCentralNode) != 0)
            window_flags |= ImGuiWindowFlags.NoBackground;

        // Important: note that we proceed even if Begin() returns false (aka window
        // is collapsed). This is because we want to keep our DockSpace() active. If
        // a DockSpace() is inactive, all active windows docked into it will lose
        // their parent and become undocked. We cannot preserve the docking
        // relationship between an active window and an inactive docking, otherwise
        // any change of dockspace/settings would lead to windows being stuck in
        // limbo and never being visible.
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        // ImGui::PushStyleVar(ImGuiStyleVar_FramePadding, ImVec2(0.0f, 7.5f));

        ImGui.Begin("DockSpace Demo", window_flags);
        ImGui.PopStyleVar(1);
        ImGui.PopStyleVar(2);

        // Submit the DockSpace
        ImGuiIOPtr io = ImGui.GetIO();
        if ((io.ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0)
        {
            uint dockspaceId = ImGui.GetID("MyDockSpace");
            ImGui.DockSpace(dockspaceId, Vector2.Zero, dockspaceFlags);
        }

        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("Debug", true))
            {
                var guiSystems = new QueryDescription().WithAll<GuiSystemHandle, Name>();


                if (ImGui.MenuItem("Entity Inspector"))
                {
                    World.Query(in guiSystems, (ref GuiSystemHandle handle, ref Name name) =>
                    {
                        if (name.Value == typeof(EntityInspectorSystem).Name)
                        {
                            handle.Value.ToggleState();
                            return;
                        }
                    });
                }


                if (ImGui.MenuItem("Overlay"))
                {
                    World.Query(in guiSystems, (ref GuiSystemHandle handle, ref Name name) =>
                    {
                        if (name.Value == typeof(OverlaySystem).Name)
                        {
                            handle.Value.ToggleState();
                            return;
                        }
                    });
                }

                ImGui.EndMenu();
            }

            Vector2 region = ImGui.GetWindowContentRegionMax();

            // Center offset
            ImGui.SameLine((region.X / 2.0f) - (1.5f * (ImGui.GetFontSize() + ImGui.GetStyle().ItemSpacing.X)), 0);
            ImGui.Text(Bgfx.bgfx.get_renderer_type().ToString());

            ImGui.SameLine(region.X - 175f, 0);
            ImGui.Text($"{ImGui.GetIO().Framerate:F2} FPS ({1000.0f / ImGui.GetIO().Framerate:F2} ms)");

            float button_size = 25.0f;
            ImGui.SameLine(region.X - button_size, 0);

            if (ImGui.Button("X", new Vector2(button_size, 0)))
                World.Create(new QuitEvent());

            ImGui.EndMenuBar();
        }

        ImGui.End();
    }
}