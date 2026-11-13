using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class GogglesKnucklesPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("GogglesKnucklesPortCheck: " + message); }
    static void Near(float actual, float expected, string message) => Require(Mathf.Abs(actual - expected) < .0001f, message + ": " + actual + " expected " + expected);
    public static void Run()
    {
        var root = new GameObject("goggles and knuckles test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>(); var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 1 };
        inventory.playerStats = new PlayerStatsNew(inventory, true); inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); inventory.playerHealth.hp = inventory.playerHealth.maxHp = 100;
        var itemData = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
        var goggles = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Gamer Goggles.asset"); var knuckles = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/BrassKnuckles.asset");
        Require(goggles != null && knuckles != null, "original assets"); itemData.Add(goggles.eItem, goggles); itemData.Add(knuckles.eItem, knuckles);
        var enemy = root.AddComponent<Enemy>(); enemy.enemyMovement = root.AddComponent<EnemyMovementRb>(); var dc = new DamageContainer(1f, "test") { enemy = enemy }; var components = new StatComponents();
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string gogglesText = goggles.GetDummyItem().GetDescription(goggles.localizedDescription), knucklesText = knuckles.GetDummyItem().GetDescription(knuckles.localizedDescription);
            Require(gogglesText != null && !gogglesText.Contains("{") && knucklesText.Contains("+25%") && !knucklesText.Contains("{"), "original descriptions");
            inventory.itemInventory.AddItem(EItem.GamerGoggles, 2); inventory.itemInventory.AddItem(EItem.BrassKnuckles, 2);
            var gogglesItem = inventory.itemInventory.GetItem(EItem.GamerGoggles); MyTime.time = 10f; inventory.itemInventory.Tick();
            Near(gogglesItem.statModifiers[EStat.DamageMultiplier].GetModifiers().Single().modification, 0f, "full health");
            inventory.playerHealth.hp = 25; MyTime.time = 10.5f; inventory.itemInventory.Tick(); Near(gogglesItem.statModifiers[EStat.DamageMultiplier].GetModifiers().Single().modification, 0f, "update cooldown");
            MyTime.time = 11f; inventory.itemInventory.Tick(); inventory.playerStats.Tick(); Near(gogglesItem.statModifiers[EStat.DamageMultiplier].GetModifiers().Single().modification, 1f, "two stacks at 25% health");
            inventory.playerHealth.hp = 50; MyTime.time = 12f; inventory.itemInventory.Tick(); Near(gogglesItem.statModifiers[EStat.DamageMultiplier].GetModifiers().Single().modification, 0f, "50% health threshold");
            enemy.enemyMovement.distanceToTarget = 12f; inventory.itemInventory.PreAttack(dc, components); Near(components.baseValue, .5f, "two stacks apply at radius boundary");
            enemy.enemyMovement.distanceToTarget = 12.01f; inventory.itemInventory.PreAttack(dc, components); Near(components.baseValue, 0f, "outside stacked radius");
            Debug.Log("GogglesKnucklesPortCheck passed: original descriptions, low-health bonus and distance-gated flat damage");
        }
        finally { inventory.itemInventory.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; }
    }
}
