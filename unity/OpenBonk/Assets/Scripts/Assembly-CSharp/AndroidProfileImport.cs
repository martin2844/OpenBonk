#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

// Split package only. Called synchronously before SaveManager reads any files.
internal static class AndroidProfileImport
{
    internal static void Prepare(string cloudRoot, string account, bool allowImport)
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest") return;
        using var files = activity.Call<AndroidJavaObject>("getFilesDir");
        using var cache = activity.Call<AndroidJavaObject>("getCacheDir");
        string dir = files.Call<string>("getAbsolutePath");
        string cacheDir = cache.Call<string>("getAbsolutePath");
        string pending = Path.Combine(dir, "megabonk-profile.pending.zip");
        string working = Path.Combine(dir, "megabonk-profile.working.zip");
        string legacyPending = Path.Combine(cacheDir, "megabonk-profile.pending.zip");
        string legacyWorking = Path.Combine(cacheDir, "megabonk-profile.working.zip");
        AndroidProfileArchive.Recover(cloudRoot, account);
        if (File.Exists(legacyWorking))
        {
            try { File.Delete(legacyWorking); } catch (Exception e) { Debug.LogWarning("Could not clear interrupted cached profile ZIP: " + e.Message); }
            Notice("interrupted");
        }
        if (File.Exists(working))
        {
            // An earlier attempt was interrupted at an unknown point. Never
            // retry automatically: it may already have published the profile.
            try { File.Delete(working); } catch (Exception e) { Debug.LogWarning("Could not clear interrupted profile ZIP: " + e.Message); }
            Notice("interrupted");
        }
        // The launcher normally migrates this before starting Unity. If the
        // game was opened directly, preserve both conflicting requests rather
        // than applying either without the launcher's recovery flow.
        if (File.Exists(legacyPending))
        {
            if (File.Exists(pending)) { Debug.LogWarning("Conflicting queued profile ZIPs need launcher recovery"); Notice("failed"); return; }
            try { File.Move(legacyPending, pending); }
            catch (Exception e) { Debug.LogWarning("Could not move cached profile ZIP: " + e.Message); Notice("failed"); return; }
        }
        if (!allowImport || !File.Exists(pending)) return;
        try
        {
            // Move first so an interrupted import cannot be applied again
            // after the player has begun a run using the imported profile.
            if (File.Exists(working)) throw new IOException("Previous profile import needs attention");
            File.Move(pending, working);
            using (var file = File.OpenRead(working)) AndroidProfileArchive.Import(file, cloudRoot, account);
            try { File.Delete(working); } catch (Exception e) { Debug.LogWarning("Could not clear imported profile ZIP: " + e.Message); }
            Debug.Log("Launcher profile import succeeded"); Notice("success");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Launcher profile import failed: " + e.Message);
            // Failure recovery is mandatory before the normal load may run.
            AndroidProfileArchive.Recover(cloudRoot, account);
            try { if (File.Exists(working)) File.Delete(working); } catch (Exception cleanup) { Debug.LogWarning("Could not clear failed profile ZIP: " + cleanup.Message); }
            Notice("failed");
        }
    }
    private static void Notice(string status)
    {
        try { using var launcher = new AndroidJavaClass("com.megabonk.launcher.SplitDataLauncher"); launcher.CallStatic("showProfileImportResult", status); }
        catch (Exception e) { Debug.LogWarning("Could not show profile result: " + e.Message); }
    }
}
#endif
