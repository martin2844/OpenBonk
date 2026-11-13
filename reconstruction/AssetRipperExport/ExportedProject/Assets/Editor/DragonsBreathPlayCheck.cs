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
public static class DragonsBreathPlayCheck
{
    const string Pending = "DragonsBreathPlayCheck.pending";
    static DragonsBreathPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("mine test runner").AddComponent<DragonsBreathPlayRunner>().Begin(); } }
}
public class DragonsBreathPlayRunner : MonoBehaviour
{
    ProjectileBase shot; string error;
    static void Check(bool b, string s) { if (!b) throw new Exception("DragonsBreathPlayCheck: " + s); }
    void Capture(string m, string s, LogType t) { if (t == LogType.Exception || t == LogType.Error || t == LogType.Assert) error = m + "\n" + s; }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    void Update() { MyTime.deltaTime = Time.deltaTime; }
    void FixedUpdate() { MyTime.fixedDeltaTime = Time.fixedDeltaTime; MyTime.time += Time.fixedDeltaTime; }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture; var root = new GameObject("test state"); root.SetActive(false); root.transform.position = Vector3.zero;
        var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy"); gm.whatIsGroundAndObjects = 1 << LayerMask.NameToLayer("Ground");
        var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies; var save = root.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1; save.config.cfVisualsSettings.damage_numbers = 0;
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerRenderer = root.AddComponent<PlayerRenderer>(); player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f; player.inventory.playerStats = stats;
        player.playerMovement = root.AddComponent<PlayerMovement>(); player.playerMovement.playerCam = new GameObject("camera direction").transform; player.playerMovement.orientation = player.transform; var body = root.AddComponent<Rigidbody>(); typeof(PlayerMovement).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player.playerMovement, body);
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool; pool.explosionFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/RocketExplosion.prefab"); typeof(PoolManager).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pool, null); EffectManager.Instance = root.AddComponent<EffectManager>();
        var floor = new GameObject("floor"); floor.layer = LayerMask.NameToLayer("Ground"); floor.transform.position = new Vector3(0f, -.5f, 0f); floor.AddComponent<BoxCollider>().size = new Vector3(50f, 1f, 50f);
        var target = new GameObject("stationary enemy"); target.SetActive(false); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = new Vector3(0f, .6f, 3f);
        var collider = target.AddComponent<CapsuleCollider>(); collider.radius = .8f; collider.height = 2f; var enemy = target.AddComponent<Enemy>(); enemy.enabled = false; enemy.collider = collider; enemy.renderer = target.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); enemies.collidersToEnemies.Add(collider, enemy); target.SetActive(true);
        Enemy nearby = CreateEnemy(enemies, new Vector3(0f, .6f, 5f));
        Enemy distant = CreateEnemy(enemies, new Vector3(0f, .6f, -20f));
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponDragonsBreath.asset"); Check(data != null, "original Dragon Breath data"); data.Init();
        MyTime.time = 10f; MyTime.paused = false; var weapon = new WeaponBase(data); var auraObject = Instantiate(data.attack); auraObject.transform.position = Vector3.zero; var aura = auraObject.GetComponent<ProjectileDragonsBreath>(); Check(aura != null, "original flame prefab"); aura.enabled = false; aura.Set(weapon);
        EffectManager.Instance = null; Physics.SyncTransforms(); var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var tick = typeof(ProjectileDragonsBreath).GetMethod("FixedUpdate", flags); var active = typeof(ProjectileDragonsBreath).GetField("isActive", flags);
        float startAt = (float)typeof(ProjectileDragonsBreath).GetField("startTime", flags).GetValue(aura); float stopAt = (float)typeof(ProjectileDragonsBreath).GetField("stopTime", flags).GetValue(aura);
        MyTime.time = startAt; tick.Invoke(aura, null); Check(!(bool)active.GetValue(aura) && enemy.hp == 1000f, "no attack at start boundary");
        MyTime.time = startAt + .001f; tick.Invoke(aura, null); Check((bool)active.GetValue(aura) && enemy.hp < 1000f && nearby.hp < 1000f && distant.hp == 1000f, "flame targets and damages forward enemies");
        float firstHp = enemy.hp; MyTime.time = startAt + .15f; tick.Invoke(aura, null); Check(enemy.hp == firstHp, "per-enemy cooldown suppresses early hit");
        MyTime.time = startAt + .251f; tick.Invoke(aura, null); Check(enemy.hp < firstHp, "per-enemy cooldown allows later hit");
        MyTime.time = stopAt; tick.Invoke(aura, null); Check(!(bool)active.GetValue(aura), "flame stops at duration boundary");
        MyTime.time = stopAt + .21f; float stoppedHp = enemy.hp; tick.Invoke(aura, null); Check(enemy.hp == stoppedHp, "damage stops after linger");
        float beforeRange = (float)typeof(ProjectileDragonsBreath).GetField("range", flags).GetValue(aura); stats.stats[EStat.SizeMultiplier] = 2f; PlayerStatsNew.A_StatUpdate?.Invoke(EStat.SizeMultiplier);
        Check(Mathf.Abs((float)typeof(ProjectileDragonsBreath).GetField("range", flags).GetValue(aura) - beforeRange * 2f) < .001f, "range updates with size");
        Check(error == null, "unexpected error: " + error); Application.logMessageReceived -= Capture; Debug.Log("DragonsBreathPlayCheck passed: flame cycle, cone damage, per-enemy cooldown, linger and range"); EditorApplication.Exit(0); yield break;
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
