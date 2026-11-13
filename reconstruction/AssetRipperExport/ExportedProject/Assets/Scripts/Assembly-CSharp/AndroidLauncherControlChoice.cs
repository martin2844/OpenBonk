#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

// Only the experimental Android launcher sends this optional choice. Existing
// installs and direct launches leave the saved control setting untouched.
internal static class AndroidLauncherControlChoice
{
    private static int choice = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        choice = intent.Call<int>("getIntExtra", "megabonk_launcher_touch_mode", -1);
        if (choice != 0 && choice != 1) return;
        SaveManager.A_SavesLoaded -= Apply;
        SaveManager.A_SavesLoaded += Apply;
        if (SaveManager.loaded) Apply();
    }

    private static void Apply()
    {
        var manager = SaveManager.Instance;
        if (manager?.config?.cfControlSettings == null) return;
        manager.config.cfControlSettings.touch_controls = choice;
        manager.SaveConfig();
        try
        {
            using var launcher = new AndroidJavaClass("com.megabonk.launcher.SplitDataLauncher");
            bool acknowledged = launcher.CallStatic<bool>("acknowledgeControlChoice", choice);
            Debug.Log($"Launcher control choice applied: touch={choice}, acknowledged={acknowledged}");
        }
        catch (System.Exception error)
        {
            Debug.LogWarning($"Launcher control choice could not be acknowledged: {error.Message}");
        }
        SaveManager.A_SavesLoaded -= Apply;
        choice = -1;
    }
}
#endif
