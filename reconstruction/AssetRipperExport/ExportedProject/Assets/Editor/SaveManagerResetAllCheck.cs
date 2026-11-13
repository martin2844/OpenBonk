using System;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEngine;

public static class SaveManagerResetAllCheck
{
    static int Subscribers(object target) => GameManager.A_GameOver == null ? 0 : GameManager.A_GameOver.GetInvocationList().Count(x => ReferenceEquals(x.Target, target));
    static void Require(bool condition, string message) { if (!condition) throw new Exception("SaveManagerResetAllCheck: " + message); }

    public static void Run()
    {
        // Keep all data in memory: the fixture never calls any save method.
        bool oldLoaded = SaveManager.loaded; SaveManager.loaded = false;
        var previousEvent = GameManager.A_GameOver;
        var root = new GameObject("disposable reset-all test"); root.SetActive(false);
        var save = root.AddComponent<SaveManager>(); save.Init();
        var oldConfig = new ConfigSaveFile(); oldConfig.Init(); oldConfig.cfVideoSettings.fov = 120f;
        var oldProgress = new ProgressionSaveFile(); oldProgress.Init(); oldProgress.silver = 123; oldProgress.achievements.Add("test-unlock");
        var oldStats = new StatsSaveFile(); oldStats.Init();
        save.config = oldConfig; save.progression = oldProgress; save.stats = oldStats;
        var initialized = typeof(SaveManager).GetField("initializedProgression", BindingFlags.Instance | BindingFlags.NonPublic);
        initialized.SetValue(save, oldProgress);
        try
        {
            Require(Subscribers(oldProgress) == 1, "old progression subscription");
            save.ResetAll();
            Require(save.config != oldConfig && save.config.cfVideoSettings.fov == new ConfigSaveFile().cfVideoSettings.fov, "new initialized config");
            Require(save.progression != oldProgress && save.progression.silver == 0 && save.progression.achievements.Count == 0 && save.progression.shopItems.Count > 0, "new initialized progression");
            Require(save.stats != oldStats && save.stats.stats.Count > 0 && save.stats.times.Count > 0, "new initialized stats");
            Require(Subscribers(oldProgress) == 0 && Subscribers(save.progression) == 1, "old listener removed and new listener installed once");
            var firstProgress = save.progression;
            save.ResetAll();
            Require(Subscribers(firstProgress) == 0 && Subscribers(save.progression) == 1, "repeated reset does not retain prior listeners");
            Require(oldConfig.cfVideoSettings.fov == 120f && oldProgress.silver == 123 && oldProgress.achievements.Contains("test-unlock"), "previous objects remain intact");
            Debug.Log("SaveManagerResetAllCheck PASS: in-memory config, progression and stats replaced and initialized; listeners cleaned on repeated reset");
        }
        finally
        {
            save.progression?.OnDestroy();
            GameManager.A_GameOver = previousEvent;
            UnityEngine.Object.DestroyImmediate(root);
            SaveManager.loaded = oldLoaded;
        }
    }
}
