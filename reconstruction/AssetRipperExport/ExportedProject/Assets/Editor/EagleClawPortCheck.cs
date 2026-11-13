using System;
using System.Collections.Generic;
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

public static class EagleClawPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("EagleClawPortCheck: " + message); }
    static void Near(float actual, float expected, string message) => Require(Mathf.Abs(actual - expected) < .0001f, message + ": " + actual + " expected " + expected);
    public static void Run()
    {
        var root = new GameObject("eagle claw test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>(); var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 1 }; inventory.playerStats = new PlayerStatsNew(inventory, true);
        var itemData = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
        var claw = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Eagle Claw.asset"); Require(claw != null, "original item asset"); itemData.Add(claw.eItem, claw);
        var enemy = root.AddComponent<Enemy>(); var movement = root.AddComponent<EnemyMovementRb>(); enemy.enemyMovement = movement; movement.enemy = enemy; movement.rb = root.AddComponent<Rigidbody>();
        typeof(Enemy).GetProperty("enemyData").SetValue(enemy, AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Slime.asset"));
        var dc = new DamageContainer(1f, "test") { enemy = enemy, direction = Vector3.right, procCoefficient = 100f }; var components = new StatComponents();
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string description = claw.GetDummyItem().GetDescription(claw.localizedDescription); Require(description.Contains("66%") && description.Contains("8%") && !description.Contains("{"), "original description: " + description);
            inventory.itemInventory.AddItem(EItem.EagleClaw, 2);
            inventory.itemInventory.PreAttack(dc, components); Near(components.additiveValue, 0f, "grounded enemy has no damage bonus");
            typeof(EnemyMovementRb).GetProperty("grounded").SetValue(movement, false); inventory.itemInventory.PreAttack(dc, components); Near(components.additiveValue, 1.32f, "two stacks versus airborne enemy");
            inventory.itemInventory.PostDamage(dc); Near(movement.rb.velocity.y, 0f, "airborne enemy not knocked up");
            typeof(EnemyMovementRb).GetProperty("grounded").SetValue(movement, true); inventory.itemInventory.PostDamage(dc); Require(movement.rb.velocity.y > 0f, "grounded proc knocks enemy upward");
            var knockback = (Vector3)typeof(EnemyMovementRb).GetField("knockbackVelocity", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(movement); Require(knockback.x > 0f, "grounded proc applies directional knockback");
            Debug.Log("EagleClawPortCheck passed: original description, stacked airborne damage and grounded knock-up through inventory");
        }
        finally { inventory.itemInventory.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; }
    }
}
