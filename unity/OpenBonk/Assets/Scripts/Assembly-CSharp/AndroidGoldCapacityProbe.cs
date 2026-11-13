#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Disposable run: forces a payout but leaves coin movement/collection to the game.
internal sealed class AndroidGoldCapacityProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_gold_capacity_probe", false)) return;
        var root = new GameObject("gold capacity probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidGoldCapacityProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PoolManager.Instance == null || PoolManager.Instance.goldPool == null || !PlayerStats.HasStats())) yield return null;
        var player = MyPlayer.Instance;
        var pool = PoolManager.Instance != null ? PoolManager.Instance.goldPool : null;
        if (player == null || player.inventory == null || pool == null || pool.CountActive != 0 || Mathf.Abs(PlayerStats.GetStat(EStat.GoldIncreaseMultiplier) - 1f) > .001f)
        { Debug.LogError("Gold capacity probe INCONCLUSIVE: player, empty pool or base gold multiplier unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        int before = player.inventory.goldInt;
        MoneyUtility.SpawnMoney(640, player.transform.position + Vector3.up * 2f);
        int active = pool.CountActive, sum = 0, largest = 0, objects = 0;
        foreach (var coin in Object.FindObjectsByType<MoneyFlying>(FindObjectsSortMode.None))
        {
            if (!coin.gameObject.activeInHierarchy) continue;
            sum += coin.value; largest = Mathf.Max(largest, coin.value); objects++;
        }
        bool split = active == 30 && objects == 30 && sum == 640 && largest == 60;
        Debug.Log($"Gold capacity probe spawned: active={active}, objects={objects}, sum={sum}, largest={largest}, split={split}");
        deadline = Time.realtimeSinceStartup + 60f;
        while (Time.realtimeSinceStartup < deadline && (pool.CountActive > 0 || player.inventory.goldInt - before < 640)) yield return null;
        int gained = player.inventory.goldInt - before;
        Debug.Log($"Gold capacity probe {(split && pool.CountActive == 0 && gained == 640 ? "PASS" : "INCONCLUSIVE")}: gained={gained}, remaining={pool.CountActive}");
    }
}
#endif
