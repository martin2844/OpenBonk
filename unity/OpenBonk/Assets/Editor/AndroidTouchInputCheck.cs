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

        // UI pointer-down can run after Update; the next physics tick must
        // receive it without waiting for another render sample or pointer-up.
        Call("Sample"); Call("SetButton", "Jump", true);
        Require((bool)Call("TakeJumpRequest") && State("Held", "Jump"), "first held jump reaches physics before the next render sample");
        for (int frame = 0; frame < 120; frame++)
        {
            Call("Sample"); Call("SetButton", "Jump", true);
            Require(!(bool)Call("TakeJumpRequest") && State("Held", "Jump"), "held jump never becomes autorepeat");
        }
        Call("SetButton", "Jump", false); Call("Sample");
        Require(!(bool)Call("TakeJumpRequest"), "release cannot generate the first jump");
        Call("SetButton", "Jump", true); Call("SetButton", "Jump", false);
        Require((bool)Call("TakeJumpRequest"), "quick tap uses the same next-physics path as holding");
        Call("SetButton", "Jump", true);
        for (int frame = 0; frame < 5; frame++) Call("Sample");
        Require((bool)Call("TakeJumpRequest"), "render sampling cannot lose a pending physics press");
        Call("ReleaseAll"); Call("Sample");

        var gestures = type.GetField("LookGestures", flags).GetValue(null);
        var gestureType = gestures.GetType();
        object Gesture(string method, params object[] args) => gestureType.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(gestures, args);
        Vector2 Motion(params (int id, TouchPhase phase, Vector2 delta)[] touches)
        {
            Gesture("BeginSample");
            foreach (var touch in touches) Gesture("Observe", touch.id, touch.phase, touch.delta);
            return (Vector2)Gesture("EndSample");
        }
        var drag = new Vector2(14, -9);
        var freeDrag = new Vector2(-3, 5);
        foreach (string action in new[] { "Jump", "Aim", "Slide" })
        {
            Call("ReleaseAll");
            Call("SetButton", action, true); Gesture("Capture", action, 101);
            Gesture("Capture", "", 201);
            Require(Motion((101, TouchPhase.Moved, drag), (201, TouchPhase.Moved, freeDrag)) == drag && State("Held", action),
                action + " drags camera while its gameplay action stays held");
            Require(Motion((101, TouchPhase.Stationary, Vector2.zero), (201, TouchPhase.Moved, freeDrag)) == freeDrag,
                "stationary " + action + " does not block another look finger");
            Gesture("Release", action, 999);
            Require(Motion((101, TouchPhase.Moved, drag), (201, TouchPhase.Stationary, Vector2.zero)) == drag,
                "unrelated pointer-up cannot release " + action);
            Gesture("Release", action, 101); Call("SetButton", action, false);
            Require(Motion((101, TouchPhase.Moved, drag), (201, TouchPhase.Moved, freeDrag)) == freeDrag,
                "releasing " + action + " returns camera ownership to free look");
        }
        Call("ReleaseAll");
        Gesture("Capture", "Aim", 301); Gesture("Capture", "Slide", 302);
        Require(Motion((301, TouchPhase.Moved, freeDrag), (302, TouchPhase.Moved, drag)) == drag,
            "latest moving action finger wins without summing deltas");
        Require(Motion((302, TouchPhase.Moved, drag), (301, TouchPhase.Moved, freeDrag)) == drag,
            "Android touch array order does not change camera ownership");
        Gesture("Release", "Slide", 302);
        Require(Motion((301, TouchPhase.Moved, freeDrag)) == freeDrag, "releasing slide preserves held aim drag");
        Require(Motion((301, TouchPhase.Canceled, drag)) == Vector2.zero, "canceled finger cannot rotate camera");
        Gesture("Capture", "Aim", 303);
        Require(Motion((303, TouchPhase.Moved, drag)) == drag, "canceled action accepts a new finger");
        Require(Motion() == Vector2.zero, "missing pointer clears capture even without pointer-up");
        Require(Motion((303, TouchPhase.Moved, drag)) == Vector2.zero, "missing pointer cannot resume stale camera motion");
        Gesture("Capture", "Interact", 401); Gesture("Capture", "MapOverlay", 402);
        Require(Motion((401, TouchPhase.Moved, drag), (402, TouchPhase.Moved, freeDrag), (999, TouchPhase.Moved, drag)) == Vector2.zero,
            "interaction, map and uncaptured movement fingers do not rotate camera");
        Gesture("Capture", "Aim", 501); Call("SetButton", "Aim", true); Call("ReleaseAll");
        Require(Motion((501, TouchPhase.Moved, drag)) == Vector2.zero && !State("Held", "Aim"), "focus loss clears held action and camera capture together");
        Call("Sample");
        Debug.Log("AndroidTouchInputCheck PASS: held/tap jump before render sampling, no autorepeat, Jump/Aim/Slide camera drag, simultaneous ownership, cancellation and focus reset");
    }
}
