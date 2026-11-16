using System;
using System.Reflection;
using UnityEngine;

public static class AndroidPerformanceSettingsCheck
{
    public static void Run()
    {
        var type = typeof(AndroidNativeInput).Assembly.GetType("AndroidPerformanceSettings", true);
        var scaled = type.GetMethod("ScaledSize", BindingFlags.Static | BindingFlags.NonPublic);
        var applies = type.GetMethod("UsesLauncherResolution", BindingFlags.Static | BindingFlags.NonPublic);
        Vector2Int Size(int w, int h, int scale) => (Vector2Int)scaled.Invoke(null, new object[] { w, h, scale });
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        Check(!(bool)applies.Invoke(null, new object[] { 0 }) && !(bool)applies.Invoke(null, new object[] { 25 }), "game default and invalid choices must not override");
        Check((bool)applies.Invoke(null, new object[] { 100 }) && (bool)applies.Invoke(null, new object[] { 50 }), "explicit native and reduced choices must override");
        Check(Size(1080, 1920, 100) == new Vector2Int(1920, 1080), "native portrait normalization");
        Check(Size(1920, 1080, 75) == new Vector2Int(1440, 810), "75 percent");
        Check(Size(1920, 1080, 50) == new Vector2Int(960, 540), "50 percent");
        Check(Size(2401, 1081, 50) == new Vector2Int(1200, 540), "even dimensions");
        Check(Size(0, 1080, 50) == Vector2Int.zero && Size(1920, 1080, 25) == Vector2Int.zero, "invalid dimensions or scale");
        Debug.Log("AndroidPerformanceSettingsCheck PASS");
    }
}
