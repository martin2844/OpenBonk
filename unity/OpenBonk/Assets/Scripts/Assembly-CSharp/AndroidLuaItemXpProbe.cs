#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

// Runs only when explicitly launched with the diagnostic intent extra.
internal sealed class AndroidLuaItemXpProbe : MonoBehaviour
{
    private int events, granted;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_item_xp_probe", false)) return;
        var go = new GameObject("Lua item XP probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLuaItemXpProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 150f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var id = (EItem)OpenBonkContentIds.Item("openbonk.example.xp", "xp-spark");
        if (player == null || player.inventory == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaTick) { Debug.LogError("Lua item XP INCONCLUSIVE: imported example unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false; foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var inventory = player.inventory; var xp = inventory.playerXp;
        if (xp == null || inventory.playerHealth == null || inventory.playerHealth.hp <= 0) { Debug.LogError("Lua item XP INCONCLUSIVE: player unavailable"); yield break; }
        float oldTime = MyTime.time; bool oldPause = MyTime.paused; int before = xp.xp;
        PlayerXp.A_XpAdded += OnXp;
        try
        {
            inventory.itemInventory.AddItem(id);
            MyTime.time = oldTime + .99f; inventory.itemInventory.Tick(); inventory.LateUpdate(); int early = xp.xp;
            MyTime.time = oldTime + 1.01f; MyTime.paused = true; inventory.itemInventory.Tick(); inventory.LateUpdate(); int paused = xp.xp;
            MyTime.paused = false; inventory.itemInventory.Tick(); inventory.LateUpdate(); int first = xp.xp;
            inventory.itemInventory.AddItem(id);
            MyTime.time = oldTime + 2.02f; MyTime.paused = false; inventory.itemInventory.Tick(); inventory.LateUpdate(); int second = xp.xp;
            inventory.itemInventory.RemoveItem(id); inventory.itemInventory.RemoveItem(id);
            MyTime.time = oldTime + 3.03f; MyTime.paused = false; inventory.itemInventory.Tick(); inventory.LateUpdate(); int removed = xp.xp;
            int firstGain = first - before, secondGain = second - first;
            bool pass = early == before && paused == before && firstGain > 0 && secondGain >= firstGain * 2 - 1 && secondGain <= firstGain * 2 + 1 && removed == second && events == 2 && granted == firstGain + secondGain;
            Debug.Log($"Lua imported XP item {(pass ? "PASS" : "FAIL")}: early={early-before} paused={paused-before} first={firstGain} second={secondGain} removed={removed-second} events={events} granted={granted}");
        }
        finally { PlayerXp.A_XpAdded -= OnXp; MyTime.time = oldTime; MyTime.paused = oldPause; }
    }
    private void OnXp(PlayerXp xp, int amount) { if (MyPlayer.Instance?.inventory?.playerXp == xp) { events++; granted += amount; } }
    private void OnDestroy() { PlayerXp.A_XpAdded -= OnXp; }
}
#endif
