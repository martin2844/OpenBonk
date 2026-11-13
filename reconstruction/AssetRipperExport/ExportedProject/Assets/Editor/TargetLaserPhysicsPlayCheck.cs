using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors;
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

[InitializeOnLoad]
public static class TargetLaserPhysicsPlayCheck
{
    const string Pending = "TargetLaserPhysicsPlayCheck.pending";
    static TargetLaserPhysicsPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("laser physics check").AddComponent<TargetLaserPhysicsRunner>().StartCheck(); } }
}
public class TargetLaserPhysicsRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    string failure;
    void Log(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) failure = message + "\n" + stack; }
    public void StartCheck() { Application.logMessageReceived += Log; StartCoroutine(Check()); }
    static void Require(bool v, string m) { if (!v) throw new Exception("TargetLaserPhysicsPlayCheck: " + m); }
    IEnumerator Check()
    {
        try
        {
            var mgr = new GameObject("managers"); mgr.SetActive(false); GameManager.Instance = mgr.AddComponent<GameManager>(); PoolManager.Instance = mgr.AddComponent<PoolManager>();
            int playerLayer = LayerMask.NameToLayer("Player"); GameManager.Instance.whatIsPlayer = 1 << playerLayer; GameManager.Instance.whatIsGroundAndObjects = 0; GameManager.Instance.whatIsGround = 0;
            var pgo = new GameObject("test player"); pgo.SetActive(false); var player = pgo.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = new Vector3(0, 1.6f, 20);
            var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>(); foreach (EStat s in Enum.GetValues(typeof(EStat))) stats.stats[s] = 0; stats.stats[EStat.EnemyDamageMultiplier] = 1; stats.stats[EStat.EnemyScalingMultiplier] = 1; stats.stats[EStat.DamageCooldownMultiplier] = 0; inv.playerStats = stats;
            var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = health.maxHp = 1000; inv.playerHealth = health;
            var colliderObj = new GameObject("physical player"); colliderObj.layer = playerLayer; colliderObj.transform.position = player.transform.position; colliderObj.AddComponent<SphereCollider>();
            MapController.runConfig = new RunConfig { mapData = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset") };
            var ego = new GameObject("enemy"); ego.SetActive(false); var enemy = ego.AddComponent<Enemy>(); enemy.renderer = ego.AddComponent<MeshRenderer>(); enemy.enemyMovement = ego.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = ego.AddComponent<Rigidbody>(); typeof(Enemy).GetProperty("meshHeight").GetSetMethod(true).Invoke(enemy, new object[] { 2f });
            var original = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Pharaoh1.asset"); var source = original.specialAttacks.First(a => a.attackPrefab != null && a.attackPrefab.GetComponent<EnemySpecialAttackTargetLaser>() != null);
            var config = new EnemySpecialAttack { isEnabled = true, attackName = "laser physics fixture", attackPrefab = source.attackPrefab, triggerDistance = 100, duration = 8, damageMultiplier = 1, attackCooldown = 10 };
            var data = Instantiate(original); data.specialAttacks = new[] { config }; typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
            MyTime.time = 1; MyTime.fixedDeltaTime = .02f; MyTime.deltaTime = .02f; MyTime.difficultyTimer = 0; MyTime.finalSwarmTimer = 0;
            var controller = new SpecialAttackController(enemy); typeof(SpecialAttackController).GetField("nextCheckTime", Private).SetValue(controller, 0f); controller.Tick();
            var attack = FindObjectsOfType<EnemySpecialAttackTargetLaser>().Single(); Require(!Physics.GetIgnoreLayerCollision(attack.laser.gameObject.layer, playerLayer), "project collision matrix allows player and laser");
            attack.laser.rotation = Quaternion.identity; typeof(EnemySpecialAttackTargetLaser).GetField("speedTimer", Private).SetValue(attack, 1f); typeof(EnemySpecialAttackTargetLaser).GetField("laserLength", Private).SetValue(attack, 40f);
            typeof(EnemySpecialAttackTargetLaser).GetMethod("FixedUpdate", Private).Invoke(attack, null); Physics.SyncTransforms();
            Require(Mathf.Abs(attack.laser.localScale.z - 20f) < .01f, "beam clips just beyond player collider");
            int hits = 0; Vector3 direction = Vector3.zero; PlayerHealth.A_TakeDamage += (_, dc, __) => { hits++; direction = dc.direction; };
            Physics.simulationMode = SimulationMode.Script; Physics.Simulate(.02f); Physics.Simulate(.02f);
            Require(hits == 1 && health.hp < 1000 && direction == Vector3.up, "real OnTriggerStay damages player once");
            MyTime.time = 1.05f; Physics.Simulate(.02f); Require(hits == 1, "laser cooldown prevents repeated damage");
            MyTime.time = 1.11f; Physics.Simulate(.02f); Require(hits == 2, "laser damages again after cooldown");
            colliderObj.transform.position = new Vector3(30, 1.6f, 20); Physics.SyncTransforms(); MyTime.time = 1.3f; Physics.Simulate(.02f); Physics.Simulate(.02f); Require(hits == 2, "leaving beam stops hits");
            Debug.Log("TargetLaserPhysicsPlayCheck passed: original prefab, Player-layer physics trigger, player clipping, damage cooldown and exit");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        yield break;
    }
}
