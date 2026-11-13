using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AegisAttackPlayCheck
{
    const string Pending = "AegisAttackPlayCheck.pending";
    static AegisAttackPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("aegis runner").AddComponent<AegisAttackPlayRunner>().Begin(); } }
}
public class AegisAttackPlayRunner : MonoBehaviour
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    string error;
    void OnLog(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) error = message + "\n" + stack; }
    static void Require(bool ok, string message) { if (!ok) throw new Exception("AegisAttackPlayCheck: " + message); }
    public void Begin() { Application.logMessageReceived += OnLog; StartCoroutine(Run()); }
    void Update() { MyTime.deltaTime = Time.deltaTime; }
    void FixedUpdate() { MyTime.fixedDeltaTime = Time.fixedDeltaTime; MyTime.time += Time.fixedDeltaTime; }
    static Enemy MakeEnemy(string name, Vector3 position, EnemyManager manager)
    {
        var obj = new GameObject(name); obj.SetActive(false); obj.layer = LayerMask.NameToLayer("Enemy"); obj.transform.position = position;
        var collider = obj.AddComponent<CapsuleCollider>(); var enemy = obj.AddComponent<Enemy>(); enemy.enabled = false; enemy.collider = collider; enemy.hp = enemy.maxHp = 1000f;
        enemy.renderer = obj.AddComponent<MeshRenderer>(); enemy.rb = obj.AddComponent<Rigidbody>(); enemy.rb.isKinematic = true;
        enemy.enemyMovement = obj.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enabled = false; enemy.enemyMovement.enemy = enemy; enemy.enemyMovement.rb = enemy.rb;
        typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        manager.collidersToEnemies.Add(collider, enemy); obj.SetActive(true); return enemy;
    }
    IEnumerator Run()
    {
        PlayerHealth health = null; Enemy front = null, side = null; MyPlayer player = null; AuraAttacks controller = null; WeaponInventory inventory = null; WeaponData data = null; PassiveAbilityLockIn passive = null;
        try
        {
            new GameObject("audio listener").AddComponent<AudioListener>();
            var root = new GameObject("state"); root.SetActive(false);
            var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
            var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
            PoolManager.Instance = root.AddComponent<PoolManager>(); EffectManager.Instance = root.AddComponent<EffectManager>();
            PoolManager.Instance.damageNumbersPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/DamageNumbersNew.prefab");
            PoolManager.Instance.damageNumbersParent = new GameObject("damage popup canvas", typeof(RectTransform), typeof(Canvas)).transform;
            typeof(PoolManager).GetMethod("Start", P).Invoke(PoolManager.Instance, null);
            var camera = new GameObject("camera"); camera.SetActive(false); PlayerCamera.Instance = camera.AddComponent<PlayerCamera>(); camera.transform.position = new Vector3(0, 2, -10);
            var save = root.AddComponent<SaveManager>(); save.Init(); save.stats = new StatsSaveFile();
            player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.width = 1f; player.feet = new GameObject("feet").transform;
            var renderer = new GameObject("player renderer"); renderer.SetActive(false); player.playerRenderer = renderer.AddComponent<PlayerRenderer>();
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory(); player.inventory.statusEffects = new PlayerStatusEffects();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
            stats.stats[EStat.MaxHealth] = 100f; stats.stats[EStat.Armor] = 0f; stats.stats[EStat.DamageReductionMultiplier] = 0f; stats.stats[EStat.Thorns] = 0f; stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.Evasion] = 0f;
            player.inventory.playerStats = stats; health = new PlayerHealth(stats); player.inventory.playerHealth = health;
            passive = new PassiveAbilityLockIn(); player.inventory.passiveAbility = passive; passive.Init();
            front = MakeEnemy("front enemy", Vector3.forward * 3f, enemies); side = MakeEnemy("side enemy", Vector3.right * 3f, enemies);
            data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponAegis.asset"); data.Init();
            var dataManager = root.AddComponent<DataManager>(); DataManager.Instance = dataManager;
            ((Dictionary<EWeapon, WeaponData>)typeof(DataManager).GetField("weapons", P).GetValue(dataManager)).Add(data.eWeapon, data);
            inventory = new WeaponInventory(); player.inventory.weaponInventory = inventory;
            controller = new GameObject("aura controller").AddComponent<AuraAttacks>(); player.playerConstantAttacks = controller;
            MyTime.time = 10f; MyTime.paused = false; MyTime.fixedDeltaTime = Time.fixedDeltaTime; inventory.AddWeapon(data, null); Physics.SyncTransforms();
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return new WaitForFixedUpdate();
        try
        {
            Require(error == null, "unexpected Unity error: " + error);
            Require(controller.aegisAttack != null && controller.aegisAttack.IsActive(), "inventory creates active original Aegis");
            health.DamagePlayerExternal(40f, 0f, Vector3.back, false, "test", DcFlags.BypassEvade);
            Require(health.hp == 98 && front.hp < 1000f && side.hp == 1000f, "actual damage path reduces health by 2 and retaliation hits only forward enemy");
            var popup = PoolManager.Instance.damageNumbersParent.GetComponentInChildren<DamageNumbers>(true);
            Require(popup != null && popup.gameObject.activeSelf && popup.damageText.text == "Block" && popup.damageText.color == MyColorUtility.aegisColor && popup.damageText.color.a == 1f, "blocked hit creates visible original world popup");
            Require(!controller.aegisAttack.IsActive(), "last charge consumed");
            Require(Mathf.Abs(passive.statModifiers[EStat.AttackSpeed].GetModifiers().Single().modification - 1.5f) < .0001f, "depleted original Aegis activates Lock In attack speed");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        float deadline = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < deadline && !controller.aegisAttack.IsActive() && error == null) yield return new WaitForFixedUpdate();
        try
        {
            Require(error == null, "unexpected Unity error: " + error);
            Require(controller.aegisAttack.IsActive(), "shield regenerates through live fixed updates");
            Require(Mathf.Abs(passive.statModifiers[EStat.AttackSpeed].GetModifiers().Single().modification) < .0001f, "regenerated original Aegis removes Lock In attack speed");
            inventory.ToggleWeapon(data.eWeapon, false);
            var auras = (Dictionary<EWeapon, ConstantAttack>)typeof(AuraAttacks).GetField("auras", P).GetValue(controller);
            Require(!auras.ContainsKey(data.eWeapon), "inventory toggle removes Aegis");
            passive.Cleanup();
            Debug.Log("AegisAttackPlayCheck passed: inventory acquisition, player damage mitigation, directional retaliation, Lock In depletion and regeneration, timed regeneration and removal");
            Application.logMessageReceived -= OnLog; EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
