using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class ShadyStatsAchievementsPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("ShadyStatsAchievementsPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; bool oldLoaded = SaveManager.loaded; var flags = BindingFlags.NonPublic | BindingFlags.Static;
        var started = typeof(MyAchievements).GetField("startedTracking", flags); var trackers = typeof(MyAchievements).GetField("statTrackers", flags); var queue = typeof(MyAchievements).GetField("queuedStatNames", flags); var next = typeof(MyAchievements).GetField("nextStatTrackersCheck", flags);
        object oldStarted = started.GetValue(null), oldTrackers = trackers.GetValue(null), oldQueue = queue.GetValue(null), oldNext = next.GetValue(null);
        var root = new GameObject("shady stats fixture"); root.SetActive(false);
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data); data.unsortedAchievements = new List<MyAchievement>();
            string[] names = { "a_hatShadyBlack", "a_hatShadyBlue", "a_hatShadyPink", "a_hatShadyGold" }; int[] targets = { 100, 80, 60, 50 };
            for (int i = 0; i < names.Length; i++) { var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + names[i] + ".asset"); Require(asset != null && asset.isEnabled && asset.targetValue == targets[i], "original asset " + names[i]); dict.Add(names[i], asset); data.unsortedAchievements.Add(asset); save.stats.stats.Add(asset.statName, new MyStat(asset.statName, targets[i] - 1)); }
            started.SetValue(null, false); trackers.SetValue(null, new Dictionary<string, List<MyAchievement>>()); queue.SetValue(null, new HashSet<MyStat>()); next.SetValue(null, -1f); SaveManager.loaded = true; MyAchievements.Init(); TrackStats.Init(); TrackStats.Init();
            var shady = root.AddComponent<InteractableShadyGuy>();
            for (int i = 0; i < names.Length; i++) { shady.rarity = (EItemRarity)i; InteractableShadyGuy.A_ShadyGuyDone?.Invoke(shady); next.SetValue(null, -1f); MyAchievements.Update(); Require(save.progression.achievements.Contains(names[i]) && MyStats.GetStat(data.unsortedAchievements[i].statName) == targets[i], "correct count and unlock " + names[i]); }
            shady.rarity = EItemRarity.Corrupted; InteractableShadyGuy.A_ShadyGuyDone?.Invoke(shady); for (int i = 0; i < names.Length; i++) Require(MyStats.GetStat(data.unsortedAchievements[i].statName) == targets[i], "unsupported rarity does not increment");
            TrackStats.Cleanup(); shady.rarity = EItemRarity.Rare; InteractableShadyGuy.A_ShadyGuyDone?.Invoke(shady); Require(MyStats.GetStat(data.unsortedAchievements[1].statName) == targets[1], "cleanup removes handler");
            Debug.Log("ShadyStatsAchievementsPortCheck passed: four real hats, original rarity counts and cleanup");
        }
        finally { TrackStats.Cleanup(); MyAchievements.Cleanup(); started.SetValue(null, oldStarted); trackers.SetValue(null, oldTrackers); queue.SetValue(null, oldQueue); next.SetValue(null, oldNext); SaveManager.loaded = oldLoaded; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
