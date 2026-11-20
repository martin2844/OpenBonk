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
    internal static readonly AndroidTouchLookGestures LookGestures = new AndroidTouchLookGestures();
    private static bool jumpRequested;

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
        if (held)
        {
            button.pendingDown = true;
            if (index == 0) jumpRequested = true;
        }
        else button.pendingUp = true;
    }

    // Physics can consume this request as soon as the UI reports pointer down.
    internal static bool TakeJumpRequest()
    {
        bool requested = jumpRequested;
        jumpRequested = false;
        return requested;
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
        jumpRequested = false;
        LookGestures.Clear();
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

// Keep one owner per action, independently of its held gameplay button. A
// moving action finger wins over background look; simultaneous action drags
// use the most recently pressed finger instead of adding camera motion twice.
internal sealed class AndroidTouchLookGestures
{
    private readonly int[] pointers = { int.MinValue, int.MinValue, int.MinValue, int.MinValue };
    private readonly long[] order = new long[4];
    private readonly bool[] seen = new bool[4];
    private long sequence, selectedOrder;
    private Vector2 delta;

    private static int Slot(string action)
    {
        switch (action)
        {
            case "": return 0;
            case "Jump": return 1;
            case "Slide": return 2;
            case "Aim": return 3;
            default: return -1;
        }
    }

    internal void Capture(string action, int pointer)
    {
        int slot = Slot(action);
        if (slot < 0 || pointers[slot] != int.MinValue) return;
        pointers[slot] = pointer;
        order[slot] = slot == 0 ? 0 : ++sequence;
    }

    internal void Release(string action, int pointer)
    {
        int slot = Slot(action);
        if (slot >= 0 && pointers[slot] == pointer) pointers[slot] = int.MinValue;
    }

    internal void Clear()
    {
        for (int i = 0; i < pointers.Length; i++) pointers[i] = int.MinValue;
        sequence = 0;
        BeginSample();
    }

    internal void BeginSample()
    {
        selectedOrder = -1;
        delta = Vector2.zero;
        for (int i = 0; i < seen.Length; i++) seen[i] = false;
    }

    internal void Observe(int pointer, TouchPhase phase, Vector2 motion)
    {
        for (int i = 0; i < pointers.Length; i++)
        {
            if (pointers[i] != pointer) continue;
            seen[i] = true;
            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled) pointers[i] = int.MinValue;
            else if (phase == TouchPhase.Moved && motion.sqrMagnitude > 0 && order[i] > selectedOrder)
            {
                selectedOrder = order[i];
                delta = motion;
            }
        }
    }

    internal Vector2 EndSample()
    {
        for (int i = 0; i < pointers.Length; i++)
            if (!seen[i]) pointers[i] = int.MinValue;
        return delta;
    }
}
