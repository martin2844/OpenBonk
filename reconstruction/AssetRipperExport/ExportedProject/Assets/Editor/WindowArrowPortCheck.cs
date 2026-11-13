using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class WindowArrowPortCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var root = new GameObject("window arrow fixture"); root.SetActive(false);
        var always = root.AddComponent<AlwaysUi>(); AlwaysUi.Instance = always;
        var arrow = root.AddComponent<SelectionArrow>(); always.selectionArrow = arrow;
        var image = new GameObject("arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); image.transform.SetParent(root.transform); arrow.renderer = image.GetComponent<Image>(); arrow.renderer.enabled = true;
        var selection = new GameObject("selected", typeof(RectTransform)); selection.transform.SetParent(root.transform);
        typeof(SelectionArrow).GetField("selectedRect", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(arrow, selection.GetComponent<RectTransform>());
        var first = root.AddComponent<Window>(); var active = root.AddComponent<Window>();
        try
        {
            WindowManager.openWindows.Clear(); WindowManager.openWindows.Add(first); WindowManager.openWindows.Add(active); WindowManager.activeWindow = active;
            WindowManager.WindowClosed(first); Check(arrow.renderer.enabled && WindowManager.activeWindow == active, "closing a non-active window preserves focus");
            WindowManager.WindowClosed(active); Check(!arrow.renderer.enabled && WindowManager.activeWindow == null && WindowManager.openWindows.Count == 0, "closing last window hides selection arrow");
            Debug.Log("WindowArrowPortCheck passed: preserve nested focus and hide arrow when last window closes");
        }
        finally { WindowManager.openWindows.Clear(); WindowManager.activeWindow = null; AlwaysUi.Instance = null; UnityEngine.Object.DestroyImmediate(root); }
    }
}
