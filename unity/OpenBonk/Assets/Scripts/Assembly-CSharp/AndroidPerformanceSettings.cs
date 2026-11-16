using UnityEngine;

// Keep the render resolution choice separate from the desktop mode index.
// The launcher preference is read before the first Unity scene so that menus
// and gameplay use the same setting. The default leaves the game's own
// resolution selection in control; an explicit launcher choice takes priority.
internal static class AndroidPerformanceSettings
{
    internal static bool OverrideActive { get; private set; }

    internal static bool UsesLauncherResolution(int percent) => percent == 50 || percent == 75 || percent == 100;

    internal static Vector2Int ScaledSize(int width, int height, int percent)
    {
        if (width <= 0 || height <= 0 || (percent != 50 && percent != 75 && percent != 100)) return Vector2Int.zero;
        int longSide = Mathf.Max(width, height), shortSide = Mathf.Min(width, height);
        int scaledWidth = Mathf.Max(2, (longSide * percent / 100) & ~1);
        int scaledHeight = Mathf.Max(2, (shortSide * percent / 100) & ~1);
        return new Vector2Int(scaledWidth, scaledHeight);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        try
        {
            using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
            using var prefs = activity.Call<AndroidJavaObject>("getSharedPreferences", "openbonk_launcher", 0);
            int percent = prefs.Call<int>("getInt", "render_resolution_percent", 0);
            OverrideActive = UsesLauncherResolution(percent);
            if (!OverrideActive) return;
            Resolution physical = Screen.currentResolution;
            int width = physical.width > 0 ? physical.width : Screen.width;
            int height = physical.height > 0 ? physical.height : Screen.height;
            Vector2Int size = ScaledSize(width, height, percent);
            if (size == Vector2Int.zero) { OverrideActive = false; return; }
            Screen.SetResolution(size.x, size.y, FullScreenMode.FullScreenWindow);
            Debug.Log("OpenBonk render resolution requested: " + size.x + "x" + size.y + " (" + percent + "%)");
            var reporter = new GameObject("Android resolution report"); Object.DontDestroyOnLoad(reporter);
            reporter.AddComponent<AndroidResolutionReport>();
        }
        catch (System.Exception e) { OverrideActive = false; Debug.LogWarning("Could not apply render resolution: " + e.Message); }
    }
#endif
}

#if UNITY_ANDROID && !UNITY_EDITOR
internal sealed class AndroidResolutionReport : MonoBehaviour
{
    private System.Collections.IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(3f);
        Debug.Log("OpenBonk render resolution active: " + Screen.width + "x" + Screen.height);
        Destroy(gameObject);
    }
}
#endif
