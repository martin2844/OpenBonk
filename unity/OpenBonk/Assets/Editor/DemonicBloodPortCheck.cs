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

public static class DemonicBloodPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("DemonicBloodPortCheck: " + message); }
    static void Near(float a, float b, string message) => Require(Mathf.Abs(a - b) < .0001f, message + ": " + a + " expected " + b);
    public static void Run()
    {
        var root = new GameObject("demonic blood test"); root.SetActive(false); var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
        inv.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); inv.tomeInventory = new TomeInventory(); inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects(); inv.statInventory = new StatInventory(); inv.playerXp = new PlayerXp { level = 1 };
        inv.playerStats = new PlayerStatsNew(inv, true); inv.playerHealth = new PlayerHealth(inv.playerStats);
        var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Demonic Blood.asset"); Require(asset != null && asset.eItem == EItem.DemonicBlood, "original item asset");
        ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(asset.eItem, asset);
        var soulAsset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Demonic Soul.asset"); Require(soulAsset != null && soulAsset.eItem == EItem.DemonicSoul, "original soul item asset");
        ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(soulAsset.eItem, soulAsset);
        var target = new GameObject("enemy target"); target.SetActive(false); var enemy = target.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 10f;
        typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 10f); enemy.renderer = target.AddComponent<MeshRenderer>();
        var enemyData = ScriptableObject.CreateInstance<EnemyData>(); enemyData.enemyName = EEnemy.Skeleton; typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { enemyData });
        enemy.rb = target.AddComponent<Rigidbody>(); enemy.enemyMovement = target.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = enemy.rb; enemy.enemyMovement.enemy = enemy;
        enemy.dissolve = target.AddComponent<EnemyDissolve>(); enemy.dissolve.enemyRenderer = enemy.renderer; enemy.dissolve.Reset();
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string desc = asset.GetDummyItem().GetDescription(asset.localizedDescription); Require(desc.Contains("+0.5") && desc.Contains("200") && !desc.Contains("{"), "localized description");
            string soulDesc = soulAsset.GetDummyItem().GetDescription(soulAsset.localizedDescription); Require(soulDesc.Contains("0.1%") && soulDesc.Contains("100%") && !soulDesc.Contains("{"), "localized soul description");
            inv.itemInventory.AddItem(EItem.DemonicBlood, 2); var item = inv.itemInventory.GetItem(EItem.DemonicBlood);
            inv.itemInventory.AddItem(EItem.DemonicSoul, 2); var soul = inv.itemInventory.GetItem(EItem.DemonicSoul);
            Action death = () => Enemy.A_EnemyDied?.Invoke(null, new DamageContainer(0f, "test"));
            MyTime.time = 10f; enemy.Kill("test"); death(); inv.itemInventory.Tick();
            Func<float> bonus = () => item.statModifiers[EStat.MaxHealth].GetModifiers().Single().modification;
            Func<float> soulBonus = () => soul.statModifiers[EStat.DamageMultiplier].GetModifiers().Single().modification;
            Near(bonus(), 1f, "two deaths add one maximum health"); inv.playerStats.Tick(); int firstMax = inv.playerHealth.maxHp; float firstDamage = inv.playerStats.GetStat(EStat.DamageMultiplier);
            Near(soulBonus(), .004f, "two items add two soul stacks per death");
            death(); MyTime.time = 10.5f; inv.itemInventory.Tick(); Near(bonus(), 1f, "one-second update delay"); Near(soulBonus(), .004f, "soul update delay");
            MyTime.time = 11f; inv.itemInventory.Tick(); Near(bonus(), 1.5f, "batched death bonus"); Near(soulBonus(), .006f, "batched soul bonus");
            for (int i = 0; i < 1100; i++) death(); MyTime.time = 12f; inv.itemInventory.Tick(); Near(bonus(), 200f, "two-item cap"); Near(soulBonus(), 2f, "two-item soul cap");
            inv.playerStats.Tick(); Require(inv.playerHealth.maxHp > firstMax, "health maximum follows stat update"); Require(inv.playerStats.GetStat(EStat.DamageMultiplier) > firstDamage, "damage stat follows soul update");
            inv.itemInventory.RemoveItem(EItem.DemonicBlood); inv.itemInventory.RemoveItem(EItem.DemonicSoul); MyTime.time = 13f; inv.itemInventory.Tick(); Near(bonus(), 100f, "clamped bonus refreshes after removal"); Near(soulBonus(), 1f, "clamped soul bonus refreshes after removal");
            inv.itemInventory.RemoveItem(EItem.DemonicBlood); int stacks = (int)item.GetType().GetField("stacks", P).GetValue(item); death(); Require((int)item.GetType().GetField("stacks", P).GetValue(item) == stacks, "cleanup unsubscribes death event");
            inv.itemInventory.RemoveItem(EItem.DemonicSoul); int soulStacks = (int)soul.GetType().GetField("stacks", P).GetValue(soul); death(); Require((int)soul.GetType().GetField("stacks", P).GetValue(soul) == soulStacks, "soul cleanup unsubscribes death event");
            Debug.Log("DemonicBloodPortCheck passed: original asset, death scaling, update timing, cap, health and cleanup");
        }
        finally { inv.itemInventory.Cleanup(); inv.playerHealth.OnDestroy(); inv.playerStats.OnDestroy(); inv.statusEffects.OnDestroy(); UnityEngine.Object.DestroyImmediate(enemyData); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; }
    }
}
