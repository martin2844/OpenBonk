using System;
using System.IO;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEngine;

// Uses a unique test-only save root; never opens or changes an ordinary save.
public static class SaveFailureRetryCheck
{
    static BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    static FieldInfo Field(Type type, string name) => type.GetField(name, StaticPrivate);
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static void Run()
    {
        var type = typeof(SaveManager);
        var test = Field(type, "TEST_SAVES");
        var version = Field(type, "TEST_SAVES_VERSION");
        var id = Field(type, "steamIdSave");
        var dirty = Field(typeof(MyAchievements), "hasUnsavedChanges");
        object oldTest = test.GetValue(null), oldVersion = version.GetValue(null), oldId = id.GetValue(null), oldDirty = dirty.GetValue(null);
        bool oldLoaded = SaveManager.loaded;
        var oldEvent = SaveManager.A_ProgressionSaved;
        var oldStatsEvent = SaveManager.A_StatsSaved;
        var statsDirty = Field(typeof(MyStats), "hasUnsavedChanges");
        object oldStatsDirty = statsDirty.GetValue(null);
        bool statsInitialized = false;
        GameObject go = null;
        string root = null;
        try
        {
            test.SetValue(null, true);
            version.SetValue(null, "codex-save-failure-" + Guid.NewGuid().ToString("N"));
            id.SetValue(null, (ulong)897654321);
            root = (string)type.GetMethod("GetDataPath", StaticPrivate).Invoke(null, null);
            Require(!Directory.Exists(root), "temporary root unexpectedly exists");
            go = new GameObject("save-failure-check");
            var manager = go.AddComponent<SaveManager>();
            manager.Init();
            manager.progression = new ProgressionSaveFile();
            manager.stats = new StatsSaveFile();
            SaveManager.loaded = true;
            string path = (string)type.GetMethod("GetProgressionPath", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Directory.CreateDirectory(path + ".tmp"); // A directory prevents opening the temporary file.
            int notifications = 0;
            Action onSaved = (Action)Delegate.CreateDelegate(typeof(Action), typeof(MyAchievements).GetMethod("OnProgressionSaved", StaticPrivate));
            SaveManager.A_ProgressionSaved = () => { notifications++; onSaved(); };
            dirty.SetValue(null, true);
            manager.SaveProgression();
            Require(notifications == 0 && (bool)dirty.GetValue(null), "failed write reported success or cleared dirty state");
            Require(!File.Exists(path), "failed write created a save");
            Directory.Delete(path + ".tmp");
            manager.SaveProgression();
            Require(notifications == 1 && !(bool)dirty.GetValue(null), "successful retry did not notify and clear dirty state");
            Require(File.Exists(path) && new FileInfo(path).Length > 0, "retry did not write progression");
            MyStats.Init();
            statsInitialized = true;
            MyStats.SetValueForce("save-failure-test", 42f);
            string statsPath = (string)type.GetMethod("GetStatsPath", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            Directory.CreateDirectory(statsPath + ".tmp");
            manager.SaveStats();
            Require((bool)statsDirty.GetValue(null), "failed stats write cleared dirty state");
            manager.SaveProgression();
            Require((bool)statsDirty.GetValue(null), "progression save cleared unsaved stats");
            Directory.Delete(statsPath + ".tmp");
            manager.SaveStats();
            Require(!(bool)statsDirty.GetValue(null) && File.Exists(statsPath), "successful stats retry failed to save or clear dirty state");
            Debug.Log("SaveFailureRetryCheck PASS: progression and stats failures retain separate dirty state; successful retries write and notify");
        }
        finally
        {
            SaveManager.loaded = oldLoaded;
            SaveManager.A_ProgressionSaved = oldEvent;
            if (statsInitialized) MyStats.Cleanup();
            SaveManager.A_StatsSaved = oldStatsEvent;
            statsDirty.SetValue(null, oldStatsDirty);
            dirty.SetValue(null, oldDirty);
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
            test.SetValue(null, oldTest);
            version.SetValue(null, oldVersion);
            id.SetValue(null, oldId);
        }
    }
}
