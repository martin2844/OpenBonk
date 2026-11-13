#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Disposable run: use actual world placement, pickup colliders and overlap queries.
internal sealed class AndroidPickupOverlapProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_pickup_overlap_probe", false)) return;
        var root = new GameObject("pickup overlap probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidPickupOverlapProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PickupManager.Instance == null || PoolManager.Instance == null || PoolManager.Instance.xpPool == null)) yield return null;
        var player = MyPlayer.Instance;
        var manager = PickupManager.Instance;
        if (player == null || manager == null || EnemyManager.Instance == null || PoolManager.Instance == null || PoolManager.Instance.xpPool == null) { Debug.LogError("Pickup overlap probe INCONCLUSIVE: game unavailable"); yield break; }
        bool waves = EnemyManager.Instance.enabledWaves;
        EnemyManager.Instance.enabledWaves = false;
        Vector3 origin = player.transform.position + new Vector3(20f, 0f, 20f);
        Pickup xp = null, distant = null;
        try
        {
            xp = manager.SpawnPickup(EPickup.Xp, origin, 3, false, 100f);
            Physics.SyncTransforms();
            var xpMerged = manager.SpawnPickup(EPickup.Xp, origin, 4, false, 100f);
            Physics.SyncTransforms();
            distant = manager.SpawnPickup(EPickup.Xp, origin + Vector3.right * 25f, 6, false, 100f);
            bool pass = xp != null && xpMerged == xp && xp.GetValue() == 7 && distant != null && distant != xp && distant.GetValue() == 6;
            Debug.Log($"Pickup overlap probe {(pass ? "PASS" : "FAIL")}: sameXp={xp != null && xpMerged == xp}, xpValue={(xp != null ? xp.GetValue() : -1)}, distantSeparate={distant != null && distant != xp}, distantValue={(distant != null ? distant.GetValue() : -1)}");
        }
        finally
        {
            if (xp != null) manager.PickupTriggered(xp);
            if (distant != null && distant != xp) manager.PickupTriggered(distant);
            EnemyManager.Instance.enabledWaves = waves;
        }
    }
}
#endif
