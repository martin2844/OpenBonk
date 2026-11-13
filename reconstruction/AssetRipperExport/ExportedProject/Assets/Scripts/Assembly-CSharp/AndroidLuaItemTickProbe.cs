#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

internal sealed class AndroidLuaItemTickProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_item_tick_probe", false)) return;
        var go = new GameObject("Lua item tick probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLuaItemTickProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 150f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var id = (EItem)OpenBonkContentIds.Item("openbonk-tick-test", "training-cell");
        if (player == null || player.inventory == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaTick) { Debug.LogError("Lua item tick INCONCLUSIVE: item unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false; foreach (var w in player.inventory.weaponInventory.weapons.Values) w.Disable();
        var inv = player.inventory; inv.playerStats.ForceUpdateStats(); float baseline = PlayerStats.GetStat(EStat.PowerupChance);
        float originalTime = MyTime.time; bool originalPause = MyTime.paused;
        try
        {
            inv.itemInventory.AddItem(id); float initial = Delta();
            MyTime.time = originalTime + .99f; inv.itemInventory.Tick(); float early = Delta();
            MyTime.time = originalTime + 1.01f; MyTime.paused = true; inv.itemInventory.Tick(); float paused = Delta();
            MyTime.paused = false; inv.itemInventory.Tick(); float first = Delta();
            inv.itemInventory.Tick(); float repeat = Delta();
            MyTime.time = originalTime + 11f; inv.itemInventory.Tick(); float skipped = Delta();
            inv.itemInventory.AddItem(id); float stacked = Delta();
            MyTime.time = originalTime + 12.01f; inv.itemInventory.Tick(); float next = Delta();
            inv.itemInventory.RemoveItem(id); inv.itemInventory.RemoveItem(id); float removed = Delta();
            inv.itemInventory.AddItem(id); float readded = Delta();
            MyTime.time = originalTime + 13.02f; inv.itemInventory.Tick(); float reset = Delta();
            bool pass = Near(initial,.05f) && Near(early,.05f) && Near(paused,.05f) && Near(first,.10f) && Near(repeat,.10f) && Near(skipped,.15f) && Near(stacked,.30f) && Near(next,.80f) && Near(removed,0f) && Near(readded,.05f) && Near(reset,.10f);
            Debug.Log($"Lua item tick {(pass ? "PASS" : "FAIL")}: initial={initial:F3} early={early:F3} paused={paused:F3} first={first:F3} repeat={repeat:F3} skipped={skipped:F3} stacked={stacked:F3} next={next:F3} removed={removed:F3} readded={readded:F3} reset={reset:F3}");
            inv.itemInventory.RemoveItem(id);
        }
        finally { MyTime.time = originalTime; MyTime.paused = originalPause; }
        inv.itemInventory.AddItem(id);
        float start = MyTime.time; deadline = Time.realtimeSinceStartup + 20f;
        // A slow frame can delay the first callback, and missed intervals are
        // intentionally not replayed. Wait for two observed ticks with a bound.
        while (MyTime.time - start < 4f && Time.realtimeSinceStartup < deadline && Delta() < .148f) yield return null;
        float natural = Delta();
        Debug.Log($"Lua item tick natural {(MyTime.time - start >= 2f && Near(natural,.15f) ? "PASS" : "FAIL")}: elapsed={MyTime.time-start:F3} delta={natural:F3}");
        inv.itemInventory.RemoveItem(id);
        float Delta() { inv.playerStats.ForceUpdateStats(); return PlayerStats.GetStat(EStat.PowerupChance) - baseline; }
    }
    private static bool Near(float a, float b) => Math.Abs(a-b) < .002f;
}
#endif
