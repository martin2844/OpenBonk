using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public static class OpenBonkTouchPreferencesCheck
{
    public static void Run()
    {
        var asm = typeof(AndroidNativeInput).Assembly;
        var editorType = asm.GetType("AndroidTouchLayoutEditor", true);
        var targetType = asm.GetType("AndroidTouchLayoutTarget", true);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var root = new GameObject("Touch test", typeof(RectTransform));
        var safe = (RectTransform)root.transform; safe.sizeDelta = new Vector2(1920, 1080);
        var control = new GameObject("test", typeof(RectTransform)); control.transform.SetParent(safe, false);
        var editor = root.AddComponent(editorType); var target = control.AddComponent(targetType);
        string prefix = "openbonk.test.preferences."; string slot = prefix + "profile.";
        void Call(string name, params object[] args) => targetType.GetMethod(name, flags).Invoke(target, args);
        void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        try {
            Call("Setup", editor, safe, "test", null, prefix);
            Call("SetVisible", false); Call("SetOpacity", .4f); Call("CopyToProfile", slot);
            var group = control.GetComponent<CanvasGroup>();
            Require(!group.blocksRaycasts && group.alpha == 0, "disabled control still active");
            Call("SetVisible", true); Call("SetOpacity", .9f); Call("CopyFromProfile", slot);
            Require(PlayerPrefs.GetInt(prefix + "test.visible") == 0, "visibility profile restore");
            Require(Math.Abs(PlayerPrefs.GetFloat(prefix + "test.opacity") - .4f) < .001f, "opacity profile restore");
            Require(!group.blocksRaycasts && group.alpha == 0, "restored control still active");
            Call("ResetLayout");
            Require(group.blocksRaycasts && group.alpha == 1, "reset did not restore control");
            var stickType = asm.GetType("AndroidTouchStick", true);
            var inputType = asm.GetType("AndroidTouchInput", true);
            var stickObject = new GameObject("Movement", typeof(RectTransform)); stickObject.transform.SetParent(safe, false);
            var stick = stickObject.AddComponent(stickType);
            var eventObject = new GameObject("Event system", typeof(EventSystem));
            var pointer = new PointerEventData(eventObject.GetComponent<EventSystem>()) { pointerId = 42 };
            const string autoKey = "openbonk.touch.layout.autorun";
            bool hadAuto = PlayerPrefs.HasKey(autoKey); int oldAuto = PlayerPrefs.GetInt(autoKey);
            try {
                PlayerPrefs.SetInt(autoKey, 1);
                stickType.GetField("pointer", flags).SetValue(stick, 42);
                stickType.GetField("autoRunArmed", flags).SetValue(stick, true);
                stickType.GetMethod("OnPointerUp", flags).Invoke(stick, new object[] { pointer });
                var move = inputType.GetField("Move", BindingFlags.Static | BindingFlags.NonPublic);
                Require((Vector2)move.GetValue(null) == Vector2.up, "autorun did not start");
                stickType.GetMethod("OnPointerDown", flags).Invoke(stick, new object[] { pointer });
                Require((Vector2)move.GetValue(null) == Vector2.zero, "autorun did not cancel on tap");
            } finally { if (hadAuto) PlayerPrefs.SetInt(autoKey, oldAuto); else PlayerPrefs.DeleteKey(autoKey); UnityEngine.Object.DestroyImmediate(eventObject); }
            Debug.Log("OpenBonkTouchPreferencesCheck PASS: disabled controls, opacity, profile, reset and autorun cancellation");
        } finally {
            foreach (string key in new[] { "test.visible", "test.opacity", "profile.test.visible", "profile.test.opacity", "profile.test.opacity.exists", "profile.test.action", "profile.test.x.exists", "profile.test.y.exists", "profile.test.scale.exists" }) PlayerPrefs.DeleteKey(prefix + key);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
