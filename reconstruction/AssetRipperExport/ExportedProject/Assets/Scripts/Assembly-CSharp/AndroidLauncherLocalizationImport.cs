#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

// The experimental launcher places a temporary ZIP in its private cache.
// This component consumes it once and never writes to the source document.
internal static class AndroidLauncherLocalizationImport
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Run()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_import_original_texts", false)) return;
        using var cache = activity.Call<AndroidJavaObject>("getCacheDir");
        string path = Path.Combine(cache.Call<string>("getAbsolutePath"), "megabonk-original-texts.pending.zip");
        bool ok = false;
        try
        {
            using var file = File.OpenRead(path);
            string active = AndroidOriginalLocalizationStore.Import(file, Path.Combine(Application.persistentDataPath, "OriginalLocalization"));
            ok = true; Debug.Log("Launcher original game text import succeeded: " + Path.GetFileName(active));
        }
        catch (Exception e) { Debug.LogWarning("Launcher original game text import failed: " + e.Message); }
        // Keep a failed import queued so that reopening the game can retry it.
        // The selected document is copied into the app cache by the launcher;
        // deleting this copy on failure made transient errors unrecoverable.
        if (ok) { try { if (File.Exists(path)) File.Delete(path); } catch (Exception e) { Debug.LogWarning("Could not clear temporary localization ZIP: " + e.Message); } }
        try { using var launcher = new AndroidJavaClass("com.megabonk.launcher.SplitDataLauncher"); launcher.CallStatic("showImportResult", ok); }
        catch (Exception e) { Debug.LogWarning("Could not display localization import result: " + e.Message); }
    }
}
#endif
