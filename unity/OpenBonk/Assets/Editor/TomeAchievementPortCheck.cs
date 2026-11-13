using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class TomeAchievementPortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception("TomeAchievementPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("tome achievement check"); root.SetActive(false);
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; float oldTimer = MyTime.runTimer;
        int unlocks = 0; Action<string> listener = _ => unlocks++;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var achievements = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            var conditions = new (string name, ETome tome)[] {
                ("a_turboSocks", ETome.Agility), ("a_battery", ETome.Cooldown), ("a_leechingCrystal", ETome.Regeneration),
                ("a_echoShard", ETome.Xp), ("a_shatteredKnowledge", ETome.Attraction), ("a_sniperRifle", ETome.Precision),
                ("a_bloodMagic", ETome.Blood), ("a_blackHole", ETome.Knockback), ("a_dice", ETome.Luck),
                ("a_megachad", ETome.Damage), ("a_athena", ETome.Thorns)
            };
            foreach (string name in new[] { "a_turboSocks", "a_battery", "a_leechingCrystal", "a_echoShard", "a_turboSkates", "a_shatteredKnowledge", "a_sniperRifle", "a_bloodMagic", "a_blackHole", "a_dice", "a_corruptedSword", "a_megachad", "a_athena", "a_cursedGloves" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset");
                Require(asset != null && asset.targetValue > 0, "original achievement " + name); achievements.Add(name, asset);
            }
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var inventory = new TomeInventory(); player.inventory.tomeInventory = inventory;
            MyAchievements.A_TryUnlock += listener; AchievementTracker.Init(); AchievementTracker.Init(); MyTime.runTimer = 0f;
            foreach (var condition in conditions)
            {
                inventory.tomeLevels.Clear(); int target = achievements[condition.name].targetValue;
                inventory.tomeLevels[condition.tome] = target - 1;
                TomeInventory.A_TomeUpgrade?.Invoke(condition.tome, EStat.MaxHealth);
                Require(!MyAchievements.IsAchievementDone(condition.name), condition.name + " below threshold");
                inventory.tomeLevels[condition.tome] = target;
                TomeInventory.A_TomeUpgrade?.Invoke(condition.tome, EStat.MaxHealth);
                Require(MyAchievements.IsAchievementDone(condition.name), condition.name + " threshold");
            }
            inventory.tomeLevels.Clear(); int skateTarget = achievements["a_turboSkates"].targetValue;
            inventory.tomeLevels[ETome.Agility] = skateTarget - 1; inventory.tomeLevels[ETome.Cooldown] = skateTarget;
            TomeInventory.A_TomeUpgrade?.Invoke(ETome.Cooldown, EStat.MaxHealth);
            Require(!MyAchievements.IsAchievementDone("a_turboSkates"), "both tomes required");
            inventory.tomeLevels[ETome.Agility] = skateTarget;
            TomeInventory.A_TomeUpgrade?.Invoke(ETome.Agility, EStat.MaxHealth);
            Require(MyAchievements.IsAchievementDone("a_turboSkates"), "both tome thresholds");
            inventory.tomeLevels.Clear(); inventory.tomeLevels[ETome.Cursed] = achievements["a_corruptedSword"].targetValue;
            MyTime.runTimer = 600.01f; TomeInventory.A_TomeUpgrade?.Invoke(ETome.Cursed, EStat.MaxHealth);
            Require(!MyAchievements.IsAchievementDone("a_corruptedSword") && MyAchievements.IsAchievementDone("a_cursedGloves"), "late cursed tome only unlocks gloves");
            MyTime.runTimer = 600f; TomeInventory.A_TomeUpgrade?.Invoke(ETome.Cursed, EStat.MaxHealth);
            Require(MyAchievements.IsAchievementDone("a_corruptedSword") && unlocks == 14, "ten-minute boundary unlocks sword once");
            Require(!MyAchievements.CheckAchievementValue("missing", 100), "missing target excluded");
            AchievementTracker.Cleanup(); save.progression.achievements.Clear();
            inventory.tomeLevels[ETome.Cursed] = 100; TomeInventory.A_TomeUpgrade?.Invoke(ETome.Cursed, EStat.MaxHealth);
            Require(unlocks == 14 && !MyAchievements.IsAchievementDone("a_corruptedSword"), "cleanup removes handler");
            Debug.Log("TomeAchievementPortCheck passed: fourteen original conditions, timing boundary and cleanup");
        }
        finally
        {
            AchievementTracker.Cleanup(); MyAchievements.A_TryUnlock -= listener;
            MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; MyTime.runTimer = oldTimer;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave });
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
