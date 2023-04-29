using System.Runtime.InteropServices;

public struct KeyState
{
    public bool Pressed;
    public bool State;
    public bool Current;
}

public struct MouseInput
{
    public float X;
    public float Y;
}

public struct MouseState
{
    public KeyState Left;
    public KeyState Right;
    public MouseInput Wnd;
    public MouseInput Rel;
    public MouseInput View;
    public MouseInput Scroll;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct InputEventCallback
{
    public delegate*<nint, bool> Pointer;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Input
{
    fixed byte keysMem[12 * 128]; // 12 bytes, 3 bools due to marshalling

    public MouseState Mouse;
    public InputEventCallback Callback;

    public Span<KeyState> Keys
    {
        get
        {
            fixed (byte* ptr = keysMem)
            {
                return new Span<KeyState>(ptr, 128);
            }
        }
    }
}

public static class Keys
{
    public const int KEY_UNKNOWN = ((int)0);
    public const int KEY_RETURN = ((int)'\r');
    public const int KEY_ESCAPE = ((int)27); // \033
    public const int KEY_BACKSPACE = ((int)'\b');
    public const int KEY_TAB = ((int)'\t');
    public const int KEY_SPACE = ((int)' ');
    public const int KEY_EXCLAIM = ((int)'!');
    public const int KEY_QUOTEDBL = ((int)'"');
    public const int KEY_HASH = ((int)'#');
    public const int KEY_PERCENT = ((int)'%');
    public const int KEY_DOLLAR = ((int)'$');
    public const int KEY_AMPERSAND = ((int)'&');
    public const int KEY_QUOTE = ((int)'\'');
    public const int KEY_LEFTPAREN = ((int)'(');
    public const int KEY_RIGHTPARE = ((int)')');
    public const int KEY_ASTERISK = ((int)'*');
    public const int KEY_PLUS = ((int)'+');
    public const int KEY_COMMA = ((int)',');
    public const int KEY_MINUS = ((int)'-');
    public const int KEY_PERIOD = ((int)'.');
    public const int KEY_SLASH = ((int)'/');
    public const int KEY_0 = ((int)'0');
    public const int KEY_1 = ((int)'1');
    public const int KEY_2 = ((int)'2');
    public const int KEY_3 = ((int)'3');
    public const int KEY_4 = ((int)'4');
    public const int KEY_5 = ((int)'5');
    public const int KEY_6 = ((int)'6');
    public const int KEY_7 = ((int)'7');
    public const int KEY_8 = ((int)'8');
    public const int KEY_9 = ((int)'9');
    public const int KEY_COLON = ((int)':');
    public const int KEY_SEMICOLON = ((int)';');
    public const int KEY_LESS = ((int)'<');
    public const int KEY_EQUALS = ((int)'=');
    public const int KEY_GREATER = ((int)'>');
    public const int KEY_QUESTION = ((int)'?');
    public const int KEY_AT = ((int)'@');
    public const int KEY_LEFTBRACK = ((int)'[');
    public const int KEY_BACKSLASH = ((int)'\\');
    public const int KEY_RIGHTBRAC = ((int)']');
    public const int KEY_CARET = ((int)'^');
    public const int KEY_UNDERSCORE = ((int)'_');
    public const int KEY_BACKQUOTE = ((int)'`');
    public const int KEY_A = ((int)'a');
    public const int KEY_B = ((int)'b');
    public const int KEY_C = ((int)'c');
    public const int KEY_D = ((int)'d');
    public const int KEY_E = ((int)'e');
    public const int KEY_F = ((int)'f');
    public const int KEY_G = ((int)'g');
    public const int KEY_H = ((int)'h');
    public const int KEY_I = ((int)'i');
    public const int KEY_J = ((int)'j');
    public const int KEY_K = ((int)'k');
    public const int KEY_L = ((int)'l');
    public const int KEY_M = ((int)'m');
    public const int KEY_N = ((int)'n');
    public const int KEY_O = ((int)'o');
    public const int KEY_P = ((int)'p');
    public const int KEY_Q = ((int)'q');
    public const int KEY_R = ((int)'r');
    public const int KEY_S = ((int)'s');
    public const int KEY_T = ((int)'t');
    public const int KEY_U = ((int)'u');
    public const int KEY_V = ((int)'v');
    public const int KEY_W = ((int)'w');
    public const int KEY_X = ((int)'x');
    public const int KEY_Y = ((int)'y');
    public const int KEY_Z = ((int)'z');
    public const int KEY_DELETE = ((int)127);

    public const int KEY_RIGHT = ((int)'R');
    public const int KEY_LEFT = ((int)'L');
    public const int KEY_DOWN = ((int)'D');
    public const int KEY_UP = ((int)'U');
    public const int KEY_CTRL = ((int)'C');
    public const int KEY_SHIFT = ((int)'S');
    public const int KEY_ALT = ((int)'A');

}