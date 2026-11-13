using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class GravestoneSpawnPlayCheck
{
    const string Pending = "GravestoneSpawnPlayCheck.pending";
    static GravestoneSpawnPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("gravestone runner").AddComponent<GravestoneSpawnPlayRunner>().Begin();
    }
}

public class GravestoneSpawnPlayRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    public void Begin() { StartCoroutine(Check()); }
    IEnumerator Check()
    {
        try
        {
            var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Enemy1.prefab");
            var effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/EnemySpawnFx.prefab");
            var gravePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Gravestone.prefab");
            if (map == null || enemyPrefab == null || effectPrefab == null || gravePrefab == null) throw new Exception("Original test assets missing");
            var root = new GameObject("managers"); root.SetActive(false);
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsGroundAndObjects = 1;
            var dataManager = root.AddComponent<DataManager>(); DataManager.Instance = dataManager;
            var enemyData = new Dictionary<EEnemy, EnemyData>();
            foreach (var name in new[] { "Ghost_0", "GhostGreater", "GhostPurple_0", "GhostRed_0" })
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/" + name + ".asset");
                if (data == null) throw new Exception("Missing " + name);
                enemyData.Add(data.enemyName, data);
            }
            typeof(DataManager).GetField("enemyData", Private).SetValue(dataManager, enemyData);
            var enemyManager = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemyManager;
            var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.enemySpawnFxPool = new ObjectPool<GameObject>(() => Instantiate(effectPrefab), o => o.SetActive(true), o => o.SetActive(false), Destroy, true, 10, 20);
            pool.enemyPool = new ObjectPool<GameObject>(() => Instantiate(enemyPrefab), o => o.SetActive(true), o => o.SetActive(false), Destroy, true, 10, 20);
            var playerObject = new GameObject("player"); playerObject.SetActive(false);
            var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player; game.player = player;
            var movement = playerObject.AddComponent<PlayerMovement>(); var body = playerObject.AddComponent<Rigidbody>(); body.position = Vector3.up * 2;
            typeof(PlayerMovement).GetField("rb", Private).SetValue(movement, body); PlayerMovement.Instance = movement; player.playerMovement = movement;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            player.inventory.statusEffects = new PlayerStatusEffects(); player.inventory.statInventory = new StatInventory();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 0f;
            stats.stats[EStat.EnemyScalingMultiplier] = 1f; stats.stats[EStat.EnemyHpMultiplier] = 1f; stats.stats[EStat.EnemySizeMultiplier] = 1f;
            player.inventory.playerStats = stats; UiManager.Instance = null;
            MapController.runConfig = new RunConfig { mapData = map, stageData = map.stages[0] };
            typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map });
            MapInfo.mapBoundsLower = new Vector3(-150, -20, -150); MapInfo.mapBoundsUpper = new Vector3(150, 50, 150); MyTime.time = 10;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.position = new Vector3(0, -.5f, 0); ground.transform.localScale = new Vector3(400, 1, 400); Physics.SyncTransforms();
            var graves = new List<InteractableGravestone>();
            for (int i = 0; i < 4; i++) graves.Add(Instantiate(gravePrefab, new Vector3(i * 4, 0, 0), Quaternion.identity).GetComponent<InteractableGravestone>());
            var spawned = new List<EEnemy>(); Action<Enemy> onSpawn = e => spawned.Add(e.enemyData.enemyName); Enemy.A_EnemySpawned += onSpawn;
            try { foreach (var grave in graves) if (!grave.Interact()) throw new Exception("Gravestone interaction failed"); }
            finally { Enemy.A_EnemySpawned -= onSpawn; }
            var expected = new[] { EEnemy.Ghost, EEnemy.GreaterGhost, EEnemy.GhostPurple, EEnemy.GhostRed };
            if (spawned.Count != expected.Length) throw new Exception("Spawn count was " + spawned.Count);
            for (int i = 0; i < expected.Length; i++) if (spawned[i] != expected[i]) throw new Exception("Wrong ghost at " + i + ": " + spawned[i]);
            Debug.Log("GravestoneSpawnPlayCheck passed: four original gravestones spawned original ghost types through the enemy pool");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        yield break;
    }
}
