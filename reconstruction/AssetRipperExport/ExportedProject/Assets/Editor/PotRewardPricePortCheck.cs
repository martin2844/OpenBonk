using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEditor;
using UnityEngine;

public static class PotRewardPricePortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run()
    {
        var root = new GameObject("pot price fixture"); root.SetActive(false);
        var oldManager = GameManager.Instance; var oldPlayer = MyPlayer.Instance; var oldData = DataManager.Instance;
        var purchased = typeof(MoneyUtility).GetField("chestsPurchased", BindingFlags.Static | BindingFlags.NonPublic);
        int oldPurchased = (int)purchased.GetValue(null);
        ItemInventory items = null;
        try
        {
            GameManager.Instance = root.AddComponent<GameManager>();
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
            items = new ItemInventory(); inventory.itemInventory = items;
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            stats.stats = new Dictionary<EStat, float> { [EStat.ChestPriceMultiplier] = 1f }; inventory.playerStats = stats;
            var pumpkin = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Pumpkin.asset");
            Check(pumpkin != null && pumpkin.eItem == EItem.Pumpkin, "original Pumpkin asset");
            var all = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data); all.Add(EItem.Pumpkin, pumpkin);
            purchased.SetValue(null, 0);
            Check(MoneyUtility.GetChestPrice() == 30 && MoneyUtility.GetPotMoney(false) == 9 && MoneyUtility.GetPotMoney(true) == 18, "base chest and pot values");
            items.AddItem(EItem.Pumpkin, 2);
            Check(MoneyUtility.GetPotMoney(false) == 12 && MoneyUtility.GetPotMoney(true) == 24, "two Pumpkin stacks scale both pot sizes");
            purchased.SetValue(null, 25);
            Check(MoneyUtility.GetChestPrice() > 3334 && MoneyUtility.GetPotMoney(false) == 1360 && MoneyUtility.GetPotMoney(true) == 2720, "cap is applied before Pumpkin and big-pot multipliers");
            items.RemoveItem(EItem.Pumpkin); items.RemoveItem(EItem.Pumpkin);
            Check(MoneyUtility.GetPotMoney(false) == 1000 && MoneyUtility.GetPotMoney(true) == 2000, "removal restores capped base reward");
            purchased.SetValue(null, 0); stats.stats[EStat.ChestPriceMultiplier] = .04f;
            Check(MoneyUtility.GetChestPrice() == 1 && MoneyUtility.GetPotMoney(false) == 0, "low chest price rounds pot reward");
            Debug.Log("PotRewardPricePortCheck passed: original item, base rewards, stacked multiplier, cap, removal and low price");
        }
        finally
        {
            items?.Cleanup(); purchased.SetValue(null, oldPurchased); MyPlayer.Instance = oldPlayer; GameManager.Instance = oldManager; DataManager.Instance = oldData;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
