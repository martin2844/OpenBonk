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
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class DemonBladePortCheck
{
    sealed class FixedRandom : System.Random { readonly double value; public FixedRandom(double value) { this.value = value; } public override double NextDouble() => value; }
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool value, string message) { if (!value) throw new Exception("DemonBladePortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("Demon Blade test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; inv.playerStats = stats;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = 50; health.maxHp = 100; inv.playerHealth = health;
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Demon Blade.asset"); Require(asset != null && asset.eItem == EItem.DemonBlade, "original item asset");
        ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(asset.eItem, asset);
        var enemyObject = new GameObject("target"); enemyObject.SetActive(false); var enemy = enemyObject.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000;
        typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 1000f); enemy.renderer = enemyObject.AddComponent<MeshRenderer>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = enemyObject.AddComponent<Rigidbody>(); enemy.enemyMovement = enemyObject.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = enemy.rb; enemy.enemyMovement.enemy = enemy;
        var oldRandom = global::Utility.MyRandom.random;
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string text = asset.GetDummyItem().GetDescription(asset.localizedDescription); Require(text.Contains("+25%") && !text.Contains("{"), "original description");
            inv.itemInventory.AddItem(EItem.DemonBlade, 2); var item = inv.itemInventory.GetItem(EItem.DemonBlade);
            Require(Mathf.Abs(item.statModifiers[EStat.CritChance].GetModifiers().Single().modification - .01f) < .0001f, "critical chance is fixed at one percent");
            var hit = new DamageContainer(1f, "test") { enemy = enemy, damage = 1f, crit = false };
            global::Utility.MyRandom.random = new FixedRandom(0.1); enemy.DamageExternal(hit); Require(health.hp == 50, "noncritical hit does not heal");
            hit.crit = true; global::Utility.MyRandom.random = new FixedRandom(.8); enemy.DamageExternal(hit); Require(health.hp == 50, "failed fractional chance does not heal");
            global::Utility.MyRandom.random = new FixedRandom(.1); enemy.DamageExternal(hit); Require(health.hp == 51, "critical hit heals on successful fractional chance");
            inv.itemInventory.AddItem(EItem.DemonBlade, 2); hit.procCoefficient = 2.5f;
            global::Utility.MyRandom.random = new FixedRandom(.8); enemy.DamageExternal(hit); Require(health.hp == 53, "whole heals from scaled proc coefficient");
            global::Utility.MyRandom.random = new FixedRandom(.1); enemy.DamageExternal(hit); Require(health.hp == 56, "fractional heal adds to whole heals");
            health.hp = 100; enemy.DamageExternal(hit); Require(health.hp == 100, "full health suppresses healing");
            inv.itemInventory.RemoveItem(EItem.DemonBlade); inv.itemInventory.RemoveItem(EItem.DemonBlade); inv.itemInventory.RemoveItem(EItem.DemonBlade); inv.itemInventory.RemoveItem(EItem.DemonBlade);
            health.hp = 50; enemy.DamageExternal(hit); Require(health.hp == 50, "cleanup unsubscribes from damage");
            Debug.Log("DemonBladePortCheck passed: crit bonus, real enemy damage events, chance, proc scaling and cleanup");
        }
        finally { inv.itemInventory.Cleanup(); global::Utility.MyRandom.random = oldRandom; UnityEngine.Object.DestroyImmediate(enemy.enemyData); UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; }
    }
}
