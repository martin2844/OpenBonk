using System;
using System.Collections;
using System.Collections.Generic;
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
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class CharacterEncounterPlayCheck {
    const string Pending = "CharacterEncounterPlayCheck.pending";
    static CharacterEncounterPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("encounter runner").AddComponent<CharacterEncounterRunner>().Begin(); } }
}
public class CharacterEncounterRunner : MonoBehaviour {
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool c, string m) { if (!c) throw new Exception("CharacterEncounterPlayCheck: " + m); }
    static void Set(object o, string n, object v) => o.GetType().GetField(n, Private).SetValue(o, v);
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run() {
        try {
            var bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/QuestGeorgeBush.prefab");
            var banditPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BanditStatue.prefab");
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Enemy1.prefab");
            var enemyFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/EnemySpawnFx.prefab");
            var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
            Check(bushPrefab && banditPrefab && enemyPrefab && enemyFx && map, "original assets");
            var template = bushPrefab.GetComponentInChildren<InteractableCharacterFight>(true);
            Check(template && template.character && template.enemyData && template.character.achievementRequirement, "original Bush fight bindings");
            var managers = new GameObject("managers"); managers.SetActive(false);
            var save = managers.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = managers.AddComponent<DataManager>(); DataManager.Instance = data;
            Set(data, "enemyData", new Dictionary<EEnemy,EnemyData> { [template.enemyData.enemyName] = template.enemyData });
            var achievement = template.character.achievementRequirement;
            Set(data, "achievementsData", new Dictionary<string,MyAchievement> { [achievement.internalName] = achievement });
            var game = managers.AddComponent<GameManager>(); GameManager.Instance = game;
            var enemyManager = managers.AddComponent<EnemyManager>(); EnemyManager.Instance = enemyManager;
            var pool = managers.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.enemySpawnFxPool = new ObjectPool<GameObject>(() => Instantiate(enemyFx), o => o.SetActive(true), o => o.SetActive(false), Destroy, true, 2, 10);
            pool.enemyPool = new ObjectPool<GameObject>(() => Instantiate(enemyPrefab), o => o.SetActive(true), o => o.SetActive(false), Destroy, true, 2, 10);
            var playerObj = new GameObject("player"); playerObj.SetActive(false); var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var movement = playerObj.AddComponent<PlayerMovement>(); var body = playerObj.AddComponent<Rigidbody>(); body.position = new Vector3(0,2,0); Set(movement, "rb", body); PlayerMovement.Instance = movement; player.playerMovement = movement; game.player = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.statusEffects = new PlayerStatusEffects();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>(); foreach (EStat s in Enum.GetValues(typeof(EStat))) stats.stats[s] = 0f; stats.stats[EStat.EnemyScalingMultiplier] = 1f; stats.stats[EStat.EnemyHpMultiplier] = 1f; stats.stats[EStat.EnemySizeMultiplier] = 1f; player.inventory.playerStats = stats;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = Vector3.down; ground.transform.localScale = new Vector3(200,2,200); game.whatIsGroundAndObjects = 1 << ground.layer;
            MapInfo.mapCenter = Vector3.zero; MapInfo.mapSize = new Vector3(100,20,100); MapInfo.mapBoundsLower = new Vector3(-150,-20,-150); MapInfo.mapBoundsUpper = new Vector3(150,50,150); Physics.SyncTransforms();
            var effect = managers.AddComponent<EffectManager>(); effect.bushQuest = bushPrefab; effect.banditQuest = banditPrefab;
            MapController.runConfig = new RunConfig { mapData = map, stageData = map.stages[0] }; typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map });
            typeof(EffectManager).GetMethod("Awake", Private).Invoke(effect, null); MapController.index = 3; MapGenerationController.A_GenerationComplete?.Invoke();
            var fight = FindObjectOfType<InteractableCharacterFight>(); Check(fight && fight.character == template.character, "Forest generates Bush encounter");
            Check(Mathf.Abs(fight.transform.root.position.y) < .05f, "Bush placed on ground");
            var charge = fight.chargeFx; Check(fight.Interact() && !fight.Interact(), "fight starts once"); Check(charge.activeSelf && charge.transform.parent == null, "charge effect released");
            fight.CancelInvoke(); typeof(InteractableCharacterFight).GetMethod("SpawnEnemy", Private).Invoke(fight, null);
            Check(fight.myEnemy && fight.myEnemy.enemyData == template.enemyData && fight.myEnemy.IsBoss(), "original Bush boss spawned");
            Check(Mathf.Abs(fight.myEnemy.teleportTime - .25f) < .001f && !fight.gameObject.activeSelf, "teleport timing and encounter hidden");
            var other = new GameObject("other enemy").AddComponent<Enemy>(); Enemy.A_EnemyDied?.Invoke(other, null); Check(!MyAchievements.IsAchievementDone(achievement.internalName), "unrelated death ignored");
            Enemy.A_EnemyDied?.Invoke(fight.myEnemy, null); Check(MyAchievements.IsAchievementDone(achievement.internalName), "Bush achievement awarded");
            Check(fight.myEnemy.renderer.sharedMaterials.Length == 1, "fight-only material removed");
            var desert = ScriptableObject.CreateInstance<MapData>(); desert.eMap = EMap.Desert; typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { desert }); MapGenerationController.A_GenerationComplete?.Invoke();
            var encounters = FindObjectsOfType<InteractableCharacterFight>(); Check(encounters.Length == 1 && encounters[0].character != template.character, "Desert generates Bandit encounter");
            Debug.Log("CharacterEncounterPlayCheck passed: original Forest and Desert placement, Bush boss and unlock"); EditorApplication.Exit(0);
        } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        yield break;
    }
}
