using System;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using TMPro;
using UnityEngine;

public static class ShrineLogsPortCheck
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Start(ShrineLogs logs) => typeof(ShrineLogs).GetMethod("Start", Private).Invoke(logs, null);
    static ShrineLogEntry Entry(GameObject parent, int index) => parent.transform.GetChild(index).GetComponent<ShrineLogEntry>();
    static void CheckText(ShrineLogEntry entry, StatModifier modifier, string color)
    {
        string expected = "<color=" + color + ">" + StatUtility.GetModificationString(modifier, false) + "</color> " + EnumUtility.EnumToReadable(modifier.stat);
        Check(entry.text.text == expected, "entry text and color");
    }
    public static void Run()
    {
        ShrineLogs.Reset();
        var root = new GameObject("shrine log root", typeof(RectTransform)); root.SetActive(false);
        var ui = root.AddComponent<UiManager>(); var logs = root.AddComponent<ShrineLogs>(); UiManager.Instance = ui; ui.shrineLogs = logs;
        var parent = new GameObject("log parent", typeof(RectTransform)); parent.transform.SetParent(root.transform, false);
        var prefab = new GameObject("log prefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        prefab.transform.SetParent(parent.transform, false); prefab.SetActive(false);
        var text = prefab.GetComponent<TextMeshProUGUI>(); var sizer = prefab.AddComponent<TextSizer>(); sizer.Text = text;
        var entry = prefab.AddComponent<ShrineLogEntry>(); entry.text = text; entry.textSizer = sizer; logs.prefab = prefab;
        var up = new StatModifier { stat = EStat.MaxHealth, modifyType = EStatModifyType.Flat, modification = 5f };
        var down = new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Multiplication, modification = .8f };
        try
        {
            var stats = new StatInventory(); stats.ChangeStat(up, true, 0f, true);
            Check(parent.transform.childCount == 1, "before start queued");
            Start(logs); Check(parent.transform.childCount == 2, "start replays queued stat");
            Check(MyColorUtility.requirementCompletedColor == "#0ceb2d" && MyColorUtility.requirementMissingColor == "#ff6a38", "original nonempty color literals");
            CheckText(Entry(parent, 1), up, MyColorUtility.requirementCompletedColor);
            stats.ChangeStat(down, true, 0f, true); Check(parent.transform.childCount == 3, "new stat creates entry");
            CheckText(Entry(parent, 2), down, MyColorUtility.requirementMissingColor);
            var second = root.AddComponent<ShrineLogs>(); second.prefab = prefab; Start(second);
            Check(parent.transform.childCount == 5, "next scene replays exactly once");
            ShrineLogs.Reset(); var third = root.AddComponent<ShrineLogs>(); third.prefab = prefab; Start(third);
            Check(parent.transform.childCount == 5, "new run reset clears history");
            Debug.Log("ShrineLogsPortCheck passed: real stat inventory, backlog, colors, replay and reset");
        }
        finally { ShrineLogs.Reset(); UiManager.Instance = null; UnityEngine.Object.DestroyImmediate(root); }
    }
}
