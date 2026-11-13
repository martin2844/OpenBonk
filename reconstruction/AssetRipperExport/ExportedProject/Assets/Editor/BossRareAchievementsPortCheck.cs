using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class BossRareAchievementsPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("BossRareAchievementsPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; float oldTime = MyTime.runTimer;
        var mapProperty = typeof(MapController).GetProperty("currentMap"); var oldMap = MapController.currentMap;
        var finalFlag = typeof(MapController).GetField("isFinalBossStage", BindingFlags.Static | BindingFlags.NonPublic); bool oldFinal = (bool)finalFlag.GetValue(null);
        var shrines = typeof(AchievementTracker).GetField("chargedShrines", BindingFlags.Static | BindingFlags.NonPublic); int oldShrines = (int)shrines.GetValue(null);
        var runStats = typeof(RunStats).GetField("stats", BindingFlags.Static | BindingFlags.NonPublic); object oldRunStats = runStats.GetValue(null);
        var root = new GameObject("rare boss progression fixture"); root.SetActive(false); MapData map = null; ItemInventory items = null;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); items = new ItemInventory(); player.inventory.itemInventory = items; player.inventory.weaponInventory = new WeaponInventory();
            var entries = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            foreach (string name in new[] { "a_heroSword", "a_hatCheese", "a_hatMedieval" }) { var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset"); Require(asset != null && asset.isEnabled, "original asset " + name); entries.Add(name, asset); }
            map = ScriptableObject.CreateInstance<MapData>(); map.eMap = EMap.Forest; mapProperty.GetSetMethod(true).Invoke(null, new object[] { map }); finalFlag.SetValue(null, false); MyTime.runTimer = 500; shrines.SetValue(null, 0); runStats.SetValue(null, new Dictionary<string,float>());
            AchievementTracker.Init();
            RunStats.AddValue(EMyStat.powerupsUsed, 1); InteractableBossSpawner.A_BossDefeated?.Invoke(true); Require(!save.progression.achievements.Contains("a_heroSword"), "powerup blocks sword");
            runStats.SetValue(null, new Dictionary<string,float>()); shrines.SetValue(null, 1); InteractableBossSpawner.A_BossDefeated?.Invoke(true); Require(!save.progression.achievements.Contains("a_heroSword"), "shrine blocks sword");
            shrines.SetValue(null, 0); var cheese = ItemFactory.CreateItem(EItem.MoldyCheese, items); cheese.amount = 7; items.items.Add(EItem.MoldyCheese, cheese); InteractableBossSpawner.A_BossDefeated?.Invoke(true); Require(!save.progression.achievements.Contains("a_heroSword"), "item blocks sword");
            items.items.Clear(); InteractableBossSpawner.A_BossDefeated?.Invoke(true); Require(save.progression.achievements.Contains("a_heroSword"), "no powerups, items or shrines unlocks sword");
            player.character = ECharacter.Amog; items.items.Add(EItem.MoldyCheese, cheese); var snek = ItemFactory.CreateItem(EItem.Snek, items); snek.amount = 1; items.items.Add(EItem.Snek, snek); finalFlag.SetValue(null, true);
            FinalFightController.A_BossDefeated?.Invoke(true); Require(!save.progression.achievements.Contains("a_hatCheese"), "seven cheeses insufficient"); cheese.amount = 8; FinalFightController.A_BossDefeated?.Invoke(true); Require(save.progression.achievements.Contains("a_hatCheese"), "eight cheeses and Snek unlock hat");
            player.character = ECharacter.SirOofie; foreach (EWeapon weapon in new[] { EWeapon.HeroSword, EWeapon.CorruptSword, EWeapon.Scythe }) { var w = (WeaponBase)FormatterServices.GetUninitializedObject(typeof(WeaponBase)); w.level = 1; player.inventory.weaponInventory.weapons.Add(weapon, w); }
            map.eMap = EMap.Desert; FinalFightController.A_BossDefeated?.Invoke(true); Require(!save.progression.achievements.Contains("a_hatMedieval"), "wrong map blocks medieval hat");
            map.eMap = EMap.Forest; player.inventory.weaponInventory.weapons[EWeapon.Scythe].level = 0; FinalFightController.A_BossDefeated?.Invoke(true); Require(!save.progression.achievements.Contains("a_hatMedieval"), "missing Scythe level blocks medieval hat");
            player.inventory.weaponInventory.weapons[EWeapon.Scythe].level = 1; FinalFightController.A_BossDefeated?.Invoke(true); Require(save.progression.achievements.Contains("a_hatMedieval"), "three weapons unlock medieval hat");
            Debug.Log("BossRareAchievementsPortCheck passed: actual assets and sword, cheese, medieval requirements");
        }
        finally { AchievementTracker.Cleanup(); items?.Cleanup(); runStats.SetValue(null, oldRunStats); shrines.SetValue(null, oldShrines); finalFlag.SetValue(null, oldFinal); mapProperty.GetSetMethod(true).Invoke(null, new object[] { oldMap }); if (map != null) UnityEngine.Object.DestroyImmediate(map); MyTime.runTimer = oldTime; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
