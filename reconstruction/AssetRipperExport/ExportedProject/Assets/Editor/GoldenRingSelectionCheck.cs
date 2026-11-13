using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts._Data;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class GoldenRingSelectionCheck
{
    public static void Run()
    {
        var root = new GameObject("golden ring test"); root.SetActive(false);
        var save = root.AddComponent<SaveManager>(); save.Init(); save.stats = new StatsSaveFile();
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var ring = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Golden Ring.asset");
        var fallback = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Battery.asset");
        if (!ring || !fallback) throw new Exception("original item assets missing");
        typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(data, new Dictionary<EItem, ItemData> { [EItem.GoldenRing] = ring });
        RunUnlockables.availableItems = new Dictionary<EItemRarity, List<ItemData>> { [EItemRarity.Legendary] = new List<ItemData> { fallback }, [EItemRarity.Common] = new List<ItemData> { fallback } };
        var oldState = UnityEngine.Random.state;
        try
        {
            int lowSeed = -1, highSeed = -1; float lowRoll = 0, highRoll = 0;
            for (int seed = 0; seed < 100000 && (lowSeed < 0 || highSeed < 0); seed++)
            {
                UnityEngine.Random.InitState(seed); float roll = UnityEngine.Random.Range(0f, 1f);
                if (roll < .0025f && lowSeed < 0) { lowSeed = seed; lowRoll = roll; }
                if (roll > .0025f && highSeed < 0) { highSeed = seed; highRoll = roll; }
            }
            if (lowSeed < 0 || highSeed < 0) throw new Exception("unable to find bounded test seeds");
            MyStats.SetValueForce(EMyStat.runs.ToString(), 5); UnityEngine.Random.InitState(lowSeed);
            if (ItemUtility.GetRandomItemFromRarity(EItemRarity.Legendary) != fallback) throw new Exception("ring appeared before six runs");
            MyStats.SetValueForce(EMyStat.runs.ToString(), 6); UnityEngine.Random.InitState(lowSeed);
            if (ItemUtility.GetRandomItemFromRarity(EItemRarity.Legendary) != ring) throw new Exception("ring missing after six runs and low roll");
            UnityEngine.Random.InitState(highSeed);
            if (ItemUtility.GetRandomItemFromRarity(EItemRarity.Legendary) != fallback) throw new Exception("high roll did not use ordinary pool");
            UnityEngine.Random.InitState(lowSeed);
            if (ItemUtility.GetRandomItemFromRarity(EItemRarity.Common) != fallback) throw new Exception("ring appeared at common rarity");
            Debug.Log($"GoldenRingSelectionCheck passed: runs threshold and rarity/roll gate; low={lowRoll:F6}, high={highRoll:F6}");

            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            GameManager.Instance = root.AddComponent<GameManager>();
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            player.inventory = inventory;
            inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
            inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory();
            inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory();
            inventory.playerStats = new PlayerStatsNew(inventory, true);
            RunUnlockables.availableItems = new Dictionary<EItemRarity, List<ItemData>> {
                [EItemRarity.Legendary] = new List<ItemData> { ring, fallback },
                [EItemRarity.Common] = new List<ItemData> { fallback },
                [EItemRarity.Rare] = new List<ItemData> { fallback },
                [EItemRarity.Epic] = new List<ItemData> { fallback }
            };
            UnityEngine.Random.InitState(lowSeed);
            var included = InventoryUtility.GetRandomItemsShadyGuy(EItemRarity.Legendary);
            if (included.Count != 2 || !included.Contains(ring) || !included.Contains(fallback))
                throw new Exception("merchant did not include both legendary items on the low gate roll");
            UnityEngine.Random.InitState(highSeed);
            var excluded = InventoryUtility.GetRandomItemsShadyGuy(EItemRarity.Legendary);
            if (excluded.Count != 1 || excluded[0] != fallback)
                throw new Exception("merchant did not exclude ring on the high gate roll");
            if (RunUnlockables.availableItems[EItemRarity.Legendary].Count != 2)
                throw new Exception("merchant modified the original legendary pool");
            Debug.Log("GoldenRingSelectionCheck passed: merchant gate, uniqueness, rarity floor and original pool preservation");
        }
        finally { UnityEngine.Random.state = oldState; UnityEngine.Object.DestroyImmediate(root); }
    }
}
