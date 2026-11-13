using System;
using System.Reflection;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HUD;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public static class ColorFilterPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception("ColorFilterPortCheck: " + message); }
    static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
    static float Interp(ColorFilterUI ui) => (float)typeof(ColorFilterUI).GetField("interpValue", Private).GetValue(ui);

    public static void Run()
    {
        var root = new GameObject("color filter test");
        root.SetActive(false);
        var ui = root.AddComponent<ColorFilterUI>();
        ui.group = root.AddComponent<CanvasGroup>();
        ui.i_color = new GameObject("image", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
        ui.i_color.transform.SetParent(root.transform);
        var volume = root.AddComponent<ColorFilter>();
        volume.color = new Color(0.8f, 0.4f, 0.2f, 0.9f);
        var camera = new GameObject("test camera"); camera.tag = "MainCamera";
        var other = new GameObject("other");
        var cameraCollider = camera.AddComponent<BoxCollider>();
        var otherCollider = other.AddComponent<BoxCollider>();
        var oldEnter = ColorFilter.A_FilterEnter;
        var oldExit = ColorFilter.A_FilterExit;
        try
        {
            ColorFilter.A_FilterEnter = null; ColorFilter.A_FilterExit = null;
            Call(ui, "Awake");
            ui.group.alpha = 0;
            Call(volume, "OnTriggerEnter", otherCollider);
            Check(!(bool)typeof(ColorFilterUI).GetField("usingFilter", Private).GetValue(ui), "other collider ignored");
            Call(volume, "OnTriggerEnter", cameraCollider);
            Color color = ui.i_color.color;
            Check(Mathf.Abs(color.r - .4f) < .001f && Mathf.Abs(color.g - .2f) < .001f && Mathf.Abs(color.b - .1f) < .001f && Mathf.Abs(color.a - .6f) < .001f, "filter color");
            Check((bool)typeof(ColorFilterUI).GetField("usingFilter", Private).GetValue(ui), "camera enter enables filter");
            typeof(ColorFilterUI).GetField("interpValue", Private).SetValue(ui, .4f);
            Call(ui, "Update");
            Check(Mathf.Abs(ui.group.alpha - Easing.InOutQuad(Interp(ui))) < .001f && Interp(ui) >= .4f, "fade in uses quadratic easing");
            Call(volume, "OnTriggerExit", cameraCollider);
            Check(!(bool)typeof(ColorFilterUI).GetField("usingFilter", Private).GetValue(ui), "camera exit disables filter");
            Call(ui, "Update");
            Check(Mathf.Abs(ui.group.alpha - Easing.InOutQuad(Interp(ui))) < .001f, "fade out uses quadratic easing");
            Call(volume, "OnTriggerEnter", cameraCollider); ui.ResetFilter();
            Check(!(bool)typeof(ColorFilterUI).GetField("usingFilter", Private).GetValue(ui), "reset disables filter");
            Call(ui, "OnDestroy");
            Check(ColorFilter.A_FilterEnter == null && ColorFilter.A_FilterExit == null, "destroy unsubscribes");
            Debug.Log("ColorFilterPortCheck passed: camera-only events, tint, easing, reset and unsubscribe");
        }
        finally
        {
            ColorFilter.A_FilterEnter = oldEnter; ColorFilter.A_FilterExit = oldExit;
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(camera); UnityEngine.Object.DestroyImmediate(other);
        }
    }
}
