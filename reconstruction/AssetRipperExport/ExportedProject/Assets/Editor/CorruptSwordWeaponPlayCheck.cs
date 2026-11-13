using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CorruptSwordWeaponPlayCheck
{
    const string Pending = "CorruptSwordWeaponPlayCheck.pending";
    static CorruptSwordWeaponPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("katana test runner").AddComponent<CorruptSwordWeaponPlayRunner>().Begin(); } }
}
public class CorruptSwordWeaponPlayRunner : MonoBehaviour
{
    ProjectileBase shot; string error;
    static void Check(bool b, string s) { if (!b) throw new Exception("CorruptSwordWeaponPlayCheck: " + s); }
    void Capture(string m, string s, LogType t) { if (t == LogType.Exception || t == LogType.Error || t == LogType.Assert) error = m + "\n" + s; }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    void Update() { MyTime.deltaTime = Time.deltaTime; }
    void FixedUpdate() { MyTime.fixedDeltaTime = Time.fixedDeltaTime; MyTime.time += Time.fixedDeltaTime; }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture; var root = new GameObject("test state"); root.SetActive(false);
        var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies; var save = root.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1; save.config.cfVisualsSettings.damage_numbers = 0;
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerRenderer = root.AddComponent<PlayerRenderer>(); player.height = 0f; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory(); player.inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); player.inventory.playerHealth.hp = 100; player.inventory.playerHealth.maxHp = 100;
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f; player.inventory.playerStats = stats;
        player.playerMovement = root.AddComponent<PlayerMovement>(); player.playerMovement.playerCam = new GameObject("camera direction").transform;
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool; EffectManager.Instance = root.AddComponent<EffectManager>();
        var target = new GameObject("stationary enemy"); target.SetActive(false); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = new Vector3(0f, .6f, 3f);
        var collider = target.AddComponent<CapsuleCollider>(); collider.radius = .8f; collider.height = 2f; var enemy = target.AddComponent<Enemy>(); enemy.enabled = false; enemy.collider = collider; enemy.renderer = target.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); enemies.collidersToEnemies.Add(collider, enemy); target.SetActive(true);
        Enemy nearby = CreateEnemy(enemies, new Vector3(0f, .6f, -8f));
        Enemy distant = CreateEnemy(enemies, new Vector3(0f, .6f, 20f));
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponCorruptedSword.asset"); Check(data != null, "original data"); data.Init(); Check(data.attack != null && data.attack.GetComponent<WeaponAttack>().prefabProjectile != null, "original attack references");
        var weapon = new WeaponBase(data); WeaponAttack.A_SpawnedProjectileSuccessfully += OnSpawn; MyTime.time = 10f; MyTime.paused = false; Physics.SyncTransforms(); WeaponUtility.WeaponAttack(weapon);
        float deadline = Time.realtimeSinceStartup + 5f; while (Time.realtimeSinceStartup < deadline && (shot == null || nearby.hp >= 1000f || pool.projectilePools[data.eWeapon].CountInactive == 0)) yield return null;
        Check(shot != null && enemy.hp < 1000f, "actual attack spawned projectile and damaged target");
        Debug.Log($"corrupt sword diagnostic: hp={enemy.hp},{nearby.hp},{distant.hp}, shotPos={shot.transform.position}");
        Check(Mathf.Abs(enemy.hp - 995.2f) < .01f && Mathf.Abs(nearby.hp - 997.6f) < .01f && distant.hp == 1000f, "melee full damage, projectile half damage and distant exclusion without duplicate hits");
        Check(pool.projectilePools[data.eWeapon].CountInactive > 0, "projectile returned to pool"); Check(error == null, "unexpected error: " + error);
        var sword = (ProjectileCringeSword)shot; var hitSet = (HashSet<Collider>)typeof(ProjectileCringeSword).GetField("hitEnemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sword);
        player.inventory.playerHealth.hp = 50; float beforeHalf = enemy.hp; hitSet.Clear(); sword.CheckZone(weapon, sword.projectileRadius); Check(Mathf.Abs((beforeHalf - enemy.hp) - 10.35f) < .02f, "half health multiplier");
        player.inventory.playerHealth.hp = 1; float beforeOne = enemy.hp; hitSet.Clear(); sword.CheckZone(weapon, sword.projectileRadius); Check(Mathf.Abs((beforeOne - enemy.hp) - 36f) < .02f, "one health multiplier");
        WeaponAttack.A_SpawnedProjectileSuccessfully -= OnSpawn; Application.logMessageReceived -= Capture; Debug.Log("CorruptSwordWeaponPlayCheck passed: original Corrupt Sword forward melee, backward projectile, health scaling and pool return"); EditorApplication.Exit(0);
    }
    static Enemy CreateEnemy(EnemyManager enemies, Vector3 position)
    {
        var obj = new GameObject("additional enemy"); obj.SetActive(false); obj.layer = LayerMask.NameToLayer("Enemy"); obj.transform.position = position;
        var collider = obj.AddComponent<CapsuleCollider>(); collider.radius = .5f; collider.height = 2f;
        var enemy = obj.AddComponent<Enemy>(); enemy.enabled = false; enemy.collider = collider; enemy.renderer = obj.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f);
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemies.collidersToEnemies.Add(collider, enemy); obj.SetActive(true); return enemy;
    }
    void OnSpawn(ProjectileBase p) { shot = p; }
}
