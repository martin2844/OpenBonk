using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class EnemyLifecyclePlayCheck
{
    const string Pending = "EnemyLifecyclePlayCheck.pending";
    static EnemyLifecyclePlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("enemy lifecycle runner").AddComponent<EnemyLifecyclePlayRunner>().Begin();
    }
}
public class EnemyLifecyclePlayRunner : MonoBehaviour
{
    static void Require(bool value, string message) { if (!value) throw new Exception("EnemyLifecyclePlayCheck: " + message); }
    static void SetField(object o, string name, object value) => o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o, value);
    public void Begin() => StartCoroutine(Check());
    IEnumerator Check()
    {
        Enemy enemy = null; PoolManager pool = null; PlayerStatusEffects status = null;
        try
        {
            var game = new GameObject("game"); game.SetActive(false); GameManager.Instance = game.AddComponent<GameManager>(); GameManager.Instance.whatIsGroundAndObjects = 1;
            var playerObject = new GameObject("player"); playerObject.SetActive(false); MyPlayer.Instance = playerObject.AddComponent<MyPlayer>();
            MyPlayer.Instance.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            status = new PlayerStatusEffects(); MyPlayer.Instance.inventory.statusEffects = status;
            var playerBody = playerObject.AddComponent<Rigidbody>(); playerObject.transform.position = new Vector3(0, 2, 0); playerBody.position = playerObject.transform.position;
            var poolObject = new GameObject("pool"); poolObject.SetActive(false); pool = poolObject.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            var effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/EnemySpawnFx.prefab");
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Enemy1.prefab");
            Require(effectPrefab != null && enemyPrefab != null, "original prefabs present");
            pool.enemySpawnFxPool = new ObjectPool<GameObject>(() => Instantiate(effectPrefab), obj => obj.SetActive(true), obj => obj.SetActive(false), Destroy, true, 10, 20);
            pool.enemyPool = new ObjectPool<GameObject>(() => Instantiate(enemyPrefab), obj => obj.SetActive(true), obj => obj.SetActive(false), Destroy, true, 1, 10);
            var enemyObject = pool.enemyPool.Get(); enemy = enemyObject.GetComponent<Enemy>();
            foreach (var component in enemyObject.GetComponentsInChildren<MonoBehaviour>(true)) if (component != enemy && component != enemy.enemyMovement) component.enabled = false;
            Require(enemy.renderer != null && enemy.collider != null && enemy.enemyMovement != null && enemy.rb != null, "original enemy references");
            var data = ScriptableObject.CreateInstance<EnemyData>(); data.teleportCooldown = 1f;
            SetField(enemy, "<enemyData>k__BackingField", data); SetField(enemy, "<target>k__BackingField", playerBody); SetField(enemy, "<meshHeight>k__BackingField", 2f);
            enemy.hp = 10f; enemy.teleportTime = .2f; enemy.state = EEnemyState.Default;
            enemy.transform.position = new Vector3(-20, 2, 0); enemy.rb.position = enemy.transform.position; enemy.rb.useGravity = false; enemy.rb.isKinematic = false;
            enemy.enemyMovement.distanceToTarget = 20f;
            SetField(enemy, "nextTeleportTimeCheck", 0f); SetField(enemy, "teleportCheckInterval", 5f); SetField(enemy, "startTeleportThresholdDistance", 10f); SetField(enemy, "lastTeleportTime", 0f);
            MapInfo.mapBoundsLower = new Vector3(-10, -10, -10); MapInfo.mapBoundsUpper = new Vector3(10, 10, 10);
            var map = ScriptableObject.CreateInstance<MapData>(); map.mapType = EMapType.ProceduralMesh;
            typeof(MapController).GetProperty("currentMap").SetValue(null, map);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "ground"; ground.transform.position = new Vector3(0, -.5f, 0); ground.transform.localScale = new Vector3(100, 1, 100);
            Physics.SyncTransforms(); MyTime.time = 10f; MyTime.deltaTime = .1f;
            Debug.Log($"Enemy lifecycle setup target={playerBody.position}, enemyTransform={enemy.transform.position}, enemyBody={enemy.rb.position}, bounds={MapInfo.mapBoundsLower}..{MapInfo.mapBoundsUpper}");
            typeof(Enemy).GetMethod("TryTeleport", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(enemy, null);
            Require(enemy.state == EEnemyState.Teleporting && !enemy.collider.enabled, "physics hit starts teleport");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        for (int i = 0; i < 8; i++) { MyTime.deltaTime = .1f; yield return null; Debug.Log($"Enemy lifecycle frame {i}: position={enemy.transform.position}, body={enemy.rb.position}, state={enemy.state}"); }
        yield return new WaitForFixedUpdate();
        yield return null;
        try
        {
            Require(Vector3.Distance(enemy.transform.position, new Vector3(9, 1, 0)) < .1f, "bounded reflected destination and ground height: " + enemy.transform.position);
            Require(Vector3.Distance(enemy.rb.position, new Vector3(9, 1, 0)) < .1f, "physics body reaches teleport destination: " + enemy.rb.position);
            Require(enemy.state == EEnemyState.Default && enemy.collider.enabled && !enemy.rb.isKinematic, "emergence completes");
            Require(pool.enemySpawnFxPool.CountActive == 1, "original effect spawned");
            enemy.enemyData.despawnTime = 1f; SetField(enemy, "despawnAtTime", 11f); MyTime.time = 12f;
            typeof(Enemy).GetMethod("TryDespawn", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(enemy, null);
            Require(enemy.state == EEnemyState.Teleporting && !enemy.collider.enabled, "timed despawn starts");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        for (int i = 0; i < 5; i++) { MyTime.deltaTime = .1f; yield return null; }
        try { Require(pool.enemyPool.CountInactive == 1 && !enemy.gameObject.activeSelf, "timed despawn returns original enemy to pool"); status.OnDestroy(); Debug.Log("EnemyLifecyclePlayCheck passed"); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
