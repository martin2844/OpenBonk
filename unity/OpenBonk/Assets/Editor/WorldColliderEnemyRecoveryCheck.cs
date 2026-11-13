using System;
using System.Collections;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class WorldColliderEnemyRecoveryCheck
{
    const string Pending = "WorldColliderEnemyRecoveryCheck.pending";
    static WorldColliderEnemyRecoveryCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/WorldColliderEnemyTemporary.unity");
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        { SessionState.SetBool(Pending, false); new GameObject("enemy recovery runner").AddComponent<WorldColliderEnemyRecoveryRunner>().Begin(); }
    }
}
public class WorldColliderEnemyRecoveryRunner : MonoBehaviour
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("WorldColliderEnemyRecoveryCheck: " + message); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard()
    {
        var test = Run();
        while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; }
    }
    IEnumerator Run()
    {
        var world = new GameObject("WorldCollider"); world.layer = LayerMask.NameToLayer("Object"); world.transform.position = new Vector3(0, -10, 0);
        world.AddComponent<BoxCollider>().size = new Vector3(20, 1, 20); world.AddComponent<WorldColliderTrigger>();
        var safe = GameObject.CreatePrimitive(PrimitiveType.Cube); safe.layer = LayerMask.NameToLayer("Ground"); safe.transform.position = new Vector3(100, -.5f, 100); safe.transform.localScale = new Vector3(200, 1, 200);
        var managerObject = new GameObject("managers"); managerObject.SetActive(false);
        var manager = managerObject.AddComponent<GameManager>(); GameManager.Instance = manager; manager.whatIsGroundAndObjects = 1 << safe.layer; manager.whatIsGround = 1 << safe.layer; typeof(GameManager).GetProperty("isCrypt").GetSetMethod(true).Invoke(manager, new object[] { true });
        var enemies = managerObject.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
        var pool = managerObject.AddComponent<PoolManager>(); PoolManager.Instance = pool; pool.enemySpawnFxPool = new ObjectPool<GameObject>(() => null);
        typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { ScriptableObject.CreateInstance<MapData>() });
        // The isolated fixture has no game clock advancing MyTime.time. Force the
        // first spawn request to calculate map bounds instead of clamping to zero.
        typeof(Assets.Scripts.Game.Spawning.SpawnPositions).GetField("nextFindBoundsTime", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, -1f);
        var playerObject = new GameObject("player"); playerObject.SetActive(false); playerObject.transform.position = new Vector3(100, 1, 100);
        var player = playerObject.AddComponent<MyPlayer>(); var movement = playerObject.AddComponent<PlayerMovement>(); var playerBody = playerObject.AddComponent<Rigidbody>(); playerBody.isKinematic = true;
        player.playerMovement = movement; typeof(PlayerMovement).GetField("rb", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, playerBody); player.enabled = false; movement.enabled = false;
        playerObject.SetActive(true); playerBody.position = new Vector3(100, 1, 100); PlayerMovement.Instance = movement; MyPlayer.Instance = player;
        var enemyObject = new GameObject("enemy"); enemyObject.SetActive(false); enemyObject.layer = LayerMask.NameToLayer("Enemy"); enemyObject.transform.position = new Vector3(0, -8.5f, 0);
        var capsule = enemyObject.AddComponent<CapsuleCollider>(); var body = enemyObject.AddComponent<Rigidbody>(); body.constraints = RigidbodyConstraints.FreezeRotation;
        var enemy = enemyObject.AddComponent<Enemy>(); var enemyMovement = enemyObject.AddComponent<EnemyMovementRb>(); enemy.collider = capsule; enemy.rb = body; enemy.enemyMovement = enemyMovement; enemy.hp = 10; enemyMovement.enemy = enemy; enemyMovement.rb = body;
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube); visual.transform.SetParent(enemyObject.transform, false); Destroy(visual.GetComponent<Collider>()); enemy.renderer = visual.GetComponent<Renderer>();
        typeof(Enemy).GetProperty("target").GetSetMethod(true).Invoke(enemy, new object[] { playerBody });
        enemies.collidersToEnemies.Add(capsule, enemy); Physics.IgnoreLayerCollision(world.layer, enemyObject.layer, false); enemyObject.SetActive(true); body.position = new Vector3(0, -8.5f, 0); Physics.SyncTransforms();
        Debug.Log($"Enemy recovery setup: player transform={playerObject.transform.position} rb={playerBody.position}, enemy transform={enemyObject.transform.position} rb={body.position}, floor={safe.transform.position}, world={world.transform.position}");
        float deadline = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < deadline && Vector3.Distance(enemyObject.transform.position, playerObject.transform.position) > 50f) yield return new WaitForFixedUpdate();
        Require(Vector3.Distance(enemyObject.transform.position, playerObject.transform.position) < 50f, $"enemy not teleported: position={enemyObject.transform.position}, rb={body.position}, player={playerObject.transform.position}, playerRb={playerBody.position}, capsule={capsule.enabled}, kinematic={body.isKinematic}");
        Require(!capsule.enabled && body.isKinematic, "enemy did not enter teleport state");
        enemy.StopAllCoroutines(); Destroy(enemyObject);
        Debug.Log("WorldColliderEnemyRecoveryCheck passed: real collision resolved through EnemyManager and teleported living enemy near player"); EditorApplication.Exit(0);
    }
}
