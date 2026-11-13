using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Pool;

public static class EnemyTeleportPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("EnemyTeleportPortCheck: " + message); }
    public static void Run()
    {
        var previousPlayer = MyPlayer.Instance; var previousPool = PoolManager.Instance; var previousGame = GameManager.Instance; var previousDelta = MyTime.deltaTime; var previousTime = MyTime.time;
        var player = new GameObject("teleport player"); player.SetActive(false);
        var manager = new GameObject("teleport pool"); manager.SetActive(false);
        var enemyObject = new GameObject("teleport enemy"); enemyObject.SetActive(false);
        var effect = new GameObject("teleport effect"); effect.SetActive(false);
        var gameObject = new GameObject("teleport game"); gameObject.SetActive(false);
        EnemyData data = null;
        PlayerStatusEffects status = null;
        try
        {
            var myPlayer = player.AddComponent<MyPlayer>(); MyPlayer.Instance = myPlayer;
            GameManager.Instance = gameObject.AddComponent<GameManager>();
            myPlayer.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            status = new PlayerStatusEffects(); myPlayer.inventory.statusEffects = status;
            var body = player.AddComponent<Rigidbody>(); player.transform.position = new Vector3(10, 0, 0);
            var pool = manager.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            var particles = effect.AddComponent<EnemySpawnParticles>(); particles.ps = effect.AddComponent<ParticleSystem>(); particles.audio = effect.AddComponent<RandomSfx>();
            pool.enemySpawnFxPool = new ObjectPool<GameObject>(() => effect, null, obj => obj.SetActive(false), null, true, 1, 1);
            var enemy = enemyObject.AddComponent<Enemy>(); var move = enemyObject.AddComponent<EnemyMovementRb>();
            enemy.rb = enemyObject.AddComponent<Rigidbody>(); enemy.collider = enemyObject.AddComponent<CapsuleCollider>();
            enemy.enemyMovement = move; move.enemy = enemy; move.rb = enemy.rb;
            var mesh = new GameObject("mesh"); mesh.transform.SetParent(enemyObject.transform); enemy.renderer = mesh.AddComponent<MeshRenderer>();
            typeof(Enemy).GetField("<target>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, body);
            typeof(Enemy).GetField("<meshHeight>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 4f);
            enemy.teleportTime = 1f; MyTime.deltaTime = .25f;
            var routine = (IEnumerator)typeof(Enemy).GetMethod("StartTeleporting", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(enemy, new object[] { new Vector3(2, 1, 0), false });
            Require(routine.MoveNext() && enemy.state == EEnemyState.Teleporting && !enemy.collider.enabled && enemy.rb.isKinematic, "starts teleport and disables collision/movement");
            Require(mesh.transform.localPosition.y < 0f, "mesh descends");
            for (int i = 0; i < 3; i++) Require(routine.MoveNext(), "descent frames");
            Require(routine.MoveNext() && enemy.transform.position == new Vector3(2, 1, 0), "relocation and emergence");
            Require(effect.activeSelf && pool.enemySpawnFxPool.CountActive == 1, "spawn effect taken from pool");
            for (int i = 0; i < 3; i++) Require(routine.MoveNext(), "emergence frames");
            Require(!routine.MoveNext() && enemy.state == EEnemyState.Default && enemy.collider.enabled && !enemy.rb.isKinematic, "restores collision and movement");
            Require(mesh.transform.localPosition == Vector3.zero, "restores mesh position");
            particles.Release(); Require(pool.enemySpawnFxPool.CountInactive == 1, "spawn effect returns to pool");
            pool.enemyPool = new ObjectPool<GameObject>(() => enemyObject, null, obj => obj.SetActive(false), null, true, 1, 1);
            pool.enemyPool.Get(); enemy.hp = 10f;
            var despawn = (IEnumerator)typeof(Enemy).GetMethod("Despawn", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(enemy, null);
            Require(despawn.MoveNext() && enemy.state == EEnemyState.Teleporting && !enemy.collider.enabled && enemy.rb.isKinematic, "despawn disables collision and movement");
            Require(mesh.transform.localPosition.y < 0f && effect.activeSelf, "despawn lowers mesh and plays effect");
            for (int i = 0; i < 3; i++) Require(despawn.MoveNext(), "despawn frames");
            Require(!despawn.MoveNext() && pool.enemyPool.CountInactive == 1 && !enemyObject.activeSelf, "despawn returns enemy to pool");
            Require(enemy.state == EEnemyState.Default && enemy.collider.enabled && !enemy.rb.isKinematic && mesh.transform.localPosition == Vector3.zero, "despawn restores pooled state");
            particles.Release();
            enemy.hp = 0f;
            var deadDespawn = (IEnumerator)typeof(Enemy).GetMethod("Despawn", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(enemy, null);
            Require(!deadDespawn.MoveNext(), "dead enemies are not despawned again");
            data = ScriptableObject.CreateInstance<EnemyData>(); data.teleportCooldown = 10f;
            typeof(Enemy).GetField("<enemyData>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, data);
            var nextCheck = typeof(Enemy).GetField("nextTeleportTimeCheck", BindingFlags.Instance | BindingFlags.NonPublic);
            var interval = typeof(Enemy).GetField("teleportCheckInterval", BindingFlags.Instance | BindingFlags.NonPublic);
            var distance = typeof(Enemy).GetField("startTeleportThresholdDistance", BindingFlags.Instance | BindingFlags.NonPublic);
            var lastTeleport = typeof(Enemy).GetField("lastTeleportTime", BindingFlags.Instance | BindingFlags.NonPublic);
            var tryTeleport = typeof(Enemy).GetMethod("TryTeleport", BindingFlags.Instance | BindingFlags.NonPublic);
            enemy.hp = 10f; MyTime.time = 20f; interval.SetValue(enemy, 5f); distance.SetValue(enemy, 50f); move.distanceToTarget = 10f; lastTeleport.SetValue(enemy, 0f); nextCheck.SetValue(enemy, 21f);
            tryTeleport.Invoke(enemy, null); Require((float)nextCheck.GetValue(enemy) == 21f, "teleport check waits for interval");
            nextCheck.SetValue(enemy, 20f); tryTeleport.Invoke(enemy, null);
            Require((float)nextCheck.GetValue(enemy) == 25f && (float)lastTeleport.GetValue(enemy) == 0f, "nearby enemy waits and schedules next teleport check");
            move.distanceToTarget = 100f; nextCheck.SetValue(enemy, 20f); lastTeleport.SetValue(enemy, 15f); tryTeleport.Invoke(enemy, null);
            Require((float)nextCheck.GetValue(enemy) == 25f && (float)lastTeleport.GetValue(enemy) == 15f, "teleport cooldown prevents early relocation");
            Debug.Log("EnemyTeleportPortCheck passed: teleport, despawn and distance/cooldown gates");
        }
        finally
        {
            status?.OnDestroy(); MyTime.deltaTime = previousDelta; MyTime.time = previousTime; MyPlayer.Instance = previousPlayer; PoolManager.Instance = previousPool; GameManager.Instance = previousGame;
            UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(gameObject); UnityEngine.Object.DestroyImmediate(effect); UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(manager); UnityEngine.Object.DestroyImmediate(player);
        }
    }
}
