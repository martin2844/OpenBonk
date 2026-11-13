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
using Assets.Scripts.Game.Combat.EnemyDebuffs;
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
public static class PoisonFlaskImpactPlayCheck
{
    const string Pending = "PoisonFlaskImpactPlayCheck.pending";
    static PoisonFlaskImpactPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("katana test runner").AddComponent<PoisonFlaskImpactPlayRunner>().Begin(); } }
}
public class PoisonFlaskImpactPlayRunner : MonoBehaviour
{
    ProjectileBase shot; string error;
    static void Check(bool b, string s) { if (!b) throw new Exception("PoisonFlaskImpactPlayCheck: " + s); }
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
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponPoisonFlask.asset"); Check(data != null, "original data"); data.Init(); Check(data.attack != null && data.attack.GetComponent<WeaponAttack>().prefabProjectile != null, "original attack references");
        var weapon = new WeaponBase(data); var weaponStats = (Dictionary<EStat, float>)typeof(WeaponBase).GetField("weaponStats", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon); weaponStats[EStat.DamageMultiplier] = 2.7f;
        WeaponAttack attack = pool.GetAttack(weapon); attack.weaponBase = weapon; var flask = (ProjectilePoisonFlask)pool.GetProjectile(attack); Check(flask != null && flask.effect != null && flask.rb != null, "original flask and effect assets");
        flask.weaponBase = weapon; typeof(ProjectileBase).GetField("weaponAttack", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(flask, attack);
        flask.transform.position = target.transform.position; flask.rb.velocity = Vector3.forward * 4f;
        typeof(ProjectilePoisonFlask).GetField("explosionSizeDefault", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(flask, flask.effect.transform.localScale);
        MyTime.time = 10f; MyTime.paused = false; Physics.SyncTransforms();
        Check((bool)typeof(ProjectilePoisonFlask).GetMethod("HitEnemy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(flask, new object[] { collider, Vector3.zero }), "enemy impact handled");
        Check(enemy.hp < 1000f && nearby.hp < 1000f && distant.hp == 1000f, "explosion damages nearby enemies only");
        Check(enemy.debuffsToAdd.TryGetValue(EDebuff.Poison, out var poison) && poison.stacks == 2 && Mathf.Abs(poison.duration - WeaponUtility.GetDuration(weapon)) < .001f, "poison stacks round down and use weapon duration");
        Check(nearby.debuffsToAdd.TryGetValue(EDebuff.Poison, out var neighborPoison) && neighborPoison.stacks == 2, "nearby enemy receives poison stacks");
        Check(flask.effect.gameObject.activeSelf && flask.effect.transform.parent == null, "impact effect detached and activated");
        Check(pool.projectilePools[data.eWeapon].CountInactive == 1, "flask returned to pool"); Check(error == null, "unexpected error: " + error);
        Application.logMessageReceived -= Capture; Debug.Log("PoisonFlaskImpactPlayCheck passed: area damage, poison stacks, detached effect and pool return"); EditorApplication.Exit(0); yield break;
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
