using System;
using System.Reflection;
using TMPro;
using UnityEngine;

public static class KillsGoldHudQueuePortCheck
{
    public static void Run()
    {
        var obj = new GameObject("kills and gold queue check");
        var hud = obj.AddComponent<KillsAndGoldCounter>();
        hud.t_kills = new GameObject("kills", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        hud.t_gold = new GameObject("gold", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        hud.t_kills.text = "old kills"; hud.t_gold.text = "old gold";
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var type = typeof(KillsAndGoldCounter);
        type.GetField("killsString", flags).SetValue(hud, "kills");
        var onKills = type.GetMethod("OnRunStatChanged", flags);
        var onGold = type.GetMethod("OnGoldIncrease", flags);
        onKills.Invoke(hud, new object[] { "other", 1f });
        if ((bool)type.GetField("queuedKillsUpdate", flags).GetValue(hud)) throw new Exception("unrelated stat queued kills");
        for (int i = 0; i < 3; i++) { onKills.Invoke(hud, new object[] { "kills", (float)i }); onGold.Invoke(hud, new object[] { null, 1 }); }
        if (hud.t_kills.text != "old kills" || hud.t_gold.text != "old gold") throw new Exception("counters changed before Update");
        type.GetMethod("Update", flags).Invoke(hud, null);
        if ((bool)type.GetField("queuedKillsUpdate", flags).GetValue(hud) || (bool)type.GetField("queuedGoldUpdate", flags).GetValue(hud)) throw new Exception("queue not cleared");
        if (hud.t_kills.text == "old kills" || hud.t_gold.text == "old gold") throw new Exception("queued counters not refreshed");
        Debug.Log("KILLS_GOLD_HUD_QUEUE_PORT_CHECK_OK");
        UnityEngine.Object.DestroyImmediate(hud.t_kills.gameObject); UnityEngine.Object.DestroyImmediate(hud.t_gold.gameObject); UnityEngine.Object.DestroyImmediate(obj);
    }
}
