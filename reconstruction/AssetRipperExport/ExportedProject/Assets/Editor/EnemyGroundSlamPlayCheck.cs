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
public static class EnemyGroundSlamPlayCheck
{
    const string Pending = "EnemyGroundSlamPlayCheck.pending";
    static EnemyGroundSlamPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("slam runner").AddComponent<EnemyGroundSlamRunner>().Begin(); } }
}
public class EnemyGroundSlamRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    string error;
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = message + "\n" + stack; }
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("EnemyGroundSlamPlayCheck: " + reason); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture;
        var state = new GameObject("state"); state.SetActive(false); var game = state.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsPlayer = 1 << 0;
        var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool; EffectManager.Instance = state.AddComponent<EffectManager>();
        pool.warningSpherePool = new ObjectPool<GameObject>(() => { var go = new GameObject("warning"); var warning = go.AddComponent<CircleWarning>(); warning.filler = new GameObject("fill").transform; warning.filler.SetParent(go.transform); return go; }, go => go.SetActive(true), go => go.SetActive(false));
        var playerObject = new GameObject("player"); playerObject.SetActive(false); var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player; playerObject.transform.position = Vector3.forward * 10;
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects();
        inv.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); inv.playerStats.stats = new Dictionary<EStat, float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) inv.playerStats.stats[stat] = 0; inv.playerStats.stats[EStat.EnemyDamageMultiplier] = 1; inv.playerStats.stats[EStat.EnemyScalingMultiplier] = 1; inv.playerStats.stats[EStat.DamageCooldownMultiplier] = 1;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = health.maxHp = 1000; inv.playerHealth = health; int hits = 0; Vector3 direction = Vector3.zero; float knockback = 0;
        PlayerHealth.A_TakeDamage += (_, dc, __) => { hits++; direction = dc.direction; knockback = dc.knockback; };
        var hitCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere); hitCollider.transform.position = playerObject.transform.position; Physics.SyncTransforms();
        MapController.runConfig = new RunConfig { mapData = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset") };
        var original = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Miniboss_Pig.asset"); var source = original.specialAttacks.First(a => a.attackName == "SphereSlam");
        var config = new EnemySpecialAttack { isEnabled = true, attackName = "slam fixture", attackPrefab = source.attackPrefab, attackRadius = source.attackRadius, attackChargeTime = .2f, damageMultiplier = 1f, triggerDistance = 100, initialCooldown = 3f, attackCooldown = 10f, endLag = 1f };
        var enemyObject = new GameObject("pig"); enemyObject.SetActive(false); var enemy = enemyObject.AddComponent<Enemy>(); enemy.renderer = enemyObject.AddComponent<MeshRenderer>(); enemy.enemyMovement = enemyObject.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = enemyObject.AddComponent<Rigidbody>();
        var data = Instantiate(original); data.specialAttacks = new[] { config }; typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
        MyTime.time = 0; MyTime.deltaTime = .1f; MyTime.difficultyTimer = 0; MyTime.finalSwarmTimer = 0;
        var controller = new SpecialAttackController(enemy);
        var nextCheck = typeof(SpecialAttackController).GetField("nextCheckTime", Private);
        var attacking = typeof(SpecialAttackController).GetField("isAttacking", Private);
        var cooldowns = (Dictionary<EnemySpecialAttack, float>)typeof(SpecialAttackController).GetField("cooldowns", Private).GetValue(controller);
        nextCheck.SetValue(controller, 1f); controller.Tick();
        Check(!(bool)attacking.GetValue(controller), "future scheduler deadline blocks attack");
        nextCheck.SetValue(controller, 0f); controller.Tick();
        Check(!(bool)attacking.GetValue(controller), "initial cooldown blocks attack");
        MyTime.time = 3f; enemy.enemyMovement.distanceToTarget = 100.01f; nextCheck.SetValue(controller, 0f); controller.Tick();
        Check(!(bool)attacking.GetValue(controller), "out-of-range target blocks attack at cooldown deadline");
        enemy.enemyMovement.distanceToTarget = 100f; nextCheck.SetValue(controller, 0f); controller.Tick();
        Check((bool)attacking.GetValue(controller) && Mathf.Approximately(cooldowns[config], 13f), "range and initial cooldown boundaries trigger attack and set cooldown");
        var slam = FindAnyObjectByType<EnemyGroundSlam>(); Check(slam != null && enemy.state == EEnemyState.Idle, "scheduler selects original slam and stops enemy");
        Check(Vector3.Distance(slam.transform.position, Vector3.forward * 9) < .001f && !slam.hitEffect.activeSelf && Mathf.Abs(slam.hitEffect.transform.localScale.x - 9f) < .001f, "original prefab positions and scales before warning");
        for (int i = 0; i < 10 && hits == 0; i++) { MyTime.deltaTime = .1f; yield return null; }
        Check(hits == 1 && health.hp < 1000 && Mathf.Abs(knockback - 20f) < .001f && direction == Vector3.forward && slam.hitEffect.activeSelf, "warning completion damages player and shows effect");
        yield return new WaitForSecondsRealtime(.75f);
        Check(pool.enemyAttacksPools[config.attackName].CountActive == 0 && pool.warningSpherePool.CountActive == 0, "slam and warning return to pools");
        playerObject.transform.position = Vector3.forward * 25; hitCollider.transform.position = playerObject.transform.position; Physics.SyncTransforms();
        MyTime.time = 5f; controller.Tick();
        ((Dictionary<EnemySpecialAttack, float>)typeof(SpecialAttackController).GetField("cooldowns", Private).GetValue(controller))[config] = 0;
        controller.Tick();
        Check(pool.enemyAttacksPools[config.attackName].CountActive == 1, "scheduler reuses slam for second strike");
        for (int i = 0; i < 10; i++) { MyTime.deltaTime = .1f; yield return null; }
        Check(hits == 1, "out-of-range player is not damaged");
        yield return new WaitForSecondsRealtime(.75f);
        Check(pool.enemyAttacksPools[config.attackName].CountActive == 0, "second slam returns to pool");
        Check(error == null, "unexpected Unity error: " + error);
        Debug.Log("EnemyGroundSlamPlayCheck passed: original miniboss prefab, scheduler, warning, in/out of range player overlap and pool reuse"); EditorApplication.Exit(0);
    }
}
