#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Game.Spawning.New.Timelines;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in controlled clock/curse setup. The manager's ordinary fixed-update path starts the timeline event.
internal sealed class AndroidMinibossTimelineProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_miniboss_timeline_probe", false)) return;
        var go = new GameObject("Miniboss timeline probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidMinibossTimelineProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || EnemyManager.Instance == null || EnemyManager.Instance.summonerController == null || MapController.currentStage == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance; var game = GameManager.Instance;
        if (player == null || manager == null || game == null || manager.summonerController == null || MapController.currentStage == null) { Debug.LogError("Miniboss timeline probe: scene unavailable"); yield break; }
        var controller = manager.summonerController; var events = MapController.currentStage.stageTimeline.events;
        var currentEvent = typeof(SummonerController).GetField("currentTimelineEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        if (events == null || events.Count == 0 || events[0].eTimelineEvent != ETimelineEvent.EMiniBoss || (int)currentEvent.GetValue(controller) != -1) { Debug.LogError("Miniboss timeline probe: expected unstarted first miniboss event unavailable"); yield break; }
        bool waves = manager.enabledWaves; manager.enabledWaves = true;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        var until = typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.NonPublic | BindingFlags.Instance);
        bool oldInvincible = (bool)invincible.GetValue(player); float oldUntil = (float)until.GetValue(player);
        invincible.SetValue(player, true); until.SetValue(player, MyTime.time + 10000f);
        int oldCurses = game.bossCurses; float oldStageTimer = MyTime.stageTimer; game.bossCurses = 2;
        var flags = typeof(Enemy).GetField("enemyFlag", BindingFlags.NonPublic | BindingFlags.Instance);
        var spawned = new List<Enemy>(); int notifications = 0; float observedAt = -1f;
        Enemy.A_EnemySpawned += OnSpawn; SummonerController.A_MiniBoss += OnMiniboss;
        float scheduled = events[0].GetTimeSeconds(); MyTime.stageTimer = scheduled - 1f;
        Debug.Log($"Miniboss timeline probe: scheduled={scheduled:F2}, start={MyTime.stageTimer:F2}, curses={game.bossCurses}, waves={manager.enabledWaves}");
        deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && notifications == 0) yield return new WaitForSecondsRealtime(.1f);
        yield return new WaitForSecondsRealtime(1f);
        Debug.Log($"Miniboss timeline probe: finished spawned={spawned.Count}, notifications={notifications}, observedStage={observedAt:F2}, eventIndex={(int)currentEvent.GetValue(controller)}, eventCount={controller.minibossCount}");
        Enemy.A_EnemySpawned -= OnSpawn; SummonerController.A_MiniBoss -= OnMiniboss;
        game.bossCurses = oldCurses; MyTime.stageTimer = oldStageTimer; if (player != null) { invincible.SetValue(player, oldInvincible); until.SetValue(player, oldUntil); } if (manager != null) manager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable();
        void OnSpawn(Enemy enemy) { if (((EEnemyFlag)flags.GetValue(enemy) & EEnemyFlag.SummonerMiniboss) == 0) return; spawned.Add(enemy); Debug.Log($"Miniboss timeline probe: spawned={enemy.enemyData.enemyName}, stage={MyTime.stageTimer:F2}, position={enemy.transform.position}"); }
        void OnMiniboss() { notifications++; observedAt = MyTime.stageTimer; }
    }
}
#endif
