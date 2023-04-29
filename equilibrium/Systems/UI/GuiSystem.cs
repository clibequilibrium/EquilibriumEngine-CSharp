using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Equilibrium.Components;
using ImGuiNET;

namespace Equilibrium.Systems.UI;

public abstract class GuiSystem : BaseSystem<World, float>, IGuiSystem
{
    protected virtual QueryDescription Query { get; }
    protected virtual bool DisabledByDefault { get; }

    private bool enabled = true;
    private Entity systemEntity = Entity.Null;

    protected GuiSystem(World world) : base(world)
    {
        systemEntity = world.Create<GuiSystemHandle, Name>(new GuiSystemHandle { Value = this }, new Name { Value = this.GetType().Name });
        enabled = !DisabledByDefault;
    }

    public override void Dispose()
    {
        base.Dispose();
        World.Destroy(systemEntity);
    }

    public void ToggleState()
    {
        enabled = !enabled;
    }

    protected virtual void Render(float deltaTime) { }

    void IGuiSystem.Render(float deltaTime, nint guiContext)
    {
        if (!enabled)
            return;

        Data = deltaTime;
        ImGui.SetCurrentContext(guiContext);
        QueryDescription query = Query;
        this.Render(deltaTime);
    }
}