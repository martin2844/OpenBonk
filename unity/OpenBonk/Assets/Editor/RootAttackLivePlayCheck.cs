using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class RootAttackLivePlayCheck
{
    const string Pending = "RootAttackLivePlayCheck.pending";
    static RootAttackLivePlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("root attack live check").AddComponent<RootAttackLiveRunner>().StartCheck(); } }
}
public class RootAttackLiveRunner : MonoBehaviour
{
    string failure;
    void Log(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) failure = message + "\n" + stack; }
    public void StartCheck() { Application.logMessageReceived += Log; StartCoroutine(Check()); }
    static void Require(bool condition, string message) { if (!condition) throw new Exception("RootAttackLivePlayCheck: " + message); }
    IEnumerator Check()
    {
        PlayerHealth health = null; SpecialAttackController controller = null; EnemySpecialAttack config = null; int hitEvents = 0; Vector3 hitDirection = Vector3.zero; int createdWarnings = 0;
        try
        {
            var managerObj = new GameObject("test managers"); managerObj.SetActive(false);
            GameManager.Instance = managerObj.AddComponent<GameManager>(); GameManager.Instance.whatIsPlayer = 1 << 0; GameManager.Instance.whatIsObjects = 0;
            PoolManager.Instance = managerObj.AddComponent<PoolManager>(); EffectManager.Instance = managerObj.AddComponent<EffectManager>();
            PoolManager.Instance.warningSpherePool = new ObjectPool<GameObject>(() => { createdWarnings++; var go = new GameObject("warning"); var warning = go.AddComponent<CircleWarning>(); warning.filler = new GameObject("fill").transform; warning.filler.SetParent(go.transform); return go; }, go => go.SetActive(true), go => go.SetActive(false));
            var playerObj = new GameObject("test player"); playerObj.SetActive(false); var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = Vector3.forward * 20;
            var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); foreach (EStat s in Enum.GetValues(typeof(EStat))) stats.stats[s] = 0; stats.stats[EStat.EnemyDamageMultiplier] = 1; stats.stats[EStat.EnemyScalingMultiplier] = 1; stats.stats[EStat.DamageCooldownMultiplier] = 1; inv.playerStats = stats;
            health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = health.maxHp = 1000; inv.playerHealth = health;
            PlayerHealth.A_TakeDamage += (_, dc, __) => { hitEvents++; hitDirection = dc.direction; };
            var playerCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere); playerCollider.name = "player hit collider"; playerCollider.transform.position = Vector3.forward * 4; Physics.SyncTransforms();
            MapController.runConfig = new RunConfig { mapData = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset") };
            var enemyObj = new GameObject("test ent"); enemyObj.SetActive(false); var enemy = enemyObj.AddComponent<Enemy>(); enemy.renderer = enemyObj.AddComponent<MeshRenderer>(); enemy.enemyMovement = enemyObj.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = enemyObj.AddComponent<Rigidbody>();
            var original = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/EntStage1.asset"); var originalAttack = original.specialAttacks.First(x => x.attackName == "RootAttackLines3");
            config = new EnemySpecialAttack { isEnabled = true, attackName = "root fixture", attackPrefab = originalAttack.attackPrefab, attackRadius = originalAttack.attackRadius, attackChargeTime = .1f, damageMultiplier = 1f, triggerDistance = 100, attackCooldown = 10f, endLag = 1f };
            var enemyData = Instantiate(original); enemyData.specialAttacks = new[] { config }; typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { enemyData });
            MyTime.time = 0; MyTime.deltaTime = .5f; MyTime.difficultyTimer = 0; MyTime.finalSwarmTimer = 0;
            controller = new SpecialAttackController(enemy); typeof(SpecialAttackController).GetField("nextCheckTime", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, 0f);
            controller.Tick();
            Require((bool)typeof(SpecialAttackController).GetField("isAttacking", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller), "scheduler selected root attack");
            Require(enemy.state == EEnemyState.Idle && PoolManager.Instance.enemyAttacksPools[config.attackName].CountActive == 1, "enemy stopped and attack checked out");
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        for (int i = 0; i < 20; i++) { MyTime.deltaTime = .5f; yield return null; if (failure != null) break; }
        try
        {
            Require(failure == null, "runtime exception: " + failure);
            Require(createdWarnings >= 3, "live coroutine produced warnings");
            Require(hitEvents > 0 && health.hp < 1000 && hitDirection == Vector3.up, "warning completion damaged player with upward knockback");
            Require(PoolManager.Instance.enemyAttacksPools[config.attackName].CountActive == 0, "attack returned to pool after all hits");
            Debug.Log("RootAttackLivePlayCheck passed: scheduler, live warnings, physical hit, player damage and complete pool return");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
