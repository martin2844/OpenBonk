using System;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using TMPro;
using UnityEngine;

public static class XpGoldHudPortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Call(object obj, string name) => obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, null);
    static XpAndGoldText MakeText(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.SetActive(false);
        var result = go.AddComponent<XpAndGoldText>();
        result.text = go.GetComponent<TextMeshProUGUI>();
        result.prefix = name + ":";
        return result;
    }
    public static void Run()
    {
        var saveGo = new GameObject("fixture save"); saveGo.SetActive(false);
        var save = saveGo.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile();
        var host = new GameObject("fixture hud"); host.SetActive(false);
        var hud = host.AddComponent<XpAndGoldHUD>();
        var xp = MakeText("xp"); var gold = MakeText("gold"); var silver = MakeText("silver");
        hud.xpText = xp; hud.goldText = gold; hud.silverText = silver;
        float previousTime = MyTime.time;
        try
        {
            MyTime.time = 10f; Call(hud, "Awake");
            save.config.cfVisualsSettings.xp_gold_hud = 0;
            PlayerXp.A_XpAdded?.Invoke(null, 2); PlayerInventory.A_GoldChange?.Invoke(null, 3); ProgressionSaveFile.A_SilverChanged?.Invoke(4);
            Check(!xp.gameObject.activeSelf && !gold.gameObject.activeSelf && !silver.gameObject.activeSelf, "disabled setting");
            save.config.cfVisualsSettings.xp_gold_hud = 1;
            PlayerXp.A_XpAdded?.Invoke(null, 2); PlayerXp.A_XpAdded?.Invoke(null, 5);
            PlayerInventory.A_GoldChange?.Invoke(null, 3); ProgressionSaveFile.A_SilverChanged?.Invoke(4);
            Check(xp.gameObject.activeSelf && xp.text.text == "xp:7" && gold.text.text == "gold:3" && silver.text.text == "silver:4", "events and accumulated values");
            PlayerXp.A_XpAdded?.Invoke(null, -1); Check(xp.text.text == "xp:7", "negative delta ignored");
            Check(Mathf.Abs(xp.transform.localScale.x - 1.69f) < .001f, "scale bump");
            MyTime.time = 11.125f; Call(xp, "Update"); Check(Mathf.Abs(xp.text.alpha - 0.5f) < .001f, "half fade");
            MyTime.time = 11.6f; Call(xp, "Update"); Check(!xp.gameObject.activeSelf, "timeout");
            PlayerXp.A_XpAdded?.Invoke(null, 9); Check(xp.gameObject.activeSelf && xp.text.text == "xp:9", "inactive reset");
            Call(hud, "OnDestroy"); PlayerXp.A_XpAdded?.Invoke(null, 8); Check(xp.text.text == "xp:9", "unsubscription");
            Debug.Log("XpGoldHudPortCheck passed: setting, events, accumulation, negative deltas, fade, expiry and cleanup");
        }
        finally
        {
            Call(hud, "OnDestroy"); MyTime.time = previousTime;
            UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(xp.gameObject); UnityEngine.Object.DestroyImmediate(gold.gameObject); UnityEngine.Object.DestroyImmediate(silver.gameObject); UnityEngine.Object.DestroyImmediate(saveGo);
        }
    }
}
