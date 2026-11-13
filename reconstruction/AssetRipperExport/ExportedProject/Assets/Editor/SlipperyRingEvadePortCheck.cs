using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;

public static class SlipperyRingEvadePortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    public static void Run()
    {
        var root = new GameObject("slippery ring evade test");
        root.SetActive(false);
        var player = root.AddComponent<MyPlayer>();
        MyPlayer.Instance = player;
        GameManager.Instance = root.AddComponent<GameManager>();
        var data = root.AddComponent<DataManager>();
        DataManager.Instance = data;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory();
        inventory.itemInventory = new ItemInventory();
        inventory.statusEffects = new PlayerStatusEffects();
        inventory.statInventory = new StatInventory();
        inventory.playerXp = new PlayerXp { level = 1 };
        inventory.playerStats = new PlayerStatsNew(inventory, true);
        inventory.playerHealth = new PlayerHealth(inventory.playerStats);
        var oldRandom = Utility.MyRandom.random;
        int evades = 0;
        Action<Enemy> onEvade = _ => evades++;
        PlayerHealth.A_Evaded += onEvade;
        try
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Slippery Ring.asset");
            Require(item != null && item.eItem == EItem.SlipperyRing, "original item data");
            var all = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            all.Add(EItem.SlipperyRing, item);
            float baseline = inventory.playerStats.GetStat(EStat.Evasion);
            inventory.itemInventory.AddItem(EItem.SlipperyRing, 2);
            inventory.playerStats.Tick();
            float chance = inventory.playerStats.GetStat(EStat.Evasion);
            Require(chance > baseline && chance < 1f, "two rings raise final evasion");
            var expected = new System.Random(8173);
            Utility.MyRandom.random = new System.Random(8173);
            int expectedEvades = 0;
            for (int i = 0; i < 200; i++)
            {
                bool shouldEvade = expected.NextDouble() <= chance;
                var hit = new DamageContainer(0f, "test") { damage = 10f };
                Require(inventory.playerHealth.CheckStopDamage(hit, false) == shouldEvade, "seeded hit " + i);
                if (shouldEvade) expectedEvades++;
            }
            Require(expectedEvades > 0 && expectedEvades < 200 && evades == expectedEvades, "evade events match avoided hits");
            var bypass = new DamageContainer(0f, "test") { damage = 10f, flags = DcFlags.BypassEvade };
            Require(!inventory.playerHealth.CheckStopDamage(bypass, false) && evades == expectedEvades, "bypass flag skips evasion");
            inventory.itemInventory.RemoveItem(EItem.SlipperyRing);
            inventory.itemInventory.RemoveItem(EItem.SlipperyRing);
            inventory.playerStats.Tick();
            Require(Mathf.Abs(inventory.playerStats.GetStat(EStat.Evasion) - baseline) < .0001f, "removal restores evasion");
            Debug.Log("SlipperyRingEvadePortCheck passed: inventory, seeded damage gating, event, bypass and removal");
        }
        finally
        {
            PlayerHealth.A_Evaded -= onEvade;
            Utility.MyRandom.random = oldRandom;
            inventory.playerHealth.OnDestroy();
            inventory.playerStats.OnDestroy();
            inventory.statusEffects.OnDestroy();
            inventory.itemInventory.Cleanup();
            UnityEngine.Object.DestroyImmediate(root);
            MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null;
        }
    }
}
