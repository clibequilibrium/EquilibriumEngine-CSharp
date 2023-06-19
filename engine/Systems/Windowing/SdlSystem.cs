

using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Engine.Utils;
using static SDL2.SDL;

namespace Engine.Systems.Windowing;

public partial class SdlSystem : BaseSystem<World, float>, IInputSystem
{
    public SdlSystem(World world, AppWindow appWindow, bool maximized = false) : base(world)
    {
        if (SDL_Init(SDL_INIT_EVERYTHING) != 0)
        {
            Console.WriteLine($"Unable to initialize SDL:  {SDL_GetError()}");
            return;
        }

        SDL_VERSION(out SDL_version compiled);
        SDL_GetVersion(out SDL_version linked);

        Console.WriteLine("SDL initialized");
        Console.WriteLine($"Compiled against SDL version {compiled.major}.{compiled.minor}.{compiled.patch}");
        Console.WriteLine($"Linked against SDL version {linked.major}.{linked.minor}.{linked.patch}");

        var entity = world.Create(appWindow, new Name { Value = "Equilibrium" });

        if (maximized)
            entity.Add(new Maximized());
    }

    public override void Dispose()
    {
        base.Dispose();

        var query = new QueryDescription().WithAll<SdlWindow, AppWindowHandle>(); // Targets entities with Position AND Velocity.
        World.Query(in query, (in Entity entity, ref SdlWindow sdlWindow, ref AppWindowHandle handle) =>
        {
            SDL_DestroyWindow(handle.Value);
            World.Destroy(entity);
        });

        SDL_Quit();
        Console.WriteLine("SDL shutdown");
    }

    [Query]
    [All<Input, AppWindow, AppWindowHandle>]
    private unsafe void SdlProcessEvents(ref Input input, ref AppWindow appWindow, ref AppWindowHandle appWindowHandle, in Entity entity)
    {
        for (int k = 0; k < 128; k++)
        {
            KeyUtils.KeyReset(ref input.Keys[k]);
        }

        KeyUtils.KeyReset(ref input.Mouse.Left);
        KeyUtils.KeyReset(ref input.Mouse.Right);
        KeyUtils.MouseReset(ref input.Mouse);

        while (SDL_PollEvent(out SDL_Event e) != 0)
        {
            if (input.Callback.Pointer != null)
                input.Callback.Pointer(new nint(&e));

            if (e.type == SDL_EventType.SDL_QUIT)
            {
                World.Create(new QuitEvent());
            }
            else if (e.type == SDL_EventType.SDL_KEYDOWN)
            {
                int sym = KeyUtils.KeySym((int)e.key.keysym.sym, input.Keys['S'].State);
                KeyUtils.KeyDown(ref input.Keys[sym]);

            }
            else if (e.type == SDL_EventType.SDL_KEYUP)
            {
                int sym = KeyUtils.KeySym((int)e.key.keysym.sym, input.Keys['S'].State);
                KeyUtils.KeyUp(ref input.Keys[sym]);

            }
            else if (e.type == SDL_EventType.SDL_MOUSEBUTTONDOWN)
            {
                if (e.button.button == SDL_BUTTON_LEFT)
                {
                    KeyUtils.KeyDown(ref input.Mouse.Left);
                }
                else if (e.button.button == SDL_BUTTON_RIGHT)
                {
                    KeyUtils.KeyDown(ref input.Mouse.Right);
                }

            }
            else if (e.type == SDL_EventType.SDL_MOUSEBUTTONUP)
            {
                if (e.button.button == SDL_BUTTON_LEFT)
                {
                    KeyUtils.KeyUp(ref input.Mouse.Left);
                }
                else if (e.button.button == SDL_BUTTON_RIGHT)
                {
                    KeyUtils.KeyUp(ref input.Mouse.Right);
                }

            }
            else if (e.type == SDL_EventType.SDL_MOUSEMOTION)
            {
                input.Mouse.Wnd.X = e.motion.x;
                input.Mouse.Wnd.Y = e.motion.y;
                input.Mouse.Rel.X = e.motion.xrel;
                input.Mouse.Rel.Y = e.motion.yrel;

            }
            else if (e.type == SDL_EventType.SDL_MOUSEWHEEL)
            {
                input.Mouse.Scroll.X = e.wheel.x;
                input.Mouse.Scroll.Y = e.wheel.y;
            }
            else if (e.type == SDL_EventType.SDL_WINDOWEVENT)
            {
                var eventType = e.window.windowEvent;

                if (eventType == SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED || eventType == SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED)
                {
                    if (SDL_GetWindowID(appWindowHandle.Value) == e.window.windowID)
                    {
                        SDL_GL_GetDrawableSize(appWindowHandle.Value, out int actual_width,
                                               out int actual_height);

                        appWindow.Width = actual_width;
                        appWindow.Height = actual_height;

                        break;
                    }
                }

                if (eventType == SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
                {
                    World.Create(new QuitEvent());
                }
                else if (eventType == SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED || eventType == SDL_WindowEventID.SDL_WINDOWEVENT_DISPLAY_CHANGED
                || eventType == SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED)
                {
                    if (entity.Has<Maximized>())
                        SetWindowMaximized(appWindowHandle.Value);
                }
            }
        }
    }

    [Query]
    [All<AppWindow>, None<AppWindowHandle>]
    private void SdlCreateWindow(ref AppWindow appWindow, in Entity entity)
    {
        string title = entity.Has<Name>() ? entity.Get<Name>().Value : null!;
        if (string.IsNullOrEmpty(title))
        {
            title = "SDL2 window";
        }

        int x = SDL_WINDOWPOS_UNDEFINED;
        int y = SDL_WINDOWPOS_UNDEFINED;

        SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI;

        if (entity.Has<Maximized>())
            flags |= SDL_WindowFlags.SDL_WINDOW_MAXIMIZED;

        nint createdWindow =
            SDL_CreateWindow(title, x, y, appWindow.Width, appWindow.Height, flags);

        if (createdWindow == 0)
        {
            Console.Error.WriteLine("SDL2 window creation failed: %s", SDL_GetError());
            return;
        }

        if (entity.Has<Maximized>())
        {
            SetWindowMaximized(createdWindow);
        }
        else
        {
            SDL_SetWindowSize(createdWindow, appWindow.Width, appWindow.Height);
        }

        SDL_GL_GetDrawableSize(createdWindow, out int actualWidth, out int actualHeight);

        appWindow.Width = actualWidth;
        appWindow.Height = actualHeight;

        entity.Add<SdlWindow, AppWindowHandle, Input>();
        entity.Set<AppWindowHandle>(new AppWindowHandle { Value = createdWindow });
    }

    private void SetWindowMaximized(nint windowHandle)
    {
        SDL_SetWindowBordered(windowHandle, SDL_bool.SDL_FALSE);

        int displayIndex = SDL_GetWindowDisplayIndex(windowHandle);
        if (displayIndex < 0)
        {
            Console.Error.WriteLine("Error getting window display");
            return;
        }

        if (0 != SDL_GetDisplayUsableBounds(displayIndex, out SDL_Rect usableBounds))
        {
            Console.Error.WriteLine("Error getting usable bounds");
            return;
        }

        SDL_SetWindowPosition(windowHandle, usableBounds.x, usableBounds.y);
        SDL_SetWindowSize(windowHandle, usableBounds.w, usableBounds.h);
    }
}