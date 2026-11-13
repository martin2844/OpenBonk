#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Disposable run; fills the real XP list/pool and restores it in the same frame.
internal sealed class AndroidXpCapacityProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_xp_capacity_probe", false)) return;
        var root = new GameObject("XP capacity probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidXpCapacityProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PickupManager.Instance == null || PoolManager.Instance == null || PoolManager.Instance.xpPool == null)) yield return null;
        var manager = PickupManager.Instance;
        var pool = PoolManager.Instance != null ? PoolManager.Instance.xpPool : null;
        var list = manager != null ? typeof(PickupManager).GetField("xpList", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager) as PickupStackableList : null;
        if (list == null || pool == null || list.pickupsList.Count != 0 || pool.CountActive != 0)
        { Debug.LogError("XP capacity probe INCONCLUSIVE: expected an empty live list and pool"); yield break; }
        bool waves = EnemyManager.Instance.enabledWaves;
        EnemyManager.Instance.enabledWaves = false;
        try
        {
            Check(manager, list, pool, 249, 250, false);
            Check(manager, list, pool, 250, 250, true);
        }
        finally { EnemyManager.Instance.enabledWaves = waves; }
    }
    static void Check(PickupManager manager, PickupStackableList list, UnityEngine.Pool.ObjectPool<GameObject> pool, int first, int second, bool rotate)
    {
        int limit = PickupManager.maxXpObjects;
        Pickup oldest = null, combined = null;
        for (int i = 0; i < limit; i++)
        {
            var pickup = list.AddPickup(Vector3.up * 500f);
            pickup.Set(Vector3.up * 500f, i == 0 ? first : i == 1 ? second : 1, 100f);
            if (i == 0) oldest = pickup;
            if (i == 1) combined = pickup;
        }
        var added = list.AddPickup(Vector3.up * 500f);
        added.Set(Vector3.up * 500f, 1, 100f);
        int total = 0;
        foreach (var p in list.pickupsList) total += p.GetValue();
        int expected = first + second + (limit - 2) + 1;
        bool order = rotate ? list.pickupsList.Last.Previous.Value == combined : list.pickupsList.First.Value == combined;
        bool pass = list.pickupsList.Count == limit && pool.CountActive == limit && total == expected && combined.GetValue() == first + second && added == oldest && order;
        Debug.Log($"XP capacity probe {(pass ? "PASS" : "FAIL")}: thresholdSum={first + second}, count={list.pickupsList.Count}, active={pool.CountActive}, total={total}, expected={expected}, reusedOldest={added == oldest}, expectedOrder={order}");
        while (list.pickupsList.Count > 0)
        {
            var pickup = list.pickupsList.First.Value;
            list.RemovePickup(pickup);
            manager.DespawnPickup(pickup);
        }
        if (pool.CountActive != 0) Debug.LogError($"XP capacity probe FAIL: {pool.CountActive} active after cleanup");
    }
}
#endif
