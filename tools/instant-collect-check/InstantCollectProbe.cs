#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Managers;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

// Disposable build-only probe; not included in distributed runtime sources.
internal sealed class InstantCollectProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "instant_xp_probe", false)) return;
        var root = new GameObject("instant XP probe"); DontDestroyOnLoad(root); root.AddComponent<InstantCollectProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PickupManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || PickupManager.Instance == null || EnemyManager.Instance == null) { Debug.LogError("Instant XP probe INCONCLUSIVE: game unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        var xp = player.inventory.playerXp; xp.xp = 0; xp.level = 9999;
        typeof(PlayerXp).GetField("leftOverXp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(xp, 0f);
        int events = 0; void OnPickup(Pickup p) { if (p != null && p.ePickup == EPickup.Xp) events++; }
        Pickup.A_PickupTriggered += OnPickup;
        var manager = PickupManager.Instance; Vector3 pos = player.transform.position + Vector3.right * 25f;
        bool startedEnabled = OpenBonkPickupPolicy.CollectXpImmediately;
        OpenBonkPickupPolicy.SetCollectXpImmediately(false);
        var original = manager.SpawnPickup(EPickup.Xp, pos, 5, false, 0f);
        Physics.SyncTransforms();
        bool vanilla = original != null && original.gameObject.activeInHierarchy && xp.xp == 0 && events == 0;
        OpenBonkPickupPolicy.SetCollectXpImmediately(true);
        var merged = manager.SpawnPickup(EPickup.Xp, pos, 7, false, 0f);
        // PlayerInventory queues XP until LateUpdate; the pickup event and
        // pooling happen synchronously, while xp.xp is updated next frame.
        bool collected = merged == original && events == 1 && !merged.gameObject.activeInHierarchy;
        yield return new WaitForSecondsRealtime(2f);
        bool once = xp.xp == 12 && events == 1;
        OpenBonkPickupPolicy.SetCollectXpImmediately(false);
        var restored = manager.SpawnPickup(EPickup.Xp, pos, 3, false, 0f);
        bool off = restored != null && restored.gameObject.activeInHierarchy && xp.xp == 12 && events == 1;
        restored?.ApplyPickup();
        yield return null;
        bool normal = xp.xp == 15 && events == 2 && restored != null && !restored.gameObject.activeInHierarchy;
        Pickup.A_PickupTriggered -= OnPickup;
        Debug.Log($"Instant XP probe {(startedEnabled && vanilla && collected && once && off && normal ? "PASS" : "FAIL")}: startup={startedEnabled}, vanilla={vanilla}, merged={collected}, once={once}, disabled={off}, normal={normal}, xp={xp.xp}, events={events}");
    }
}
#endif
