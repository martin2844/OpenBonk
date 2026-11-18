using System;
using System.Reflection;
using UnityEngine;

public static class AndroidTouchInputCheck
{
    public static void Run()
    {
        var type = typeof(AndroidNativeInput).Assembly.GetType("AndroidTouchInput");
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        object Call(string method, params object[] args) => type.GetMethod(method, flags).Invoke(null, args);
        bool State(string method, string action) => (bool)Call(method, action);
        void Require(bool condition, string message) { if (!condition) throw new Exception("Touch input: " + message); }

        Call("ReleaseAll"); Call("Sample");
        Call("SetButton", "Jump", true); Call("Sample");
        Require(State("Held", "Jump") && State("Down", "Jump") && !State("Up", "Jump"), "press");
        Call("Sample"); Require(State("Held", "Jump") && !State("Down", "Jump"), "held without repeat");
        Require((bool)Call("TakeJumpRequest"), "press remains available to the physics step after sampling");
        Require(!(bool)Call("TakeJumpRequest"), "holding jump cannot queue a second press");
        Call("SetButton", "Interact", true); Call("Sample");
        Require(State("Held", "Jump") && State("Held", "Interact") && State("Down", "Interact"), "simultaneous buttons");
        Call("SetButton", "Jump", false); Call("Sample");
        Require(!State("Held", "Jump") && State("Up", "Jump") && State("Held", "Interact"), "independent release");
        Call("SetButton", "Slide", true); Call("SetButton", "Slide", false); Call("Sample");
        Require(State("Down", "Slide") && State("Up", "Slide") && !State("Held", "Slide"), "tap between input samples");
        type.GetField("Move", flags).SetValue(null, Vector2.one);
        type.GetField("Look", flags).SetValue(null, Vector2.one);
        Call("ReleaseAll"); Call("Sample");
        Require(!State("Held", "Interact") && State("Up", "Interact") &&
                (Vector2)type.GetField("Move", flags).GetValue(null) == Vector2.zero &&
                (Vector2)type.GetField("Look", flags).GetValue(null) == Vector2.zero, "cancel on hide");
        Call("SetButton", "Jump", true); Call("SetButton", "Jump", false);
        Require((bool)Call("TakeJumpRequest"), "a short tap still reaches physics");
        Call("SetButton", "Jump", true); Call("ReleaseAll");
        Require(!(bool)Call("TakeJumpRequest"), "hiding the overlay cancels a queued jump");
        Debug.Log("AndroidTouchInputCheck PASS: edges, simultaneous buttons, fast tap and cancellation");
    }
}
