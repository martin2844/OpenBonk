using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class BossProgressAchievementsPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("BossProgressAchievementsPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; float oldTime = MyTime.runTimer; int oldIndex = MapController.index;
        var mapProperty = typeof(MapController).GetProperty("currentMap"); var oldMap = MapController.currentMap;
        var damaged = typeof(AchievementTracker).GetField("hasTakenDamageThisRun", BindingFlags.Static | BindingFlags.NonPublic); bool oldDamaged = (bool)damaged.GetValue(null);
        var root = new GameObject("boss progression fixture"); root.SetActive(false);
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.character = ECharacter.Fox;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); player.inventory.playerHealth.maxHp = 100; player.inventory.playerHealth.hp = 100;
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
            var entries = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            foreach (string name in new[] { "a_cursedTome", "a_clank", "a_robinette", "a_chadwell", "a_ninja", "a_vlad", "a_spaceNoodle", "a_holyBook", "a_speedBoi", "a_gamerGoggles" })
            { var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset"); Require(asset != null && asset.isEnabled, "original enabled asset " + name); entries.Add(name, asset); }
            var map = ScriptableObject.CreateInstance<MapData>(); map.eMap = EMap.Forest; mapProperty.GetSetMethod(true).Invoke(null, new object[] { map }); AchievementTracker.Init();
            MyTime.runTimer = 300.01f; damaged.SetValue(null, true); game.bossCurses = 1; MapController.index = 0; InteractableBossSpawner.A_BossDefeated?.Invoke(true);
            Require(save.progression.achievements.Contains("a_clank") && !save.progression.achievements.Contains("a_cursedTome") && !save.progression.achievements.Contains("a_holyBook") && !save.progression.achievements.Contains("a_speedBoi") && !save.progression.achievements.Contains("a_gamerGoggles"), "Forest 0 and negative thresholds");
            MyTime.runTimer = 300f; damaged.SetValue(null, false); game.bossCurses = 2; player.inventory.playerHealth.hp = 10; MapController.index = 1; InteractableBossSpawner.A_BossDefeated?.Invoke(true);
            float ratio = player.inventory.playerHealth.GetHpRatio(), threshold = entries["a_gamerGoggles"].targetValue / 100f;
            Debug.Log("Boss progress diagnostic hpbits=" + BitConverter.SingleToInt32Bits(ratio) + " thresholdbits=" + BitConverter.SingleToInt32Bits(threshold) + " compare=" + (ratio <= threshold) + " unlocked=" + string.Join(",", save.progression.achievements));
            foreach (string name in new[] { "a_cursedTome", "a_robinette", "a_holyBook", "a_speedBoi", "a_gamerGoggles" }) Require(save.progression.achievements.Contains(name), "inclusive condition " + name);
            MapController.index = 2; InteractableBossSpawner.A_BossDefeated?.Invoke(true); Require(save.progression.achievements.Contains("a_chadwell"), "Forest 2");
            map.eMap = EMap.Desert; MapController.index = 0; InteractableBossSpawner.A_BossDefeated?.Invoke(true); Require(save.progression.achievements.Contains("a_ninja") && !save.progression.achievements.Contains("a_vlad"), "Desert 0");
            MapController.index = 1; InteractableBossSpawner.A_BossDefeated?.Invoke(true); Require(save.progression.achievements.Contains("a_vlad") && save.progression.achievements.Contains("a_spaceNoodle"), "Desert 1");
            UnityEngine.Object.DestroyImmediate(map); Debug.Log("BossProgressAchievementsPortCheck passed: ten original achievements, stage and threshold boundaries");
        }
        finally { AchievementTracker.Cleanup(); damaged.SetValue(null, oldDamaged); mapProperty.GetSetMethod(true).Invoke(null, new object[] { oldMap }); MapController.index = oldIndex; MyTime.runTimer = oldTime; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; GameManager.Instance = oldGame; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
