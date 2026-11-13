#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;

// Explicit intent only. Writes to a unique test root in the current app's
// private storage, restores all changed static fields, and never yields while
// the test root is selected.
internal sealed class AndroidSaveFailureProbe : MonoBehaviour
{
    const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;
    const BindingFlags InstancePrivate = BindingFlags.NonPublic | BindingFlags.Instance;
    static FieldInfo Field(Type type, string name) => type.GetField(name, StaticPrivate);
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
    static bool HasListener(Action action, string name)
    {
        if (action == null) return false;
        foreach (var listener in action.GetInvocationList()) if (listener.Method.DeclaringType?.Name == name) return true;
        return false;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_save_failure_probe", false)) return;
        var go = new GameObject("save failure probe");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidSaveFailureProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (!SaveManager.loaded || SaveManager.Instance == null ||
            !HasListener(SaveManager.A_ProgressionSaved, "MyAchievements") || !HasListener(SaveManager.A_StatsSaved, "MyStats"))) yield return null;
        if (!SaveManager.loaded || SaveManager.Instance == null || !HasListener(SaveManager.A_ProgressionSaved, "MyAchievements") || !HasListener(SaveManager.A_StatsSaved, "MyStats"))
        { Debug.LogError("Android save failure probe INCONCLUSIVE: save services not ready"); yield break; }
        Run();
    }
    static void Run()
    {
        var type = typeof(SaveManager);
        var test = Field(type, "TEST_SAVES");
        var version = Field(type, "TEST_SAVES_VERSION");
        var id = Field(type, "steamIdSave");
        var achievementDirty = Field(typeof(MyAchievements), "hasUnsavedChanges");
        var statsDirty = Field(typeof(MyStats), "hasUnsavedChanges");
        object oldTest = test.GetValue(null), oldVersion = version.GetValue(null), oldId = id.GetValue(null);
        object oldAchievements = achievementDirty.GetValue(null), oldStats = statsDirty.GetValue(null);
        string root = null;
        try
        {
            test.SetValue(null, true);
            version.SetValue(null, "android-save-check-" + Guid.NewGuid().ToString("N"));
            id.SetValue(null, (ulong)897654321);
            root = (string)type.GetMethod("GetDataPath", StaticPrivate).Invoke(null, null);
            Check(!Directory.Exists(root), "temporary root exists");
            var manager = SaveManager.Instance;
            string progression = (string)type.GetMethod("GetProgressionPath", InstancePrivate).Invoke(manager, null);
            string stats = (string)type.GetMethod("GetStatsPath", InstancePrivate).Invoke(manager, null);
            Directory.CreateDirectory(Path.GetDirectoryName(progression));
            Directory.CreateDirectory(progression + ".tmp");
            achievementDirty.SetValue(null, true);
            manager.SaveProgression();
            Check((bool)achievementDirty.GetValue(null) && !File.Exists(progression), "failed progression write cleared dirty state or created save");
            Directory.Delete(progression + ".tmp");
            manager.SaveProgression();
            Check(!(bool)achievementDirty.GetValue(null) && File.Exists(progression), "progression retry failed");
            Directory.CreateDirectory(stats + ".tmp");
            statsDirty.SetValue(null, true);
            manager.SaveStats();
            Check((bool)statsDirty.GetValue(null), "failed stats write cleared dirty state");
            manager.SaveProgression(); // Also exercises File.Replace and backup on Android.
            Check((bool)statsDirty.GetValue(null) && File.Exists(progression + ".bak"), "progression save cleared stats or failed backup");
            Directory.Delete(stats + ".tmp");
            manager.SaveStats();
            Check(!(bool)statsDirty.GetValue(null) && File.Exists(stats), "stats retry failed");
            Check(!string.IsNullOrEmpty(SimpleEncryptor.Decrypt(File.ReadAllText(stats))) && !string.IsNullOrEmpty(SimpleEncryptor.Decrypt(File.ReadAllText(progression))), "saved files do not decrypt");
            Debug.Log("Android save failure probe PASS: independent dirty state, retries, backup and decryption");
        }
        catch (Exception e) { Debug.LogError("Android save failure probe FAIL: " + e); }
        finally
        {
            test.SetValue(null, oldTest);
            version.SetValue(null, oldVersion);
            id.SetValue(null, oldId);
            achievementDirty.SetValue(null, oldAchievements);
            statsDirty.SetValue(null, oldStats);
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
#endif
