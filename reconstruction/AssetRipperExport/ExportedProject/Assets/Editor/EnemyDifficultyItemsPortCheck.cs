using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class EnemyDifficultyItemsPortCheck
{
    static void Near(float actual, float expected, string message) { if (Mathf.Abs(actual - expected) > .0001f) throw new Exception(message + ": " + actual + " expected " + expected); }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var root = new GameObject("enemy difficulty items test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 1 };
        inventory.playerStats = new PlayerStatsNew(inventory, true);
        var oldMask = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/OldMask.asset"); var skuleg = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Skuleg.asset");
        Require(oldMask != null && oldMask.eItem == EItem.OldMask && skuleg != null && skuleg.eItem == EItem.Skuleg, "original item assets");
        var allItems = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data); allItems.Add(oldMask.eItem, oldMask); allItems.Add(skuleg.eItem, skuleg);
        var enemy = ScriptableObject.CreateInstance<EnemyData>(); enemy.canBeElite = true; enemy.creditCost = 10f;
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string maskDesc = oldMask.GetDummyItem().GetDescription(oldMask.localizedDescription); string skullDesc = skuleg.GetDummyItem().GetDescription(skuleg.localizedDescription);
            Require(maskDesc.Contains("+15%") && !maskDesc.Contains("{"), "old mask description: " + maskDesc); Require(skullDesc.Contains("+7%") && !skullDesc.Contains("{"), "skuleg description: " + skullDesc);
            float baseDifficulty = inventory.playerStats.GetStat(EStat.Difficulty); float baseElite = inventory.playerStats.GetStat(EStat.EliteSpawnIncrease);
            var card = new EnemyCard(enemy, true); float baseWeight = card.weight; float baseChance = EnemyStats.GetEliteChance(enemy);
            inventory.itemInventory.AddItem(EItem.OldMask, 2); inventory.itemInventory.AddItem(EItem.Skuleg, 3); inventory.playerStats.Tick(); card.RefreshWeightAndCost();
            Near(inventory.playerStats.GetStat(EStat.EliteSpawnIncrease), baseElite + .3f, "two masks propagate"); Near(inventory.playerStats.GetStat(EStat.Difficulty), baseDifficulty + .21f, "three skulegs propagate");
            Near(card.weight, baseWeight + .3f * .04f, "elite card weight"); Near(EnemyStats.GetEliteChance(enemy), baseChance + .3f * .006f, "elite chance");
            inventory.itemInventory.RemoveItem(EItem.OldMask); inventory.itemInventory.RemoveItem(EItem.Skuleg); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetStat(EStat.EliteSpawnIncrease), baseElite + .15f, "mask removal"); Near(inventory.playerStats.GetStat(EStat.Difficulty), baseDifficulty + .14f, "skuleg removal");
            inventory.itemInventory.RemoveItem(EItem.OldMask); inventory.itemInventory.RemoveItem(EItem.Skuleg); inventory.itemInventory.RemoveItem(EItem.Skuleg); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetStat(EStat.EliteSpawnIncrease), baseElite, "mask fully removed"); Near(inventory.playerStats.GetStat(EStat.Difficulty), baseDifficulty, "skuleg fully removed");
            Debug.Log("EnemyDifficultyItemsPortCheck passed: original items, stacking, player stats, elite weight, chance and removal");
        }
        finally { inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(enemy); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; }
    }
}
