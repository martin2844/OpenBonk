#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class AndroidSceneLocalizationBuildCheck
{
    public static void Run()
    {
        var projectSettings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>("Assets/MonoBehaviour/Localization Settings.asset");
        if (projectSettings == null) throw new Exception("Project localization settings missing");
        var previous = LocalizationSettings.HasSettings ? LocalizationSettings.Instance : null;
        int missingSettings = 0;
        Application.LogCallback callback = (message, stack, type) =>
        {
            if (message.Contains("There is no active LocalizationSettings")) missingSettings++;
        };
        try
        {
            LocalizationSettings.Instance = null;
            Application.logMessageReceived += callback;
            var method = typeof(NativeAndroidBuild).GetMethod("PrepareAndroidScenesWithLocalization", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new Exception("Scene preparation method missing");
            var scenes = method.Invoke(null, null) as string[];
            if (scenes == null || scenes.Length != 5) throw new Exception("Unexpected Android scene count");
            if (missingSettings != 0) throw new Exception("Missing localization settings while opening scenes: " + missingSettings);
            Debug.Log("AndroidSceneLocalizationBuildCheck passed: five scenes opened without missing settings");
        }
        finally
        {
            Application.logMessageReceived -= callback;
            LocalizationSettings.Instance = previous;
        }
    }
}
#endif
