using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class DesertGraveChainPlayCheck {
    const string Pending = "DesertGraveChainPlayCheck.pending";
    static DesertGraveChainPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("grave test runner").AddComponent<DesertGraveChainRunner>().Begin(); } }
}
public class DesertGraveChainRunner : MonoBehaviour {
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string msg) { if (!ok) throw new Exception("DesertGraveChainPlayCheck: " + msg); }
    static void Set(object o, string n, object v) => o.GetType().GetField(n, P).SetValue(o, v);
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run() {
        try {
            var prefabs = new GameObject[5]; for (int i = 0; i < 4; i++) prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/DesertGrave" + (i+1) + ".prefab");
            prefabs[4] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/SkeletonKingStatue.prefab");
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Enemy1.prefab"); var spawnFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/EnemySpawnFx.prefab");
            var achievement = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_graveyard.asset");
            Check(Array.TrueForAll(prefabs, x => x) && enemyPrefab && spawnFx && achievement, "original assets");
            var root = new GameObject("managers"); root.SetActive(false);
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var enemies = new Dictionary<EEnemy,EnemyData>();
            for (int i = 0; i < 4; i++) { var e = prefabs[i].GetComponent<InteractableDesertGrave>().enemyData; enemies[e.enemyName] = e; }
            var finalData = prefabs[4].GetComponent<InteractableSkeletonKingStatue>().enemyData; enemies[finalData.enemyName] = finalData;
            Set(data, "enemyData", enemies); Set(data, "achievementsData", new Dictionary<string,MyAchievement> { ["a_graveyard"] = achievement });
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
            var manager = root.AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
            var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.enemySpawnFxPool = new ObjectPool<GameObject>(() => Instantiate(spawnFx), o => o.SetActive(true), o => o.SetActive(false), Destroy, true, 10, 20);
            pool.enemyPool = new ObjectPool<GameObject>(() => Instantiate(enemyPrefab), o => o.SetActive(true), o => o.SetActive(false), Destroy, true, 10, 20);
            var playerObj = new GameObject("player"); playerObj.SetActive(false); var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var movement = playerObj.AddComponent<PlayerMovement>(); var body = playerObj.AddComponent<Rigidbody>(); body.position = new Vector3(0,2,0); Set(movement, "rb", body); PlayerMovement.Instance = movement; player.playerMovement = movement; game.player = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.statusEffects = new PlayerStatusEffects();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>(); foreach (EStat s in Enum.GetValues(typeof(EStat))) stats.stats[s] = 0f; stats.stats[EStat.EnemyScalingMultiplier] = stats.stats[EStat.EnemyHpMultiplier] = stats.stats[EStat.EnemySizeMultiplier] = 1f; player.inventory.playerStats = stats;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = Vector3.down; ground.transform.localScale = new Vector3(200,2,200); game.whatIsGroundAndObjects = 1 << ground.layer;
            var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapDesert.asset"); Check(map && map.stages.Length > 0, "original Desert map");
            MapInfo.mapCenter = Vector3.zero; MapInfo.mapSize = new Vector3(100,20,100); MapInfo.mapBoundsLower = new Vector3(-150,-20,-150); MapInfo.mapBoundsUpper = new Vector3(150,50,150); Physics.SyncTransforms(); MyTime.time = 10f;
            typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map }); MapController.runConfig = new RunConfig { mapData = map, stageData = map.stages[0], mapTierIndex = 0 };
            var effect = root.AddComponent<EffectManager>(); effect.desertGraves = prefabs; typeof(EffectManager).GetMethod("Awake", P).Invoke(effect, null);
            CharacterMenu.selectedCharacter = ECharacter.Fox; MapGenerationController.A_GenerationComplete?.Invoke(); Check(FindObjectsOfType<InteractableDesertGrave>(true).Length == 0, "other character excluded");
            CharacterMenu.selectedCharacter = ECharacter.Calcium; MapGenerationController.A_GenerationComplete?.Invoke(); Check(FindObjectsOfType<InteractableDesertGrave>(true).Length == 0, "tier zero excluded");
            MapController.runConfig.mapTierIndex = 1; MapGenerationController.A_GenerationComplete?.Invoke();
            var graves = FindObjectsOfType<InteractableDesertGrave>(true); var statue = FindObjectOfType<InteractableSkeletonKingStatue>(true); Check(graves.Length == 4 && statue, "four graves and statue spawned");
            Array.Sort(graves, (a,b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));
            Check(graves[0].gameObject.activeSelf && !graves[1].gameObject.activeSelf && !statue.gameObject.activeSelf, "only first grave active");
            for (int i = 0; i < 4; i++) {
                var grave = graves[i]; Check(grave.gameObject.activeSelf && grave.nextShrine != null, "linked grave " + i);
                var target = grave.nextShrine; Vector3 destination = target.transform.position;
                Check(grave.Interact() && !grave.Interact(), "grave interaction once"); grave.CancelInvoke(); typeof(InteractableDesertGrave).GetMethod("SpawnEnemy", P).Invoke(grave, null);
                var enemy = (Enemy)typeof(InteractableDesertGrave).GetField("myEnemy", P).GetValue(grave); Check(enemy && enemy.enemyData == grave.enemyData && enemy.IsBoss() && Mathf.Abs(enemy.extraKnockbackRes - 3f) < .001f, "grave boss spawn");
                Enemy.A_EnemyDied?.Invoke(enemy, null); Check(target.gameObject.activeSelf && Mathf.Abs(target.transform.position.y - (destination.y - target.offset)) < .01f, "next grave rises");
                MyTime.fixedDeltaTime = 3f; typeof(ShrineSpawnAnimation).GetMethod("FixedUpdate", P).Invoke(target, null); Check(Vector3.Distance(target.transform.position, destination) < .01f, "reveal finishes at original position");
            }
            Check(statue.Interact() && !statue.Interact(), "statue interaction once"); statue.CancelInvoke(); typeof(InteractableSkeletonKingStatue).GetMethod("SpawnEnemy", P).Invoke(statue, null);
            var king = (Enemy)typeof(InteractableSkeletonKingStatue).GetField("myEnemy", P).GetValue(statue); Check(king && king.enemyData == finalData && king.IsBoss(), "Skeleton King spawned");
            Check(!MyAchievements.IsAchievementDone("a_graveyard"), "achievement remains locked before boss death"); Enemy.A_EnemyDied?.Invoke(king, null); Check(MyAchievements.IsAchievementDone("a_graveyard"), "Skeleton King unlocks Graveyard");
            Debug.Log("DesertGraveChainPlayCheck passed: gates, original graves, boss sequence, reveals and Graveyard unlock"); EditorApplication.Exit(0);
        } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        yield break;
    }
}
