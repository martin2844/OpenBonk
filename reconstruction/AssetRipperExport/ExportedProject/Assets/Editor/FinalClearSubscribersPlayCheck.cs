using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class FinalClearSubscribersPlayCheck {
    const string Pending = "FinalClearSubscribersPlayCheck.pending";
    static FinalClearSubscribersPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("clear runner").AddComponent<FinalClearSubscribersRunner>().Begin(); } }
}
public class FinalClearSubscribersRunner : MonoBehaviour {
    class NoDropRandom : System.Random { public override double NextDouble() => .99; }
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string msg) { if (!ok) throw new Exception("FinalClearSubscribersPlayCheck: " + msg); }
    static Enemy MakeEnemy(GameObject root, EnemyData data, string name) {
        var go = new GameObject(name); go.transform.SetParent(root.transform); go.SetActive(false); go.transform.position = Vector3.up * 2;
        var enemy = go.AddComponent<Enemy>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data }); enemy.hp = 100f;
        var movement = go.AddComponent<EnemyMovementRb>(); movement.rb = go.AddComponent<Rigidbody>(); enemy.enemyMovement = movement;
        var dissolve = go.AddComponent<EnemyDissolve>(); dissolve.enemyRenderer = go.AddComponent<MeshRenderer>(); dissolve.Reset(); enemy.dissolve = dissolve; return enemy;
    }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run() {
        try {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = Vector3.down * .5f; ground.transform.localScale = new Vector3(100,1,100); Physics.SyncTransforms();
            var state = new GameObject("state"); state.SetActive(false);
            var save = state.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var dataManager = state.AddComponent<DataManager>(); DataManager.Instance = dataManager;
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsGround = 1 << ground.layer;
            var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = Vector3.right * 20;
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory; inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.playerXp = new PlayerXp();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f; stats.stats[EStat.PowerupChance] = 0; inventory.playerStats = stats;
            var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            var xp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/XpPickup.prefab"); var money = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/MoneyFlying.prefab"); Check(xp && money, "original pickups");
            pool.xpPool = new ObjectPool<GameObject>(() => Instantiate(xp), o => o.SetActive(true), o => o.SetActive(false)); pool.goldPool = new ObjectPool<GameObject>(() => Instantiate(money), o => o.SetActive(true), o => o.SetActive(false));
            var effect = state.AddComponent<EffectManager>(); typeof(EffectManager).GetMethod("Awake", P).Invoke(effect, null);
            var pickup = new GameObject("pickup manager").AddComponent<PickupManager>(); pickup.whatIsPickups = 0;
            var manager = state.AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
            var enemyData = ScriptableObject.CreateInstance<EnemyData>(); enemyData.enemyName = EEnemy.Goblin; enemyData.xp = 2;
            var first = MakeEnemy(state, enemyData, "first"); var second = MakeEnemy(state, enemyData, "second"); manager.enemies.Add(1, first); manager.enemies.Add(2, second);
            global::Utility.MyRandom.random = new NoDropRandom(); MyTime.time = 0f; MoneyUtility.Init(); TrackStats.Init(); AchievementTracker.Init();
            typeof(MapController).GetField("isFinalBossStage", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, true);
            typeof(GameManager).GetMethod("OnStageBossDied", P).Invoke(game, null);
            Check(first.IsDead() && second.IsDead(), "both enemies cleared");
            var xpList = (PickupStackableList)typeof(PickupManager).GetField("xpList", P).GetValue(pickup); Check(xpList.pickupsList.Count == 2, "pickup manager creates XP for both null-source deaths");
            Check(pool.goldPool.CountAll == 2, "money manager creates gold for both deaths");
            Check(MyStats.GetStat("kills") == 2, "stats manager records both deaths");
            AchievementTracker.Cleanup(); TrackStats.Cleanup(); MoneyUtility.Cleanup(); inventory.itemInventory.Cleanup();
            Debug.Log("FinalClearSubscribersPlayCheck passed: final clear through pickup, money, stats and achievement subscribers"); EditorApplication.Exit(0);
        } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        yield break;
    }
}
