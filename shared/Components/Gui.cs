using Arch.Core;
using static Bgfx.bgfx;

public struct GuiSystemHandle
{
    public IGuiSystem Value;
}

public struct GuiContext
{
    public nint Value;
    public QueryDescription GuiSystemsQuery;
}

public struct AppWindow
{
    public int Width;
    public int Height;
}

public struct AppWindowHandle
{
    public nint Value;
}

public struct Maximized
{
}

public struct Renderer
{
    public RendererType Type;
}

public struct SdlWindow
{
}