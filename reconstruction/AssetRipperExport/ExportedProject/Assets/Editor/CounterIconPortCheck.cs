using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using TMPro;
using UnityEngine;

public static class CounterIconPortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    static TextMeshProUGUI Text(Transform parent) { var go = new GameObject("text", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent); return go.GetComponent<TextMeshProUGUI>(); }
    public static void Run()
    {
        var root = new GameObject("counter test"); var counter = root.AddComponent<KillsAndGoldCounter>(); counter.t_kills = Text(root.transform); counter.t_gold = Text(root.transform); counter.t_silver = Text(root.transform); counter.t_chestPrice = Text(root.transform);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        typeof(PlayerInventory).GetProperty("goldInt").GetSetMethod(true).Invoke(player.inventory, new object[] { 1234 });
        player.inventory.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); player.inventory.playerStats.stats = new Dictionary<EStat, float> { [EStat.ChestPriceMultiplier] = 1f };
        GameManager.Instance = root.AddComponent<GameManager>();
        var save = root.AddComponent<SaveManager>(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save }); save.progression = new ProgressionSaveFile(); save.progression.silver = 2345;
        typeof(RunStats).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null); RunStats.AddValue(TrackStats.GetStatString(EMyStat.kills), 42);
        Call(counter, "UpdateKillCounter"); Call(counter, "UpdateGoldCounter"); Call(counter, "OnSilverChange", 0); Call(counter, "OnChestPriceIncreased");
        Check(counter.t_kills.text.Contains("<sprite name=skull>") && counter.t_kills.text.Contains("42"), "kill icon and count");
        Check(counter.t_gold.text.Contains("<sprite name=gold>") && counter.t_gold.text.Contains(1234.ToString("N0")), "gold icon and count");
        Check(counter.t_silver.text.Contains("<sprite name=silver>") && counter.t_silver.text.Contains(2345.ToString("N0")), "silver icon and count");
        Check(counter.t_chestPrice.text.Contains("<sprite name=chest>") && counter.t_chestPrice.text.Contains("<sprite name=gold>") && counter.t_chestPrice.text.EndsWith("30"), "chest and gold icons with price");
        MyPlayer.Instance = null; GameManager.Instance = null; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null }); UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("CounterIconPortCheck passed: original skull, gold, silver and chest tags and values");
    }
}
