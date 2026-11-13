using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class SpinningLaserPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool value, string message) { if (!value) throw new Exception("SpinningLaserPortCheck: " + message); }
    static void Near(float x, float y, string m) => Require(Mathf.Abs(x-y) < .001f, m + ": " + x + " != " + y);
    public static void Run()
    {
        var root = new GameObject("laser test managers"); root.SetActive(false); GameManager.Instance = root.AddComponent<GameManager>(); PoolManager.Instance = root.AddComponent<PoolManager>();
        var playerObj = new GameObject("laser test player"); playerObj.SetActive(false); var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = new Vector3(0, 1.6f, 20);
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>(); foreach (EStat s in Enum.GetValues(typeof(EStat))) stats.stats[s] = 0; stats.stats[EStat.EnemyDamageMultiplier] = 1; stats.stats[EStat.EnemyScalingMultiplier] = 1; stats.stats[EStat.DamageCooldownMultiplier] = 1; inv.playerStats = stats;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = health.maxHp = 1000; inv.playerHealth = health;
        var enemyObj = new GameObject("test pharaoh"); enemyObj.SetActive(false); var enemy = enemyObj.AddComponent<Enemy>(); enemy.renderer = enemyObj.AddComponent<MeshRenderer>(); enemy.enemyMovement = enemyObj.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = enemyObj.AddComponent<Rigidbody>();
        typeof(Enemy).GetProperty("meshHeight").GetSetMethod(true).Invoke(enemy, new object[] { 2f });
        var original = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Pharaoh1.asset"); var source = original.specialAttacks.First(a => a.attackPrefab != null && a.attackPrefab.GetComponent<EnemySpecialAttackSpinningLasers>() != null); var cloned = UnityEngine.Object.Instantiate(original);
        var config = new EnemySpecialAttack { isEnabled = true, attackName = "spinning laser fixture", attackPrefab = source.attackPrefab, triggerDistance = 100, duration = 8, damageMultiplier = 1, attackCooldown = 10 };
        cloned.specialAttacks = new[] { config }; typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { cloned });
        MapController.runConfig = new RunConfig { mapData = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset") };
        var colliderObj = GameObject.CreatePrimitive(PrimitiveType.Cube); colliderObj.transform.position = new Vector3(0, 1.6f, 10f); colliderObj.layer = 0; GameManager.Instance.whatIsGroundAndObjects = 1 << 0; GameManager.Instance.whatIsPlayer = 0;
        Action<PlayerHealth, DamageContainer, bool> listener = null; GameObject attackObj = null;
        try
        {
            Near(enemy.GetHeight(), 2, "height"); Near(enemy.GetBottomPosition().y, -1, "bottom"); Near(enemy.GetGroundCheckPosition().y, -.75f, "ground check");
            typeof(Enemy).GetField("allowSpecialAttacks", Private).SetValue(enemy, true); enemy.DisableSpecialAttacks(); Require(!(bool)typeof(Enemy).GetField("allowSpecialAttacks", Private).GetValue(enemy), "disable special attacks");
            MyTime.time = 1f; MyTime.fixedDeltaTime = .5f; MyTime.deltaTime = .25f; MyTime.difficultyTimer = 0; MyTime.finalSwarmTimer = 0;
            var controller = new SpecialAttackController(enemy); typeof(SpecialAttackController).GetField("nextCheckTime", Private).SetValue(controller, 0f); controller.Tick();
            Require((bool)typeof(SpecialAttackController).GetField("isAttacking", Private).GetValue(controller), "scheduler selected original laser prefab");
            var attack = Resources.FindObjectsOfTypeAll<EnemySpecialAttackSpinningLasers>().First(x => x.gameObject.name.EndsWith("(Clone)")); attackObj = attack.gameObject;
            var beams = (Transform[])typeof(EnemySpecialAttackSpinningLasers).GetField("lasers", Private).GetValue(attack); Require(beams.Length > 1, "original prefab contains multiple beams");
            Near(attack.audio.volume, 0f, "audio begins faded out");
            MyTime.time = 1.5f; Physics.SyncTransforms(); typeof(EnemySpecialAttackSpinningLasers).GetMethod("FixedUpdate", Private).Invoke(attack, null);
            float expected = 250f * (1f - Mathf.Sqrt(.75f));
            Near((float)typeof(EnemySpecialAttackSpinningLasers).GetField("laserLength", Private).GetValue(attack), expected, "beam expands using circular easing");
            Require(attack.audio.volume > 0f && attack.laserParent.rotation != Quaternion.identity, "audio and rotation advance");
            int events = 0; Vector3 direction = Vector3.zero; listener = (_, dc, __) => { events++; direction = dc.direction; }; PlayerHealth.A_TakeDamage += listener;
            MyTime.time = 2; var hit = typeof(EnemySpecialAttackSpinningLasers).GetMethod("OnTriggerStay", Private); hit.Invoke(attack, new object[] { colliderObj.GetComponent<Collider>() }); Require(health.hp < 1000 && events == 1 && direction == Vector3.up, "laser applies original damage and direction");
            hit.Invoke(attack, new object[] { colliderObj.GetComponent<Collider>() }); Require(events == 1, "damage cooldown");
            MyTime.time = 9; typeof(EnemySpecialAttackSpinningLasers).GetMethod("FixedUpdate", Private).Invoke(attack, null); Require(!attackObj.activeSelf && PoolManager.Instance.enemyAttacksPools[config.attackName].CountActive == 0, "duration returns lasers to pool");
            Debug.Log("SpinningLaserPortCheck passed: original prefab scheduling, beam expansion, rotation, damage cooldown and pool return");
        }
        finally { if (listener != null) PlayerHealth.A_TakeDamage -= listener; inv.itemInventory.Cleanup(); inv.statusEffects.OnDestroy(); if (attackObj != null) UnityEngine.Object.DestroyImmediate(attackObj); UnityEngine.Object.DestroyImmediate(colliderObj); UnityEngine.Object.DestroyImmediate(cloned); UnityEngine.Object.DestroyImmediate(enemyObj); UnityEngine.Object.DestroyImmediate(playerObj); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; PoolManager.Instance = null; }
    }
}
