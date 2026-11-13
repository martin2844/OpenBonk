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
public static class AxeWeaponPlayCheck
{
    const string Pending = "AxeWeaponPlayCheck.pending";
    static AxeWeaponPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("axe test runner").AddComponent<AxeWeaponPlayRunner>().Begin(); } }
}
public class AxeWeaponPlayRunner : MonoBehaviour
{
    ProjectileBase shot; int spawned; string error; readonly List<Vector3> launchDirections = new List<Vector3>();
    static void Check(bool b, string s) { if (!b) throw new Exception("AxeWeaponPlayCheck: " + s); }
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
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponAxe.asset"); Check(data != null, "original data"); data.Init(); Check(data.attack != null && data.attack.GetComponent<WeaponAttack>().prefabProjectile != null, "original attack references");
        var weapon = new WeaponBase(data); WeaponAttack.A_SpawnedProjectileSuccessfully += OnSpawn; MyTime.time = 10f; MyTime.paused = false; Physics.SyncTransforms(); WeaponUtility.WeaponAttack(weapon);
        float deadline = Time.realtimeSinceStartup + 5f; while (Time.realtimeSinceStartup < deadline && (shot == null || enemy.hp >= 1000f || pool.projectilePools[data.eWeapon].CountInactive == 0)) yield return null;
        Check(shot != null && spawned == 2 && enemy.hp < 1000f, "actual two-axe attack damaged enemy"); Check(pool.projectilePools[data.eWeapon].CountInactive > 0, "projectile returned to pool"); Check(error == null, "unexpected error: " + error);
        Check(launchDirections.Count == 2 && Mathf.Abs(Vector3.SignedAngle(Vector3.forward, launchDirections[0], Vector3.up) + 24f) < .1f && Mathf.Abs(Vector3.SignedAngle(Vector3.forward, launchDirections[1], Vector3.up) - 24f) < .1f, "two axes spread evenly around facing");
        Check(Mathf.Abs(shot.transform.position.magnitude - (3.4f + 1.1f * shot.projectileRadius)) < .05f, "axe reaches its outward destination");
        var wall = new GameObject("blocking wall"); wall.layer = LayerMask.NameToLayer("Ground"); wall.transform.position = new Vector3(0f, 0f, 2f); wall.AddComponent<BoxCollider>().size = new Vector3(10f, 10f, .2f);
        gm.whatIsGroundAndObjects = 1 << wall.layer; Physics.SyncTransforms();
        var init = typeof(ProjectileAxe).GetMethod("TryInit", BindingFlags.Instance | BindingFlags.NonPublic);
        Check((bool)init.Invoke(shot, new object[] { 1 }), "wall test initialized");
        Check(Physics.Raycast(Vector3.zero, shot.transform.forward, out RaycastHit hit, 10f, gm.whatIsGroundAndObjects, QueryTriggerInteraction.Ignore), "wall blocks axe path");
        Vector3 destination = (Vector3)typeof(ProjectileAxe).GetField("desiredPosition", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shot);
        Check(Vector3.Distance(destination, hit.point - shot.transform.forward * shot.projectileRadius) < .01f, "wall shortens outward destination");
        WeaponAttack.A_SpawnedProjectileSuccessfully -= OnSpawn; Application.logMessageReceived -= Capture; Debug.Log("AxeWeaponPlayCheck passed: original two-axe attack, damage and projectile pool return"); EditorApplication.Exit(0);
    }
    void OnSpawn(ProjectileBase p) { shot = p; spawned++; launchDirections.Add(p.transform.forward); }
}
