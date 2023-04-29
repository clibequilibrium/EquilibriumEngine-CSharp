public interface ISystem { }

public interface IInputSystem : ISystem { }
public interface IGameSystem : ISystem { }
public interface IRenderSystem : ISystem { }

public interface IGuiSystem : ISystem
{
    void Render(float deltaTime, nint guiContext);
    void ToggleState();
}