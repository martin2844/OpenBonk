using System;
using UnityEngine;

public static class MinimapZoomPortCheck
{
    private static void Check(bool value, string message) { if (!value) throw new Exception("MinimapZoomPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("minimap camera test");
        var camera = root.AddComponent<Camera>();
        try
        {
            camera.orthographic = true; camera.orthographicSize = 50;
            OpenBonkMinimapPolicy.Reset();
            OpenBonkChestTracker.SetEnabled(false); OpenBonkItemGrantPolicy.SetEnabled(false);
            Check(!OpenBonkOptionalUiPolicy.TryApplyMinimap("minimap_zoom", 0), "zero result rejected");
            Check(OpenBonkMinimapPolicy.Zoom == 1 && !OpenBonkChestTracker.Enabled && !OpenBonkItemGrantPolicy.Enabled, "zero activates no policies");
            Check(OpenBonkOptionalUiPolicy.TryApplyMinimap("minimap_zoom", 2), "zoom dispatcher accepts numeric result");
            Check(!OpenBonkChestTracker.Enabled && !OpenBonkItemGrantPolicy.Enabled, "zoom does not activate other policies");
            OpenBonkMinimapPolicy.Reset(); OpenBonkMinimapPolicy.Apply(camera);
            Check(camera.orthographicSize == 50, "disabled leaves original size");
            Check(!OpenBonkMinimapPolicy.TrySetZoom(double.NaN) && !OpenBonkMinimapPolicy.TrySetZoom(double.PositiveInfinity), "nonfinite rejected");
            Check(!OpenBonkMinimapPolicy.TrySetZoom(0.99) && !OpenBonkMinimapPolicy.TrySetZoom(4.01), "out of range rejected");
            Check(OpenBonkMinimapPolicy.Zoom == 1, "invalid values do not mutate policy");
            Check(OpenBonkMinimapPolicy.TrySetZoom(2), "valid value accepted");
            OpenBonkMinimapPolicy.Apply(camera); Check(camera.orthographicSize == 100, "camera coverage applied");
            Check(OpenBonkMinimapPolicy.TrySetZoom(4), "upper boundary accepted");
            OpenBonkMinimapPolicy.Reset(); camera.orthographicSize = 50;
            OpenBonkMinimapPolicy.Apply(camera); Check(camera.orthographicSize == 50, "disabled restores newly created camera default");
            OpenBonkMinimapPolicy.Apply(null);
            Debug.Log("MinimapZoomPortCheck passed");
        }
        finally { OpenBonkMinimapPolicy.Reset(); UnityEngine.Object.DestroyImmediate(root); }
    }
}
