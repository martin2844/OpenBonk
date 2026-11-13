using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEditor;
using UnityEngine;

public static class MoaiLegendaryRandomPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("MoaiLegendaryRandomPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("Moai random check"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        GameManager.Instance = root.AddComponent<GameManager>();
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
        stats.stats = new Dictionary<EStat, float> { [EStat.Luck] = 0f }; player.inventory.playerStats = stats;
        var originalPools = RunUnlockables.availableItems;
        var originalRandom = UnityEngine.Random.state;
        var originalGenerator = global::Utility.MyRandom.random;
        try
        {
            RunUnlockables.availableItems = new Dictionary<EItemRarity, List<ItemData>>();
            foreach (EItemRarity rarity in Enum.GetValues(typeof(EItemRarity))) RunUnlockables.availableItems[rarity] = new List<ItemData>();
            foreach (string name in new[] { "Gloves Power", "Speed Boi", "Ice Cube" })
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/" + name + ".asset");
                Require(item != null && item.rarity == EItemRarity.Legendary, "original legendary " + name);
                RunUnlockables.availableItems[EItemRarity.Legendary].Add(item);
            }
            UnityEngine.Random.InitState(94871);
            for (int i = 0; i < 3; i++) Rarity.GetItemRarity(0f);
            float expectedNext = UnityEngine.Random.value;
            UnityEngine.Random.InitState(94871);
            global::Utility.MyRandom.random = new System.Random(41);
            var offers = InventoryUtility.GetRandomItemsMoai(3);
            float actualNext = UnityEngine.Random.value;
            Require(offers.Count == 3 && new HashSet<ItemData>(offers).Count == 3, "three distinct legendary items");
            Require(actualNext == expectedNext, "one rarity roll per legendary offer");
            Require(RunUnlockables.availableItems[EItemRarity.Legendary].Count == 3, "source pool remains unchanged");
            Debug.Log("MoaiLegendaryRandomPortCheck passed: original items, unique legendary choices and three rarity rolls");
        }
        finally { global::Utility.MyRandom.random = originalGenerator; UnityEngine.Random.state = originalRandom; RunUnlockables.availableItems = originalPools; UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; }
    }
}
