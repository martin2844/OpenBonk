using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Actors.Enemies;
using Assets.Scripts.Actors;
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
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BloodMagicWeaponPlayCheck
{
    const string Pending = "BloodMagicWeaponPlayCheck.pending";
    static BloodMagicWeaponPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("katana test runner").AddComponent<BloodMagicWeaponPlayRunner>().Begin(); } }
}
public class BloodMagicWeaponPlayRunner : MonoBehaviour
{
    ProjectileBase shot; string error;
    static void Check(bool b, string s) { if (!b) throw new Exception("BloodMagicWeaponPlayCheck: " + s); }
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
        player.playerMovement = root.AddComponent<PlayerMovement>(); player.playerMovement.playerCam = new GameObject("camera direction").transform;
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool; EffectManager.Instance = root.AddComponent<EffectManager>();
        var target = new GameObject("stationary enemy"); target.SetActive(false); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = new Vector3(0f, .6f, 3f);
        var collider = target.AddComponent<CapsuleCollider>(); collider.radius = .8f; collider.height = 2f; var enemy = target.AddComponent<Enemy>(); enemy.enabled = false; enemy.collider = collider; enemy.renderer = target.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); enemies.collidersToEnemies.Add(collider, enemy); target.SetActive(true);
        Enemy nearby = CreateEnemy(enemies, new Vector3(1f, .6f, 3.2f));
        Enemy distant = CreateEnemy(enemies, new Vector3(0f, .6f, 20f));
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponBloodMagic.asset"); Check(data != null, "original data"); data.Init(); Check(data.attack != null && data.attack.GetComponent<WeaponAttack>().prefabProjectile != null, "original attack references");
        player.inventory.statInventory = new StatInventory(); save.stats = new StatsSaveFile();
        var weapon = new WeaponBase(data);
        var passive = typeof(WeaponBase).GetField("passive", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon);
        passive.GetType().GetField("stackChance", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(passive, 1f);
        nearby.rb = nearby.gameObject.AddComponent<Rigidbody>();
        nearby.enemyMovement = nearby.gameObject.AddComponent<EnemyMovementRb>();
        nearby.enemyMovement.enemy = nearby; nearby.enemyMovement.rb = nearby.rb; nearby.enemyMovement.enabled = false;
        nearby.dissolve = nearby.gameObject.AddComponent<EnemyDissolve>();
        nearby.dissolve.enemyRenderer = nearby.renderer; nearby.dissolve.Reset();
        nearby.enemyData.enemyName = EEnemy.Skeleton;
        nearby.hp = 1f; typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(nearby, 1f);
        WeaponAttack.A_SpawnedProjectileSuccessfully += OnSpawn; MyTime.time = 10f; MyTime.paused = false;
        Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(0f, "FireStaff"));
        Check(player.inventory.statInventory.movingStats.Count == 0, "other damage source does not grant passive health");
        Physics.SyncTransforms(); WeaponUtility.WeaponAttack(weapon);
        float deadline = Time.realtimeSinceStartup + 5f; while (Time.realtimeSinceStartup < deadline && shot == null) yield return null;
        Check(shot is ProjectileBloodMagic && enemy.hp < 1000f && nearby.hp < 1000f && distant.hp == 1000f, "original attack damages target and nearby enemy only");
        Check(nearby.IsDead(), "Blood Magic projectile killed nearby enemy");
        target.transform.position += Vector3.right * .5f; yield return null;
        Check(Vector3.Distance(shot.transform.position, enemy.GetFeetPosition()) < .001f, "effect follows living target's feet");
        float before = enemy.hp; while (Time.realtimeSinceStartup < deadline && pool.projectilePools[data.eWeapon].CountInactive == 0) yield return null;
        Check(enemy.hp == before, "attack does not repeat damage while active");
        Check(pool.projectilePools[data.eWeapon].CountInactive > 0, "projectile returned after duration"); Check(error == null, "unexpected error: " + error);
        typeof(Enemy).GetProperty("meshRadius").GetSetMethod(true).Invoke(enemy, new object[] { 2f });
        var init = typeof(ProjectileBloodMagic).GetMethod("TryInit", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int seed = 1; seed <= 5; seed++)
        {
            shot.transform.position = player.transform.position; Physics.SyncTransforms(); UnityEngine.Random.InitState(seed);
            Vector3 offset = UnityEngine.Random.insideUnitSphere; offset.y = 0f;
            Vector3 expected = enemy.GetFeetPosition() + offset * .8f;
            UnityEngine.Random.InitState(seed);
            Check((bool)init.Invoke(shot, new object[] { 0 }), "randomized strike initializes");
            Check(Vector3.Distance(shot.transform.position, expected) < .001f, "randomized placement uses target radius");
        }
        Vector3 lastPosition = shot.transform.position; enemy.hp = 0f; target.transform.position += Vector3.right;
        typeof(ProjectileBloodMagic).GetMethod("MyUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shot, null);
        Check(shot.transform.position == lastPosition, "effect stops following a dead target");
        collider.enabled = false; nearby.collider.enabled = false; distant.collider.enabled = false; Physics.SyncTransforms();
        Check(!(bool)init.Invoke(shot, new object[] { 0 }), "no target prevents initialization");
        var bloodSource = new DamageContainer(0f, EWeapon.BloodMagic.ToString());
        var modifier = player.inventory.statInventory.movingStats[EWeapon.BloodMagic.ToString()];
        Check(modifier.stat == EStat.MaxHealth && modifier.modification == 1f, "Blood Magic death event grants maximum health");
        Check(MyStats.GetStat(TrackStats.GetStatString(EMyStat.bloodMagicProcs)) == 1f, "Blood Magic proc is counted");
        player.inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        player.inventory.tomeInventory = new TomeInventory();
        player.inventory.statusEffects = new PlayerStatusEffects();
        player.inventory.statInventory.movingStats.Remove(EWeapon.BloodMagic.ToString());
        var actualStats = new PlayerStatsNew(player.inventory, true); player.inventory.playerStats = actualStats;
        typeof(PlayerStatsNew).GetMethod("UpdateStat", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(actualStats, new object[] { EStat.MaxHealth });
        player.inventory.playerHealth = new PlayerHealth(actualStats);
        int baselineMaxHp = player.inventory.playerHealth.maxHp;
        player.inventory.statInventory.movingStats.Add(EWeapon.BloodMagic.ToString(), modifier);
        player.inventory.statInventory.Tick(); actualStats.Tick();
        Check(player.inventory.playerHealth.maxHp == baselineMaxHp + 1, "passive flows through real stats to player maximum health");
        Enemy.A_EnemyDied?.Invoke(enemy, bloodSource);
        Check(player.inventory.statInventory.movingStats[EWeapon.BloodMagic.ToString()].modification == 1f, "passive cooldown prevents immediate second stack");
        weapon.Cleanup(); MyTime.time += 10f; Enemy.A_EnemyDied?.Invoke(enemy, bloodSource);
        Check(player.inventory.statInventory.movingStats[EWeapon.BloodMagic.ToString()].modification == 1f, "cleanup removes death listener");
        player.inventory.playerHealth.OnDestroy(); actualStats.OnDestroy(); player.inventory.statusEffects.OnDestroy();
        WeaponAttack.A_SpawnedProjectileSuccessfully -= OnSpawn; Application.logMessageReceived -= Capture; Debug.Log("BloodMagicWeaponPlayCheck passed: area damage, target following, death event passive, cooldown, cleanup and projectile pool return"); EditorApplication.Exit(0);
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
