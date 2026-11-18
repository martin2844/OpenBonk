#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

internal sealed class AndroidCustomEnemyProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" ||
            !intent.Call<bool>("openbonk_custom_enemy_probe", false)) return;
        var probe = new GameObject("Custom enemy smoke test");
        DontDestroyOnLoad(probe);
        probe.AddComponent<AndroidCustomEnemyProbe>();
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 150;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (MyPlayer.Instance != null && MyPlayer.Instance.hasStarted && !MyPlayer.Instance.isTeleporting &&
                GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene &&
                EnemyManager.Instance != null &&
                OpenBonkExternalMapData.GetEncounters(MapController.currentMap, MapController.currentStage) != null)
                break;
            yield return null;
        }

        var manager = EnemyManager.Instance;
        if (Time.realtimeSinceStartup >= deadline || manager == null) { Debug.LogError("Custom enemy probe FAIL: no active encounter"); yield break; }
        var walkFrames = new HashSet<Mesh>();
        var data = new HashSet<EnemyData>();
        bool charged = false, rushed = false, killed = false, foreignEnemy = false;
        void OnDeath(Enemy enemy, DamageContainer _) { if (enemy.enemyData is OpenBonkMapEnemyData) killed = true; }
        Enemy.A_EnemyDied += OnDeath;
        try
        {
            deadline = Time.realtimeSinceStartup + 18;
            while (Time.realtimeSinceStartup < deadline && !(killed && walkFrames.Count > 1) &&
                GameManager.Instance.IsPlaying())
            {
                foreach (var enemy in manager.enemies.Values)
                {
                    if (enemy == null) continue;
                    if (!(enemy.enemyData is OpenBonkMapEnemyData)) { foreignEnemy = true; continue; }
                    data.Add(enemy.enemyData);
                    var mesh = enemy.animatedMesh.GetComponent<MeshFilter>().sharedMesh;
                    if (mesh == null) continue;
                    if (mesh.name.Contains("Walk")) walkFrames.Add(mesh);
                    if (mesh.name.Contains("Charge") && !enemy.IsTeleporting())
                    {
                        charged = true;
                        Vector3 velocity = enemy.enemyMovement.rb.velocity;
                        if (new Vector2(velocity.x, velocity.z).magnitude > 8f) rushed = true;
                    }
                }
                yield return null;
            }
        }
        finally { Enemy.A_EnemyDied -= OnDeath; }
        bool passed = !foreignEnemy && data.Count > 0 && walkFrames.Count > 1 && killed;
        Debug.Log($"Custom enemy probe {(passed ? "PASS" : "FAIL")}: data={data.Count}, walkFrames={walkFrames.Count}, charge={charged}, rush={rushed}, killed={killed}, inherited={foreignEnemy}");
        if (!passed) yield break;

        var encounter = OpenBonkExternalMapData.GetEncounters(MapController.currentMap, MapController.currentStage);
        var timeline = MapController.currentStage.stageTimeline;
        if (encounter.reserve.Count > 0)
        {
            MyTime.stageTimer = timeline.checkNewEnemyInterval + .5f;
            deadline = Time.realtimeSinceStartup + 14;
            bool reinforcement = false;
            while (!reinforcement && Time.realtimeSinceStartup < deadline && GameManager.Instance.IsPlaying())
            {
                foreach (var enemy in manager.enemies.Values)
                    if (enemy != null && encounter.reserve.Contains(enemy.enemyData.enemyName))
                    {
                        reinforcement = true;
                        break;
                    }
                yield return null;
            }
            Debug.Log($"Custom reinforcement probe {(reinforcement ? "PASS" : "FAIL")}");
        }

        if (timeline.boss is OpenBonkMapEnemyData && GameManager.Instance.IsPlaying())
        {
            var portal = FindAnyObjectByType<InteractableBossSpawner>();
            if (portal == null) { Debug.LogError("Custom boss probe FAIL: no boss portal"); yield break; }
            manager.enabledWaves = false;
            foreach (var enemy in new List<Enemy>(manager.enemies.Values))
                if (enemy != null && enemy.gameObject.activeInHierarchy) enemy.ReleaseToPool();
            Vector3 position = portal.transform.position + Vector3.back * 10f;
            MyPlayer.Instance.playerMovement.TeleportPlayerBackToBounds(position);
            Physics.SyncTransforms();
            if (!portal.Interact()) { Debug.LogError("Custom boss probe FAIL: portal did not summon"); yield break; }
            Enemy boss = null;
            deadline = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < deadline)
            {
                foreach (var enemy in manager.enemies.Values)
                    if (enemy != null && enemy.IsStageBoss() && enemy.enemyData == timeline.boss) { boss = enemy; break; }
                RecordCharge(boss, ref charged, ref rushed);
                yield return null;
            }
            bool hud = false;
            foreach (var row in FindObjectsOfType<TargetOfInterestPrefab>())
                if (row.t_name != null && row.t_name.text == timeline.boss.GetName() && row.hpBar != null)
                    hud = true;
            var mesh = boss != null ? boss.animatedMesh.GetComponent<MeshFilter>().sharedMesh : null;
            bool floor = boss != null && Physics.Raycast(boss.transform.position + Vector3.up * 2f,
                Vector3.down, 12f, GameManager.Instance.whatIsGround, QueryTriggerInteraction.Ignore);
            bool bossPassed = mesh != null && hud && floor;
            Debug.Log($"Custom boss probe {(bossPassed ? "PASS" : "FAIL")}: model={mesh?.name}, hud={hud}, floor={floor}, hp={boss?.hp:F1}/{boss?.maxHp:F1}");
            if (boss != null && boss.enemyData is OpenBonkMapEnemyData enemySpec && enemySpec.charges)
            {
                deadline = Time.realtimeSinceStartup + 10f;
                while (Time.realtimeSinceStartup < deadline && boss.hp > 0f && GameManager.Instance.IsPlaying())
                {
                    RecordCharge(boss, ref charged, ref rushed);
                    if (charged && rushed) break;
                    yield return null;
                }
                Debug.Log($"Custom charge probe {(charged && rushed ? "PASS" : "FAIL")}: charge={charged}, rush={rushed}");
            }
        }
    }

    private static void RecordCharge(Enemy enemy, ref bool charged, ref bool rushed)
    {
        if (enemy == null) return;
        var mesh = enemy.animatedMesh.GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null || !mesh.name.Contains("Charge") || enemy.IsTeleporting()) return;
        charged = true;
        Vector3 velocity = enemy.enemyMovement.rb.velocity;
        if (new Vector2(velocity.x, velocity.z).magnitude > 8f) rushed = true;
    }
}
#endif
