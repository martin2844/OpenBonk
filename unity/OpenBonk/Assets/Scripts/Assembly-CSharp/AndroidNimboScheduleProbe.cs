#if UNITY_ANDROID && !UNITY_EDITOR && (!OPENBONK_BETA_NO_PROBES || OPENBONK_DISTRICT_QA)
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

// Explicit QA intent only. The normal EnemyManager.FixedUpdate dispatches each
// boundary; this probe never invokes SummonerController.StartEvent itself.
internal sealed class AndroidNimboScheduleProbe : MonoBehaviour
{
    private static readonly int[][] Seconds = { new[] { 180, 240, 420, 480 }, new[] { 120, 180, 360, 420 }, new[] { 90, 150, 240, 300 } };
    private static readonly int[][] Durations = { new[] { 30, 30, 30, 30 }, new[] { 15, 30, 30, 45 }, new[] { 30, 30, 30, 45 } };
    private static readonly ETimelineEvent[] Kinds = { ETimelineEvent.EMiniBoss, ETimelineEvent.ESwarm, ETimelineEvent.ESwarm, ETimelineEvent.EMiniBoss };
    private static readonly FieldInfo EventIndex = typeof(SummonerController).GetField("currentTimelineEvent", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo Invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo DamageUntil = typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ScaleRemaining = typeof(MyTime).GetField("timescaleTimeRemaining", BindingFlags.Static | BindingFlags.NonPublic);
    private readonly List<WeaponBase> disabledWeapons = new List<WeaponBase>();
    private readonly List<(Enemy enemy, uint id)> spawned = new List<(Enemy, uint)>();
    private readonly List<ETimelineEvent> observed = new List<ETimelineEvent>();
    private readonly List<float> observedTimes = new List<float>();
    private EnemyManager manager;
    private MyPlayer player;
    private SummonerController original, test;
    private bool prepared, oldWaves, oldInvincible, oldPaused;
    private float oldDamageUntil, oldStageTime, oldScale, oldScaleRemaining;
    private int oldCurses;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "openbonk_nimbo_schedule_probe", false)) return;
        var root = new GameObject("Nimbo schedule probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidNimboScheduleProbe>();
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180;
        while (Time.realtimeSinceStartup < deadline && !Ready()) yield return null;
        if (!Ready()) { Fail("first Nimbo stage not ready"); Destroy(gameObject); yield break; }
        if (!CheckLoadedSchedule()) { Destroy(gameObject); yield break; }
        try
        {
            Prepare();
            // This is MyTime's simulation multiplier, not Unity Time.timeScale.
            // Unity's FixedUpdate continues while elapsed gameplay time is held.
            MyTime.SetTimeScale(0, 999);
            for (int i = 0; i < Kinds.Length; i++)
            {
                int scheduled = Seconds[0][i];
                MyTime.stageTimer = scheduled - .125f;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                if (observed.Count != i || (int)EventIndex.GetValue(test) != i - 1)
                { Fail("event dispatched before threshold " + scheduled); yield break; }
                if (i == 0)
                {
                    MyTime.Pause(); MyTime.stageTimer = scheduled;
                    yield return new WaitForSecondsRealtime(.25f);
                    if (observed.Count != 0 || MyTime.stageTimer != scheduled || (int)EventIndex.GetValue(test) != -1)
                    { Fail("paused due event advanced"); yield break; }
                    MyTime.Unpause();
                    Debug.Log("Nimbo schedule probe pause PASS: due event stayed pending and stage clock held");
                }
                else MyTime.stageTimer = scheduled;
                deadline = Time.realtimeSinceStartup + 5;
                while (Time.realtimeSinceStartup < deadline && observed.Count == i) yield return null;
                if (observed.Count != i + 1 || observed[i] != Kinds[i] || observedTimes[i] != scheduled || (int)EventIndex.GetValue(test) != i)
                { Fail("missing/wrong event at threshold " + scheduled); yield break; }
                for (int tick = 0; tick < 3; tick++) yield return new WaitForFixedUpdate();
                if (observed.Count != i + 1 || (int)EventIndex.GetValue(test) != i)
                { Fail("duplicate event at threshold " + scheduled); yield break; }
                Debug.Log($"Nimbo schedule probe boundary PASS: stage=1 kind={Kinds[i]} elapsed={observedTimes[i]:F3} remaining={600-scheduled} index={i} once=True");
            }
            if (test.minibossCount != 2) { Fail("miniboss count"); yield break; }
            Debug.Log("Nimbo schedule probe PASS: all three loaded native schedules; four stage-one boundaries through ordinary FixedUpdate, pause gate and exactly-once callbacks; clock accelerated for QA, not an endurance run");
        }
        finally { Cleanup(); Destroy(gameObject); }
    }

    private static bool Ready() => GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene &&
        GameManager.Instance.GetAliveTime() > 1 && !MyTime.paused && MyPlayer.Instance != null && MyPlayer.Instance.hasStarted &&
        MyPlayer.Instance.inventory?.weaponInventory != null && EnemyManager.Instance?.summonerController != null &&
        MapController.currentMap is OpenBonkExternalMapData map && map.displayName == "Nimbo District" &&
        MapController.currentStage == map.stages[0];

    private static bool CheckLoadedSchedule()
    {
        var map = (OpenBonkExternalMapData)MapController.currentMap;
        if (map.stageDuration != 600 || map.stages.Length != 3) { Fail("loaded map duration"); return false; }
        for (int stage = 0; stage < 3; stage++)
        {
            var timeline = map.stages[stage].stageTimeline;
            if (timeline == null || timeline.stageTime != 600 - stage * 60 || timeline.checkNewEnemyInterval != 60 || timeline.events == null || timeline.events.Count != 4)
            { Fail("loaded stage timeline " + (stage + 1)); return false; }
            for (int i = 0; i < 4; i++)
            {
                var entry = timeline.events[i];
                if (entry == null || entry.eTimelineEvent != Kinds[i] || entry.GetTimeSeconds() != Seconds[stage][i] || entry.duration != Durations[stage][i])
                { Fail("loaded stage event " + (stage + 1) + "/" + i); return false; }
            }
            Debug.Log($"Nimbo schedule probe loaded PASS: stage={stage+1} duration={timeline.stageTime} reinforcement={timeline.checkNewEnemyInterval} events={string.Join(",", Seconds[stage])}");
        }
        return true;
    }

    private void Prepare()
    {
        manager = EnemyManager.Instance; player = MyPlayer.Instance; original = manager.summonerController;
        oldWaves = manager.enabledWaves; oldCurses = GameManager.Instance.bossCurses; oldPaused = MyTime.paused;
        oldStageTime = MyTime.stageTimer; oldScale = MyTime.timeScale; oldScaleRemaining = (float)ScaleRemaining.GetValue(null);
        oldInvincible = (bool)Invincible.GetValue(player); oldDamageUntil = (float)DamageUntil.GetValue(player);
        prepared = true;
        Invincible.SetValue(player, true); DamageUntil.SetValue(player, MyTime.time + 10000);
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values)
            if (weapon.enabled) { disabledWeapons.Add(weapon); weapon.Disable(); }
        GameManager.Instance.bossCurses = 0;
        test = new SummonerController(); manager.summonerController = test; manager.enabledWaves = true;
        Enemy.A_EnemySpawned += OnEnemySpawned;
        SummonerController.A_MiniBoss += OnMiniboss; SummonerController.A_SwarmStarted += OnSwarm;
    }
    private void OnEnemySpawned(Enemy enemy) { spawned.Add((enemy, enemy.id)); }
    private void OnMiniboss() { observed.Add(ETimelineEvent.EMiniBoss); observedTimes.Add(MyTime.stageTimer); }
    private void OnSwarm() { observed.Add(ETimelineEvent.ESwarm); observedTimes.Add(MyTime.stageTimer); }
    private static void Fail(string reason) { Debug.LogError("Nimbo schedule probe FAIL: " + reason); }
    private void OnDestroy() { Cleanup(); }

    private void Cleanup()
    {
        if (!prepared) return;
        prepared = false;
        Enemy.A_EnemySpawned -= OnEnemySpawned;
        SummonerController.A_MiniBoss -= OnMiniboss; SummonerController.A_SwarmStarted -= OnSwarm;
        test?.Cleanup();
        if (manager != null && (test == null || ReferenceEquals(manager.summonerController, test)))
        {
            manager.summonerController = original; manager.enabledWaves = oldWaves;
            foreach (var entry in spawned)
                if (entry.enemy != null && entry.enemy.gameObject.activeInHierarchy && manager.enemies.TryGetValue(entry.id, out var current) && current == entry.enemy)
                    entry.enemy.ReleaseToPool();
            MyTime.stageTimer = oldStageTime;
            MyTime.SetTimeScale(oldScale, oldScaleRemaining);
            if (oldPaused) MyTime.Pause(); else MyTime.Unpause();
            if (GameManager.Instance != null) GameManager.Instance.bossCurses = oldCurses;
        }
        else original?.Cleanup();
        if (player != null) { Invincible.SetValue(player, oldInvincible); DamageUntil.SetValue(player, oldDamageUntil); }
        foreach (var weapon in disabledWeapons) if (weapon != null) weapon.Enable();
        disabledWeapons.Clear(); spawned.Clear();
    }
}
#endif
