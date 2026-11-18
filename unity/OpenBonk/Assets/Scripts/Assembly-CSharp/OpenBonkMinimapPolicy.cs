using System;
using UnityEngine;

// A bounded multiplier on the existing minimap camera, not the full-map camera.
public static class OpenBonkMinimapPolicy
{
    public static float Zoom { get; private set; } = 1f;
    public static void Reset() { Zoom = 1f; }
    public static bool TrySetZoom(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 1 || value > 4) return false;
        Zoom = (float)value;
        return true;
    }
    public static void Apply(Camera camera)
    {
        if (camera != null) camera.orthographicSize *= Zoom;
    }
}
