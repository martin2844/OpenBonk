#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using Object = UnityEngine.Object;

public static class AndroidPreloadedAssetsCheck
{
    public static void Run()
    {
        var ui = AssetDatabase.LoadMainAssetAtPath("Assets/MonoBehaviour/UIParticle.asset");
        var localization = AssetDatabase.LoadAssetAtPath<LocalizationSettings>("Assets/MonoBehaviour/Localization Settings.asset");
        if (ui == null || localization == null) throw new Exception("Expected preloaded assets are missing");
        var input = new Object[] { ui, null, localization, ui };
        var filtered = NativeAndroidBuild.FilterDesktopOnlyPreloads(input);
        if (filtered.Length != 2 || filtered[0] != null || filtered[1] != localization)
            throw new Exception("Preload filtering removed or retained an unexpected asset");
        if (input.Length != 4 || input[0] != ui || input[3] != ui)
            throw new Exception("Preload filtering modified the source list");
        Debug.Log("AndroidPreloadedAssetsCheck passed: desktop-only asset excluded, localization retained");
    }
}
#endif
