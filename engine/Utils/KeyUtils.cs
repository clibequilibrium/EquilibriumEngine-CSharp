using static SDL2.SDL;

namespace Engine.Utils;

public static class KeyUtils
{
    public static int KeySym(int sdl_sym, bool shift)
    {
        if (sdl_sym < 128)
        {
            if (shift)
            {
                if (sdl_sym == Keys.KEY_EQUALS)
                {
                    sdl_sym = Keys.KEY_PLUS;
                }
                else if (sdl_sym == Keys.KEY_UNDERSCORE)
                {
                    sdl_sym = Keys.KEY_MINUS;
                }
                else
                {
                    return sdl_sym;
                }
            }
            return sdl_sym;
        }

        switch (sdl_sym)
        {
            case (int)SDL_Keycode.SDLK_RIGHT:
                return 'R';
            case (int)SDL_Keycode.SDLK_LEFT:
                return 'L';
            case (int)SDL_Keycode.SDLK_DOWN:
                return 'D';
            case (int)SDL_Keycode.SDLK_UP:
                return 'U';
            case (int)SDL_Keycode.SDLK_LCTRL:
                return 'C';
            case (int)SDL_Keycode.SDLK_LSHIFT:
                return 'S';
            case (int)SDL_Keycode.SDLK_LALT:
                return 'A';
            case (int)SDL_Keycode.SDLK_RCTRL:
                return 'C';
            case (int)SDL_Keycode.SDLK_RSHIFT:
                return 'S';
            case (int)SDL_Keycode.SDLK_RALT:
                return 'A';
        }
        return 0;
    }

    public static void KeyDown(ref KeyState key)
    {
        if (key.State)
        {
            key.Pressed = false;
        }
        else
        {
            key.Pressed = true;
        }

        key.State = true;
        key.Current = true;
    }

    public static void KeyUp(ref KeyState key) { key.Current = false; }

    public static void KeyReset(ref KeyState state)
    {
        if (!state.Current)
        {
            state.State = false;
            state.Pressed = false;
        }
        else if (state.State)
        {
            state.Pressed = false;
        }
    }

    public static void MouseReset(ref MouseState state)
    {
        state.Rel.X = 0;
        state.Rel.Y = 0;

        state.Scroll.X = 0;
        state.Scroll.Y = 0;

        state.View.X = 0;
        state.View.Y = 0;

        state.Wnd.X = 0;
        state.Wnd.Y = 0;
    }
}