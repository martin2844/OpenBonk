using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ChunkersAttackPlayCheck
{
    const string Pending = "ChunkersAttackPlayCheck.pending";
    static ChunkersAttackPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("chunkers check runner").AddComponent<ChunkersAttackPlayRunner>().Begin(); } }
}
public class ChunkersAttackPlayRunner : MonoBehaviour
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    string error;
    void OnLog(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) error = message + "\n" + stack; }
    static void Require(bool ok, string message) { if (!ok) throw new Exception("ChunkersAttackPlayCheck: " + message); }
    public void Begin() { Application.logMessageReceived += OnLog; StartCoroutine(Run()); }
    void Update() { MyTime.deltaTime = Time.deltaTime; }
    void FixedUpdate() { MyTime.fixedDeltaTime = Time.fixedDeltaTime; MyTime.time += Time.fixedDeltaTime; }
    IEnumerator Run()
    {
        Enemy enemy = null; AuraAttacks auras = null; WeaponData data = null; WeaponInventory inventory = null;
        try
        {
            var root = new GameObject("test state"); root.SetActive(false);
            var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
            new GameObject("audio listener").AddComponent<AudioListener>();
            var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.feet = new GameObject("feet").transform; player.width = .5f;
            var renderer = new GameObject("renderer"); renderer.SetActive(false); player.playerRenderer = renderer.AddComponent<PlayerRenderer>();
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
            stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f; player.inventory.playerStats = stats;
            PoolManager.Instance = root.AddComponent<PoolManager>(); EffectManager.Instance = root.AddComponent<EffectManager>();
            var target = new GameObject("test enemy"); target.SetActive(false); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = new Vector3(0f, 2.5f, 3f);
            var collider = target.AddComponent<CapsuleCollider>(); enemy = target.AddComponent<Enemy>(); enemy.enabled = false; enemy.collider = collider; enemy.hp = enemy.maxHp = 1000f;
            enemy.renderer = target.AddComponent<MeshRenderer>(); enemy.rb = target.AddComponent<Rigidbody>(); enemy.rb.isKinematic = true;
            enemy.enemyMovement = target.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enabled = false; enemy.enemyMovement.enemy = enemy; enemy.enemyMovement.rb = enemy.rb;
            typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
            enemies.collidersToEnemies.Add(collider, enemy); target.SetActive(true);
            data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponChunkers.asset"); data.Init();
            var dataManager = root.AddComponent<DataManager>(); DataManager.Instance = dataManager;
            ((Dictionary<EWeapon, WeaponData>)typeof(DataManager).GetField("weapons", P).GetValue(dataManager)).Add(data.eWeapon, data);
            inventory = new WeaponInventory(); player.inventory.weaponInventory = inventory;
            auras = new GameObject("aura controller").AddComponent<AuraAttacks>();
            MyTime.time = 10f; MyTime.paused = false; MyTime.fixedDeltaTime = Time.fixedDeltaTime;
            inventory.AddWeapon(data, null); Physics.SyncTransforms();
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        float end = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < end && enemy.hp >= 1000f && error == null) yield return new WaitForFixedUpdate();
        try
        {
            Require(error == null, "unexpected Unity error: " + error);
            var active = (Dictionary<EWeapon, ConstantAttack>)typeof(AuraAttacks).GetField("auras", P).GetValue(auras);
            Require(active.TryGetValue(data.eWeapon, out var aura) && aura is ChunkersAttack, "inventory event creates original aura");
            var rotating = ((ChunkersAttack)aura).rotatingProjectiles;
            var rocks = (List<ParticleSystem>)typeof(RotatingProjectiles).GetField("prefabs", P).GetValue(rotating);
            Debug.Log($"Chunkers live diagnostic: gameTime={MyTime.time} target={enemy.transform.position} hp={enemy.hp} active={rotating.gameObject.activeInHierarchy} rock={rocks[0].transform.position} rockActive={rocks[0].gameObject.activeInHierarchy} radius={typeof(RotatingProjectiles).GetField("projectileRadius", P).GetValue(rotating)}");
            Require(enemy.hp < 1000f, "live physics detects and damages enemy");
            inventory.ToggleWeapon(data.eWeapon, false); Require(!active.ContainsKey(data.eWeapon), "toggle removes original aura");
            Debug.Log($"ChunkersAttackPlayCheck passed: inventory acquired original aura and live physics dealt {1000f - enemy.hp} damage, then toggle removed it");
            Application.logMessageReceived -= OnLog; EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
