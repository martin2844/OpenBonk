using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class BloodmarkItemsPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool value, string message) { if (!value) throw new Exception("BloodmarkItemsPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("bloodmark items test"); root.SetActive(false); var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        var assets = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data);
        var transfusion = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/UnstableTransfusion.asset"); var cleaver = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Bloody Cleaver.asset");
        Require(transfusion != null && cleaver != null, "original item assets"); assets.Add(transfusion.eItem, transfusion); assets.Add(cleaver.eItem, cleaver);
        var enemyObject = new GameObject("bloodmark target"); enemyObject.SetActive(false); var enemy = enemyObject.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = enemyObject.AddComponent<Rigidbody>(); enemy.enemyMovement = enemyObject.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = enemy.rb; enemy.enemyMovement.enemy = enemy; enemy.renderer = enemyObject.AddComponent<MeshRenderer>();
        var random = global::Utility.MyRandom.random; ItemInventory items = null; PlayerHealth health = null;
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string t = transfusion.GetDummyItem().GetDescription(transfusion.localizedDescription), c = cleaver.GetDummyItem().GetDescription(cleaver.localizedDescription);
            Require(t.Contains("35%") && !t.Contains("{") && c.Contains("50%") && !c.Contains("{"), "original descriptions");
            global::Utility.MyRandom.random = new System.Random(1234); items = new ItemInventory(); inventory.itemInventory = items; items.AddItem(EItem.UnstableTransfusion, 20);
            enemy.DamageFromPlayerOther(new DamageContainer(1f, "test") { enemy = enemy, damage = 1f });
            Require(enemy.debuffsToAdd.TryGetValue(EDebuff.Bloodmark, out var pending) && pending.stacks == 7 && pending.duration == 5f, "real hit triggers transfusion stacks");
            enemy.debuffsToAdd.Clear(); items.Cleanup(); items = new ItemInventory(); inventory.itemInventory = items; items.AddItem(EItem.BloodyCleaver, 2);
            enemy.DamageFromPlayerOther(new DamageContainer(1f, "test") { enemy = enemy, damage = 1f });
            Require(enemy.debuffsToAdd.TryGetValue(EDebuff.Bloodmark, out pending) && pending.stacks == 1 && pending.duration == 5f, "real hit triggers cleaver");
            enemy.debuffsToAdd.Clear(); PlayerHealth.A_LifestealProc?.Invoke(enemy, 2); PlayerHealth.A_LifestealProc?.Invoke(enemy, 3);
            Require(!enemy.debuffsToAdd.ContainsKey(EDebuff.Bloodmark), "lifesteal waits for item tick"); items.Tick();
            Require(enemy.debuffsToAdd.TryGetValue(EDebuff.Bloodmark, out pending) && pending.stacks == 10 && pending.duration == 4f, "lifesteal stacks aggregate and scale");
            enemy.debuffsToAdd.Clear(); items.Tick(); Require(!enemy.debuffsToAdd.ContainsKey(EDebuff.Bloodmark), "tracker clears after processing");
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            stats.stats = new Dictionary<EStat, float> { [EStat.MaxHealth] = 100f, [EStat.Shield] = 0f, [EStat.HealthRegen] = 0f, [EStat.Lifesteal] = 2f };
            inventory.playerStats = stats; health = new PlayerHealth(stats); inventory.playerHealth = health; health.hp = 50;
            enemy.DamageFromPlayerOther(new DamageContainer(0f, "test") { enemy = enemy, damage = 1f }); items.Tick();
            Require(enemy.debuffsToAdd.TryGetValue(EDebuff.Bloodmark, out pending) && pending.stacks == 4 && pending.duration == 4f, "real damage triggers health lifesteal and deferred cleaver stacks");
            enemy.debuffsToAdd.Clear(); health.OnDestroy(); health = null;
            items.Cleanup(); PlayerHealth.A_LifestealProc?.Invoke(enemy, 2); items.Tick(); Require(!enemy.debuffsToAdd.ContainsKey(EDebuff.Bloodmark), "cleanup unsubscribes lifesteal");
            Debug.Log("BloodmarkItemsPortCheck passed: original descriptions, real hit procs and deferred lifesteal stacks");
        }
        finally { health?.OnDestroy(); items?.Cleanup(); global::Utility.MyRandom.random = random; UnityEngine.Object.DestroyImmediate(enemy.enemyData); UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; }
    }
}
