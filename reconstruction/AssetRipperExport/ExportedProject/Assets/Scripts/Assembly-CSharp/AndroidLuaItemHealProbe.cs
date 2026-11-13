#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

internal sealed class AndroidLuaItemHealProbe : MonoBehaviour
{
    int events;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_item_heal_probe", false)) return;
        var root = new GameObject("Lua heal probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidLuaItemHealProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 150f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var p = MyPlayer.Instance; var id = (EItem)OpenBonkContentIds.Item("openbonk-heal-test", "training-cell");
        if (p == null || p.inventory == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaTick || data.bonuses.Length != 0) { Debug.LogError("Lua item heal INCONCLUSIVE: item unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false; foreach (var w in p.inventory.weaponInventory.weapons.Values) w.Disable();
        var inv = p.inventory; var hp = inv.playerHealth; int oldHp = hp.hp; float oldTime = MyTime.time; bool oldPause = MyTime.paused;
        PlayerHealth.A_Heal += OnHeal;
        try
        {
            hp.hp = 50; inv.itemInventory.AddItem(id);
            MyTime.time = oldTime + .99f; inv.itemInventory.Tick(); int early = hp.hp;
            MyTime.time = oldTime + 1.01f; MyTime.paused = true; inv.itemInventory.Tick(); int paused = hp.hp;
            MyTime.paused = false; inv.itemInventory.Tick(); int healed = hp.hp; int firstEvents = events;
            MyTime.time = oldTime + 2.02f; inv.itemInventory.Tick(); int invalid = hp.hp; int finalEvents = events;
            MyTime.time = oldTime + 3.03f; inv.itemInventory.Tick(); int stopped = hp.hp;
            inv.itemInventory.RemoveItem(id);
            bool pass = early == 50 && paused == 50 && healed == 57 && invalid == 57 && stopped == 57 && firstEvents == 1 && finalEvents == 1;
            Debug.Log($"Lua item heal {(pass ? "PASS" : "FAIL")}: early={early} paused={paused} healed={healed} invalid={invalid} stopped={stopped} healEvents={firstEvents}/{finalEvents}");
        }
        finally { PlayerHealth.A_Heal -= OnHeal; hp.hp = oldHp; MyTime.time = oldTime; MyTime.paused = oldPause; }
    }
    void OnHeal(PlayerHealth health, float amount, bool shield) { if (health == MyPlayer.Instance?.inventory?.playerHealth) events++; }
    void OnDestroy() { PlayerHealth.A_Heal -= OnHeal; }
}
#endif
