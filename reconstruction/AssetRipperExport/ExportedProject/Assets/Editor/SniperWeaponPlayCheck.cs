using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
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
public static class SniperWeaponPlayCheck
{
    const string Pending = "SniperWeaponPlayCheck.pending";
    static SniperWeaponPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("sniper test runner").AddComponent<SniperWeaponPlayRunner>().Begin(); } }
}

public class SniperWeaponPlayRunner : MonoBehaviour
{
    ProjectileBase shot; Enemy enemy; PoolManager pool; WeaponData data; string error;
    static void Check(bool b, string s) { if (!b) throw new Exception("SniperWeaponPlayCheck: " + s); }
    void Capture(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) error = message + "\n" + stack; }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    void Update() { MyTime.deltaTime = Time.deltaTime; }
    void FixedUpdate() { MyTime.fixedDeltaTime = Time.fixedDeltaTime; MyTime.time += Time.fixedDeltaTime; }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture;
        var root = new GameObject("test state"); root.SetActive(false);
        var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy"); gm.whatIsGroundAndObjects = 1 << LayerMask.NameToLayer("Ground");
        var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
        var save = root.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1; save.config.cfVisualsSettings.damage_numbers = 0;
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f; player.inventory.playerStats = stats;
        player.playerMovement = root.AddComponent<PlayerMovement>(); player.playerMovement.playerCam = new GameObject("camera direction").transform;
        pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
        string impactPath = AssetDatabase.GUIDToAssetPath("fc509692caceb454c8a2ba50740680cf"); pool.bulletHit = AssetDatabase.LoadAssetAtPath<GameObject>(impactPath); Check(pool.bulletHit != null, "original bullet impact prefab");
        typeof(PoolManager).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pool, null); Check(pool.bulletHitPool != null, "bullet impact pool initialized");
        EffectManager.Instance = root.AddComponent<EffectManager>();
        var target = new GameObject("stationary enemy"); target.SetActive(false); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = new Vector3(0f, .6f, 4f);
        var collider = target.AddComponent<SphereCollider>(); collider.radius = .8f; enemy = target.AddComponent<Enemy>(); enemy.enabled = false; enemy.renderer = target.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); enemies.collidersToEnemies.Add(collider, enemy); target.SetActive(true);
        var wall = new GameObject("wall"); wall.layer = LayerMask.NameToLayer("Ground"); wall.transform.position = new Vector3(0f, .6f, 8f); wall.AddComponent<BoxCollider>().size = new Vector3(4f, 4f, .2f);
        data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponSniper.asset"); Check(data != null, "original Sniper data"); data.Init(); Check(data.attack != null && data.attack.GetComponent<WeaponAttack>().prefabProjectile != null, "original attack references");
        var weapon = new WeaponBase(data); WeaponAttack.A_SpawnedProjectileSuccessfully += OnSpawn; MyTime.time = 10f; MyTime.paused = false; Physics.SyncTransforms(); WeaponUtility.WeaponAttack(weapon);
        float deadline = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < deadline && (shot == null || enemy.hp >= 1000f || pool.bulletHitPool.CountAll == 0)) yield return null;
        Check(shot != null && enemy.hp < 1000f, "normal attack spawned projectile and damaged enemy"); Check(pool.bulletHitPool.CountAll == 1 && pool.bulletHitPool.CountInactive == 0, "wall impact spawned");
        var effects = UnityEngine.Object.FindObjectsOfType<ParticleSystem>(); bool located = false; foreach (var effect in effects) if (Mathf.Abs(effect.transform.position.z - 7.89f) < .2f) located = true;
        Check(located, "impact is placed at wall");
        deadline = Time.realtimeSinceStartup + 5f; while (Time.realtimeSinceStartup < deadline && (pool.bulletHitPool.CountInactive == 0 || pool.projectilePools[data.eWeapon].CountInactive == 0)) yield return null;
        Check(pool.bulletHitPool.CountInactive == 1, "impact returns to pool"); Check(pool.projectilePools[data.eWeapon].CountInactive > 0, "projectile returns to pool"); Check(error == null, "unexpected error: " + error);
        WeaponAttack.A_SpawnedProjectileSuccessfully -= OnSpawn; Application.logMessageReceived -= Capture; Debug.Log("SniperWeaponPlayCheck passed: actual weapon attack, enemy damage, wall impact and pooled returns"); EditorApplication.Exit(0);
    }
    void OnSpawn(ProjectileBase projectile) { shot = projectile; }
}
