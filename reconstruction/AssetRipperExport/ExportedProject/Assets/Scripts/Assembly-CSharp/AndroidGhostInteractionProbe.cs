#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Controlled interaction followed by autonomous ghost movement and damage.
internal sealed class AndroidGhostInteractionProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_ghost_interaction_probe", false)) return;
        var go = new GameObject("Ghost interaction probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidGhostInteractionProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PoolManager.Instance == null || PoolManager.Instance.ghostPool == null || DataManager.Instance == null || FindObjectOfType<InteractablePot>() == null || FindObjectOfType<DetectInteractables>() == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance; var pot = FindObjectOfType<InteractablePot>(); var detector = FindObjectOfType<DetectInteractables>();
        if (player == null || manager == null || pot == null || detector == null || PoolManager.Instance == null || PoolManager.Instance.ghostPool == null) { Debug.LogError("Ghost probe: scene unavailable"); yield break; }
        var pool = PoolManager.Instance.ghostPool;
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>();
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        player.inventory.itemInventory.AddItem(EItem.Ghost, 1);
        var data = DataManager.Instance.GetEnemyData(EEnemy.Goblin);
        var enemy = manager.SpawnEnemy(data, player.transform.position + player.transform.forward * 6f, -1, true, canBeElite: false);
        deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline && enemy != null && !enemy.CanTakeDamage()) yield return null;
        if (enemy == null || !enemy.CanTakeDamage()) { Debug.LogError("Ghost probe: target unavailable"); Restore(); yield break; }
        enemy.SetDiagnosticHealth(10000f);
        int hits = 0;
        void OnDamage(Enemy target, DamageContainer dc) { if (dc != null && dc.damageSource == EItem.Ghost.ToString()) hits++; }
        Enemy.A_Damage += OnDamage;
        int before = pool.CountActive;
        typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(detector, pot);
        detector.TryInteract();
        int spawned = pool.CountActive - before;
        Debug.Log($"Ghost probe: acquired={player.inventory.itemInventory.GetAmount(EItem.Ghost)}, spawned={spawned}, pot={pot.name}");
        yield return new WaitForSecondsRealtime(12f);
        Debug.Log($"Ghost probe {(spawned == 6 && hits > 0 ? "PASS" : "FAIL")}: spawned={spawned}, ghostDamageEvents={hits}, active={pool.CountActive}, inactive={pool.CountInactive}");
        Enemy.A_Damage -= OnDamage;
        Restore();
        void Restore() { if (manager != null) manager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable(); }
    }
}
#endif
