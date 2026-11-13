using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
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
public static class CombatAuraPlayCheck
{
    const string Pending = "CombatAuraPlayCheck.pending";
    static CombatAuraPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("mine test runner").AddComponent<CombatAuraPlayRunner>().Begin(); } }
}
public class CombatAuraPlayRunner : MonoBehaviour
{
    ProjectileBase shot; string error;
    static void Check(bool b, string s) { if (!b) throw new Exception("CombatAuraPlayCheck: " + s); }
    void Capture(string m, string s, LogType t) { if (t == LogType.Exception || t == LogType.Error || t == LogType.Assert) error = m + "\n" + s; }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    void Update() { MyTime.deltaTime = Time.deltaTime; }
    void FixedUpdate() { MyTime.fixedDeltaTime = Time.fixedDeltaTime; MyTime.time += Time.fixedDeltaTime; }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture; var root = new GameObject("test state"); root.SetActive(false); root.transform.position = new Vector3(0f, .5f, 0f);
        var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy"); gm.whatIsGroundAndObjects = 1 << LayerMask.NameToLayer("Ground");
        var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies; var save = root.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1; save.config.cfVisualsSettings.damage_numbers = 0;
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerRenderer = root.AddComponent<PlayerRenderer>(); player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f; player.inventory.playerStats = stats;
        player.playerMovement = root.AddComponent<PlayerMovement>(); player.playerMovement.playerCam = new GameObject("camera direction").transform;
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool; pool.explosionFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/RocketExplosion.prefab"); typeof(PoolManager).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pool, null); EffectManager.Instance = root.AddComponent<EffectManager>();
        var floor = new GameObject("floor"); floor.layer = LayerMask.NameToLayer("Ground"); floor.transform.position = new Vector3(0f, -.5f, 0f); floor.AddComponent<BoxCollider>().size = new Vector3(50f, 1f, 50f);
        var target = new GameObject("stationary enemy"); target.SetActive(false); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = new Vector3(0f, .6f, -2f);
        var collider = target.AddComponent<CapsuleCollider>(); collider.radius = .8f; collider.height = 2f; var enemy = target.AddComponent<Enemy>(); enemy.enabled = false; enemy.collider = collider; enemy.renderer = target.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); enemies.collidersToEnemies.Add(collider, enemy); target.SetActive(true);
        Enemy nearby = CreateEnemy(enemies, new Vector3(0f, .6f, -3f));
        Enemy distant = CreateEnemy(enemies, new Vector3(0f, .6f, 20f));
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponAura.asset"); Check(data != null, "original Aura data"); data.Init();
        var weapon = new WeaponBase(data); var auraObject = Instantiate(data.attack); auraObject.transform.position = Vector3.zero; var aura = auraObject.GetComponent<CombatAura>(); Check(aura != null, "original Aura prefab"); aura.enabled = false; aura.Set(weapon);
        MyTime.time = 10f; MyTime.paused = false; Physics.SyncTransforms();
        var tick = typeof(CombatAura).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        pool.defaultHitPrefab = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GeneralProjectileHit.prefab")); pool.defaultHitPrefab.SetActive(false); pool.defaultHitPrefab.GetComponent<RandomSfx>().playOnAwake = false;
        var extras = new List<Enemy>(); for (int i = 0; i < 23; i++) extras.Add(CreateEnemy(enemies, new Vector3(2f, .6f, 0f))); Physics.SyncTransforms();
        tick.Invoke(aura, null);
        Debug.Log($"aura diagnostic hp={enemy.hp},{nearby.hp},{distant.hp} radius={typeof(CombatAura).GetField("radius", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(aura)} auraPos={aura.transform.position} count={EnemyTargeting.GetEnemiesInRadiusSafe(aura, aura.transform.position, 7f, out var tmp)}");
        Check(enemy.hp < 1000f && nearby.hp < 1000f && distant.hp == 1000f, "area damage and distant exclusion");
        Check(extras.TrueForAll(e => e.hp < 1000f), "damage is not limited to first twenty enemies");
        Check(pool.projectileHitPools[data.eWeapon.ToString()].CountActive == 20, "hit effects limited to twenty");
        float firstHp = enemy.hp;
        float interval = (float)typeof(CombatAura).GetField("cooldown", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(aura);
        MyTime.time = 10f + interval - .001f; tick.Invoke(aura, null); Check(enemy.hp == firstHp, "no early hit");
        MyTime.time = 10f + interval; tick.Invoke(aura, null); Check(enemy.hp < firstHp, "hit at cooldown boundary");
        float originalRadius = (float)typeof(CombatAura).GetField("radius", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(aura);
        stats.stats[EStat.SizeMultiplier] = 2f; PlayerStatsNew.A_StatUpdate?.Invoke(EStat.SizeMultiplier);
        float newRadius = (float)typeof(CombatAura).GetField("radius", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(aura);
        Check(Mathf.Abs(newRadius - originalRadius * 2f) < .001f, "size update");
        stats.stats[EStat.AttackSpeed] = 2f; PlayerStatsNew.A_StatUpdate?.Invoke(EStat.AttackSpeed);
        float faster = (float)typeof(CombatAura).GetField("cooldown", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(aura);
        Check(faster < interval && faster >= .06f, "attack speed and minimum interval");
        Check(error == null, "unexpected error: " + error); Application.logMessageReceived -= Capture; Debug.Log("CombatAuraPlayCheck passed: damage, hit effect cap, cooldown, size and speed"); EditorApplication.Exit(0); yield break;
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

}
