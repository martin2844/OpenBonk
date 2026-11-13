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
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class FinalGoonsLivePlayCheck
{
    const string Pending = "FinalGoonsLivePlayCheck.pending";
    static FinalGoonsLivePlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return; SessionState.SetBool(Pending, false); new GameObject("goon runner").AddComponent<FinalGoonsLivePlayRunner>().Begin(); }
}
public class FinalGoonsLivePlayRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool c, string m) { if (!c) throw new Exception("FinalGoonsLivePlayCheck: " + m); }
    static void Set(object o, string n, object v) => o.GetType().GetField(n, Private).SetValue(o, v);
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        try {
            var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset"); Check(map != null && map.stages.Length > 0 && map.stages[0].stageTimeline.boss != null, "original map and boss data");
            var data = map.stages[0].stageTimeline.boss; var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Enemy1.prefab"); var effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/EnemySpawnFx.prefab"); Check(enemyPrefab != null && effectPrefab != null, "original prefabs");
            var managers = new GameObject("managers"); managers.SetActive(false); var game = managers.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsGroundAndObjects = 1;
            var dataManager = managers.AddComponent<DataManager>(); DataManager.Instance = dataManager; Set(dataManager, "enemyData", new Dictionary<EEnemy, EnemyData> { [data.enemyName] = data });
            var enemyManager = managers.AddComponent<EnemyManager>(); EnemyManager.Instance = enemyManager;
            var pool = managers.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.enemySpawnFxPool = new ObjectPool<GameObject>(() => Instantiate(effectPrefab), o => o.SetActive(true), o => o.SetActive(false), Destroy, true, 10, 20);
            pool.enemyPool = new ObjectPool<GameObject>(() => Instantiate(enemyPrefab), o => o.SetActive(true), o => o.SetActive(false), Destroy, true, 10, 20);
            var playerObj = new GameObject("player"); playerObj.SetActive(false); var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var movement = playerObj.AddComponent<PlayerMovement>(); var body = playerObj.AddComponent<Rigidbody>(); body.position = new Vector3(0,2,0); Set(movement, "rb", body); PlayerMovement.Instance = movement; player.playerMovement = movement; game.player = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.statusEffects = new PlayerStatusEffects();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>(); foreach(EStat s in Enum.GetValues(typeof(EStat))) stats.stats[s] = 0f; stats.stats[EStat.EnemyScalingMultiplier] = 1f; stats.stats[EStat.EnemyHpMultiplier] = 1f; stats.stats[EStat.EnemySizeMultiplier] = 1f; player.inventory.playerStats = stats;
            MapController.runConfig = new RunConfig { mapData = map, stageData = map.stages[0] }; typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map }); MapInfo.mapBoundsLower = new Vector3(-150,-20,-150); MapInfo.mapBoundsUpper = new Vector3(150,50,150);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.position = new Vector3(0,-.5f,0); ground.transform.localScale = new Vector3(400,1,400); Physics.SyncTransforms(); MyTime.time = 10;
            var bossObj = new GameObject("boss"); bossObj.SetActive(false); var boss = bossObj.AddComponent<Enemy>(); boss.maxHp = 10000;
            var fight = managers.AddComponent<FinalFightController>(); fight.boss = boss; typeof(FinalFightController).GetProperty("currentPhase").GetSetMethod(true).Invoke(fight, new object[] { 2 });
            typeof(FinalFightController).GetMethod("SpawnGoons", Private).Invoke(fight, null);
            var goons = (List<Enemy>)typeof(FinalFightController).GetField("goons", Private).GetValue(fight); Check(goons.Count == 4, "four spawned minions: " + goons.Count);
            foreach (var goon in goons) { Check(goon.enemyData == data && goon.hp == 600 && goon.maxHp == 600, "map boss type and six-percent health"); Check(!(bool)typeof(Enemy).GetField("allowSpecialAttacks", Private).GetValue(goon), "special attacks disabled"); Check(Mathf.Abs((float)typeof(Enemy).GetField("speedMultiplier", Private).GetValue(goon) - .6f) < .001f, "movement multiplier"); }
            Debug.Log("FinalGoonsLivePlayCheck passed: original Forest boss data, pooled enemy prefab and four phase-two minions"); EditorApplication.Exit(0);
        } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        yield break;
    }
}
