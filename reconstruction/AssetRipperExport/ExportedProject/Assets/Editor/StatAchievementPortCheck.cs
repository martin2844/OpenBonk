using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class StatAchievementPortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception("StatAchievementPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("stat achievement check"); root.SetActive(false);
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance;
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var baseField = typeof(AchievementTracker).GetField("baseMovementSpeed", flags); var timerField = typeof(AchievementTracker).GetField("noDamageTimer", flags);
        float oldBase = (float)baseField.GetValue(null), oldTimer = (float)timerField.GetValue(null);
        int unlocks = 0; Action<string> listener = _ => unlocks++;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var achievements = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            foreach (string name in new[] { "a_chonkplate", "a_skuleg" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset");
                Require(asset != null, "original achievement " + name); achievements.Add(name, asset);
            }
            GameManager.Instance = root.AddComponent<GameManager>(); var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            stats.stats = new Dictionary<EStat, float> { [EStat.MoveSpeedMultiplier] = 1.35f, [EStat.Difficulty] = .39f };
            player.inventory.playerStats = stats; player.inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
            MyAchievements.A_TryUnlock += listener; AchievementTracker.Init(); AchievementTracker.Init();
            timerField.SetValue(null, 99f); MyPlayer.A_PlayerInventoryInitialized?.Invoke(player.inventory);
            Require((float)baseField.GetValue(null) == 1.35f && (float)timerField.GetValue(null) == 0f, "inventory initializes baseline and timer");
            player.inventory.playerHealth.maxHp = 499; PlayerStatsNew.A_StatUpdate?.Invoke(EStat.MaxHealth);
            PlayerStatsNew.A_StatUpdate?.Invoke(EStat.Difficulty); Require(unlocks == 0, "below original thresholds");
            player.inventory.playerHealth.maxHp = 500; PlayerStatsNew.A_StatUpdate?.Invoke(EStat.Difficulty);
            Require(!MyAchievements.IsAchievementDone("a_chonkplate"), "only matching stat triggers health check");
            PlayerStatsNew.A_StatUpdate?.Invoke(EStat.MaxHealth);
            stats.stats[EStat.Difficulty] = .4f; PlayerStatsNew.A_StatUpdate?.Invoke(EStat.Difficulty);
            PlayerStatsNew.A_StatUpdate?.Invoke(EStat.Difficulty);
            Require(MyAchievements.IsAchievementDone("a_chonkplate") && MyAchievements.IsAchievementDone("a_skuleg") && unlocks == 2, "both boundaries and duplicate suppression");
            AchievementTracker.Cleanup(); save.progression.achievements.Clear(); PlayerStatsNew.A_StatUpdate?.Invoke(EStat.MaxHealth);
            Require(unlocks == 2 && save.progression.achievements.Count == 0, "cleanup removes handler");
            Debug.Log("StatAchievementPortCheck passed: baseline, health and difficulty boundaries, cleanup");
        }
        finally
        {
            AchievementTracker.Cleanup(); MyAchievements.A_TryUnlock -= listener; baseField.SetValue(null, oldBase); timerField.SetValue(null, oldTimer);
            GameManager.Instance = oldGame; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave });
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
