using System;
using UnityEngine;

// Pointer callbacks can arrive after gameplay input was sampled. Queue both
// edges so even a short tap is delivered on the next input frame.
internal static class AndroidTouchInput
{
    private sealed class ButtonState
    {
        internal bool held, pendingDown, pendingUp, down, up;
    }

    private static readonly string[] Names = { "Jump", "Interact", "Slide", "Aim", "MapOverlay" };
    private static readonly ButtonState[] Buttons =
        { new ButtonState(), new ButtonState(), new ButtonState(), new ButtonState(), new ButtonState() };

    internal static Vector2 Move;
    internal static Vector2 Look;

    private static int Index(string action)
    {
        if (string.Equals(action, "Jump Bhop", StringComparison.OrdinalIgnoreCase)) return 0;
        for (int i = 0; i < Names.Length; i++)
            if (string.Equals(action, Names[i], StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    internal static void SetButton(string action, bool held)
    {
        int index = Index(action);
        if (index < 0) return;
        var button = Buttons[index];
        if (button.held == held) return;
        button.held = held;
        if (held) button.pendingDown = true;
        else button.pendingUp = true;
    }

    internal static void Sample()
    {
        foreach (var button in Buttons)
        {
            button.down = button.pendingDown;
            button.up = button.pendingUp;
            button.pendingDown = button.pendingUp = false;
        }
    }

    internal static bool Held(string action) { int i = Index(action); return i >= 0 && Buttons[i].held; }
    internal static bool Down(string action) { int i = Index(action); return i >= 0 && Buttons[i].down; }
    internal static bool Up(string action) { int i = Index(action); return i >= 0 && Buttons[i].up; }

    internal static void ReleaseAll()
    {
        Move = Look = Vector2.zero;
        foreach (var button in Buttons)
        {
            if (button.held) button.pendingUp = true;
            button.held = false;
            button.pendingDown = false;
            button.down = false;
        }
    }
}
