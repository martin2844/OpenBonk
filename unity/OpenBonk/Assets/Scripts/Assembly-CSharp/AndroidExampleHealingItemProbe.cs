#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in diagnostic for the independently packaged example item.
internal sealed class AndroidExampleHealingItemProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_example_healing_probe", false)) return;
        var root = new GameObject("Example healing item probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidExampleHealingItemProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 150f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        var id = (EItem)OpenBonkContentIds.Item("openbonk.example.healing", "healing-heart");
        if (player == null || player.inventory == null || player.inventory.playerHealth == null || player.inventory.playerHealth.maxHp < 4 || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaTick || data.bonuses.Length != 0)
        { Debug.LogError("Example healing item INCONCLUSIVE: item or player unavailable"); yield break; }
        var inv = player.inventory; var health = inv.playerHealth;
        int oldHp = health.hp; float oldTime = MyTime.time; bool oldPause = MyTime.paused;
        try
        {
            int low = Math.Max(1, health.maxHp / 4), high = Math.Max(low + 1, health.maxHp * 3 / 4);
            health.hp = low; inv.itemInventory.AddItem(id);
            MyTime.time = oldTime + 1.01f; MyTime.paused = true; inv.itemInventory.Tick(); int paused = health.hp;
            MyTime.paused = false; inv.itemInventory.Tick(); int healed = health.hp;
            health.hp = high; MyTime.time = oldTime + 2.02f; inv.itemInventory.Tick(); int above = health.hp;
            inv.itemInventory.RemoveItem(id);
            bool pass = paused == low && healed > low && healed <= health.maxHp && above == high;
            Debug.Log($"Example healing item {(pass ? "PASS" : "FAIL")}: low={low} paused={paused} healed={healed} above={above}/{high}");
        }
        finally { health.hp = oldHp; MyTime.time = oldTime; MyTime.paused = oldPause; }
    }
}
#endif
