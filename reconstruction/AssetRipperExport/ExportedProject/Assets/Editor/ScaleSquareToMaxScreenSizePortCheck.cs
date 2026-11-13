using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class ScaleSquareToMaxScreenSizePortCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception("ScaleSquareToMaxScreenSizePortCheck: " + message); }
    public static void Run()
    {
        var chooser = typeof(ScaleSquareToMaxScreenSize).GetMethod("SelectSize", BindingFlags.NonPublic | BindingFlags.Static);
        var reference = new Vector2(1920, 1080);
        float Select(float width, float height) => (float)chooser.Invoke(null, new object[] { width, height, reference });
        Check(Select(1920, 1080) == 1920f, "landscape selects reference width");
        Check(Select(1080, 1920) == 1080f, "portrait selects reference height");
        Check(Select(1000, 1000) == 1080f, "square selects reference height");
        var root = new GameObject("square scaler fixture");
        root.SetActive(false);
        var canvas = root.AddComponent<CanvasScaler>(); canvas.referenceResolution = reference;
        var effect = new GameObject("square effect", typeof(RectTransform)); effect.transform.SetParent(root.transform, false);
        var component = effect.AddComponent<ScaleSquareToMaxScreenSize>(); component.canvasScaler = canvas;
        try
        {
            root.SetActive(true);
            // Explicit invocation also covers edit-mode fixtures where activation callbacks may not run.
            typeof(ScaleSquareToMaxScreenSize).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
            float size = Select(Screen.width, Screen.height);
            Check(effect.GetComponent<RectTransform>().sizeDelta == new Vector2(size, size), "OnEnable applies selected square size");
            Debug.Log("ScaleSquareToMaxScreenSizePortCheck passed: landscape, portrait, square and enabled RectTransform");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
