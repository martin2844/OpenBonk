#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

// The experimental launcher places a temporary ZIP in its private files.
// This component consumes it once and never writes to the source document.
internal static class AndroidLauncherLocalizationImport
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Run()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        try
        {
            string existing = AndroidOriginalLocalizationStore.ActiveDirectory(Path.Combine(Application.persistentDataPath, "OriginalLocalization"));
            if (existing != null) { using var launcher = new AndroidJavaClass("com.megabonk.launcher.SplitDataLauncher"); launcher.CallStatic("registerGameDataGeneration", Path.GetFileName(existing)); }
        }
        catch (Exception e) { Debug.LogWarning("Could not register existing game data: " + e.Message); }
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_import_original_texts", false)) return;
        using var files = activity.Call<AndroidJavaObject>("getFilesDir");
        string path = Path.Combine(files.Call<string>("getAbsolutePath"), "megabonk-original-texts.pending.zip");
        bool ok = false;
        string generation = "";
        try
        {
            using var file = File.OpenRead(path);
            string active = AndroidOriginalLocalizationStore.Import(file, Path.Combine(Application.persistentDataPath, "OriginalLocalization"));
            generation = Path.GetFileName(active);
            ok = true; Debug.Log("Launcher original game text import succeeded: " + Path.GetFileName(active));
        }
        catch (Exception e) { Debug.LogWarning("Launcher original game text import failed: " + e.Message); }
        // Keep a failed import queued so that reopening the game can retry it.
        // The selected document is copied into private app files by the launcher;
        // deleting this copy on failure made transient errors unrecoverable.
        if (ok) { try { if (File.Exists(path)) File.Delete(path); } catch (Exception e) { Debug.LogWarning("Could not clear temporary localization ZIP: " + e.Message); } }
        try { using var launcher = new AndroidJavaClass("com.megabonk.launcher.SplitDataLauncher"); launcher.CallStatic("showImportResult", ok, generation); }
        catch (Exception e) { Debug.LogWarning("Could not display localization import result: " + e.Message); }
    }
}
#endif
