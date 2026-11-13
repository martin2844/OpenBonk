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
public static class SwordWeaponPlayCheck
{
    const string Pending = "SwordWeaponPlayCheck.pending";
    static SwordWeaponPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("sword test runner").AddComponent<SwordWeaponPlayRunner>().Begin(); } }
}
public class SwordWeaponPlayRunner : MonoBehaviour
{
    ProjectileBase shot; string error;
    static void Check(bool b, string s) { if (!b) throw new Exception("SwordWeaponPlayCheck: " + s); }
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
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f; player.inventory.playerStats = stats;
        player.playerRenderer = root.AddComponent<PlayerRenderer>(); player.playerMovement = root.AddComponent<PlayerMovement>(); player.playerMovement.playerCam = new GameObject("camera direction").transform;
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool; EffectManager.Instance = root.AddComponent<EffectManager>();
        var target = new GameObject("stationary enemy"); target.SetActive(false); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = new Vector3(0f, .6f, 3f);
        var collider = target.AddComponent<SphereCollider>(); collider.radius = .8f; var enemy = target.AddComponent<Enemy>(); enemy.enabled = false; enemy.renderer = target.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); enemies.collidersToEnemies.Add(collider, enemy); target.SetActive(true);
        var outside = new GameObject("outside enemy"); outside.SetActive(false); outside.layer = LayerMask.NameToLayer("Enemy"); outside.transform.position = new Vector3(0f, .6f, 20f);
        var outsideCollider = outside.AddComponent<SphereCollider>(); outsideCollider.radius = .8f; var outsideEnemy = outside.AddComponent<Enemy>(); outsideEnemy.enabled = false; outsideEnemy.renderer = outside.AddComponent<MeshRenderer>(); outsideEnemy.hp = outsideEnemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(outsideEnemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(outsideEnemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); enemies.collidersToEnemies.Add(outsideCollider, outsideEnemy); outside.SetActive(true);
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponSword.asset"); Check(data != null, "original data"); data.Init(); Check(data.attack != null && data.attack.GetComponent<WeaponAttack>().prefabProjectile != null, "original attack references");
        var weapon = new WeaponBase(data); WeaponAttack.A_SpawnedProjectileSuccessfully += OnSpawn; MyTime.time = 10f; MyTime.paused = false; Physics.SyncTransforms(); WeaponUtility.WeaponAttack(weapon);
        float spawnDeadline = Time.realtimeSinceStartup + 4f;
        while (Time.realtimeSinceStartup < spawnDeadline && shot == null) yield return null;
        Check(shot != null && enemy.hp < 1000f, "actual attack spawned projectile and damaged nearby enemy");
        Check(outsideEnemy.hp == 1000f, "enemy outside radius is unharmed");
        Check(Vector3.Distance(shot.transform.position, new Vector3(0f, .5f, 2.5f)) < .01f, "initial position follows original offset");
        root.transform.position = new Vector3(1f, 0f, 0f); root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        yield return null;
        Check(shot.gameObject.activeInHierarchy && Vector3.Distance(shot.transform.position, new Vector3(3.5f, .5f, 0f)) < .01f, "active sword follows player position and facing");
        string key = data.eWeapon.ToString(); float deadline = Time.realtimeSinceStartup + 4f;
        while (Time.realtimeSinceStartup < deadline && (pool.projectileHitPools == null || !pool.projectileHitPools.ContainsKey(key) || pool.projectilePools[data.eWeapon].CountInactive == 0)) yield return null;
        Check(pool.projectileHitPools != null && pool.projectileHitPools.ContainsKey(key) && pool.projectileHitPools[key].CountAll > 0, "delayed hit effect spawned");
        Check(pool.projectilePools[data.eWeapon].CountInactive > 0, "projectile returned to pool"); Check(error == null, "unexpected error: " + error);
        WeaponAttack.A_SpawnedProjectileSuccessfully -= OnSpawn; Application.logMessageReceived -= Capture; Debug.Log("SwordWeaponPlayCheck passed: actual weapon attack, nearby and distant targets, player following, delayed effect and projectile pool return"); EditorApplication.Exit(0);
    }
    void OnSpawn(ProjectileBase p) { shot = p; }
}
