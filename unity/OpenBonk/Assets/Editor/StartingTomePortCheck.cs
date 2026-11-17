using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

public static class StartingTomePortCheck
{
    private static void Require(bool ok, string detail) { if (!ok) throw new Exception("Starting tome: " + detail); }
    public static void Run()
    {
        var previous = DataManager.Instance;
        var go = new GameObject("starting tome test");
        var data = ScriptableObject.CreateInstance<TomeData>();
        Action<ETome, EStat> onUpgrade = null;
        try
        {
            var manager = go.AddComponent<DataManager>(); DataManager.Instance = manager;
            data.eTome = ETome.Luck;
            data.statModifier = new StatModifier { stat = EStat.Luck, modifyType = EStatModifyType.Flat, modification = 1 };
            var field = typeof(DataManager).GetField("tomeData", BindingFlags.Instance | BindingFlags.NonPublic);
            ((Dictionary<ETome, TomeData>)field.GetValue(manager)).Add(ETome.Luck, data);
            Require(OpenBonkStartingTomes.TryParseHook(1, "starting_tome:Luck", out var id) && id == ETome.Luck, "parse");
            Require(!OpenBonkStartingTomes.TryParseHook(1, "starting_tome:13", out _) && !OpenBonkStartingTomes.TryParseHook(2, "starting_tome:Luck", out _), "invalid hook");
            Require(!OpenBonkStartingTomes.ValidValues(double.NaN, 99) && !OpenBonkStartingTomes.ValidValues(10001, 99) && !OpenBonkStartingTomes.ValidValues(10000, 99.5), "invalid values");
            var run = new TomeInventory(); int events = 0; onUpgrade = (t, s) => events++;
            TomeInventory.A_TomeUpgrade += onUpgrade;
            Require(OpenBonkStartingTomes.Grant(run, "lucky-tome", data, 10000, 99), "initial grant");
            Require(run.GetTomeLevel(ETome.Luck) == 99 && run.tomeUpgrade[ETome.Luck].modification == 10000 && events == 99, "level, stats and events");
            Require(!OpenBonkStartingTomes.Grant(run, "lucky-tome", data, 10000, 99), "duplicate rejected");
            Require(events == 99 && run.tomeUpgrade[ETome.Luck].modification == 10000, "no duplicate rewards");
            var nextRun = new TomeInventory();
            Require(OpenBonkStartingTomes.Grant(nextRun, "lucky-tome", data, 10000, 99), "new run granted");
            Require(nextRun.GetTomeLevel(ETome.Luck) == 99 && events == 198, "new run independent");
            Debug.Log("StartingTomePortCheck PASS: exact values and events, repeated spawn and independent run");
        }
        finally
        {
            if (onUpgrade != null) TomeInventory.A_TomeUpgrade -= onUpgrade;
            DataManager.Instance = previous;
            UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
