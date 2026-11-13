using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
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
using UnityEngine.Pool;

public static class EnemySingleAttackKnockbackPortCheck
{
    public static void Run()
    {
        var state = new GameObject("state"); state.SetActive(false);
        GameManager.Instance = state.AddComponent<GameManager>(); GameManager.Instance.whatIsPlayer = 1;
        PoolManager.Instance = state.AddComponent<PoolManager>();
        var playerObject = new GameObject("player"); playerObject.SetActive(false);
        var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player; playerObject.transform.position = Vector3.right;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects();
        inventory.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); inventory.playerStats.stats = new Dictionary<EStat, float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) inventory.playerStats.stats[stat] = 0f;
        inventory.playerStats.stats[EStat.EnemyDamageMultiplier] = 1f; inventory.playerStats.stats[EStat.EnemyScalingMultiplier] = 1f; inventory.playerStats.stats[EStat.DamageCooldownMultiplier] = 1f;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = health.maxHp = 1000; inventory.playerHealth = health;
        int hits = 0; Vector3 direction = Vector3.zero; float force = 0f;
        PlayerHealth.A_TakeDamage += (_, dc, __) => { hits++; direction = dc.direction; force = dc.knockback; };
        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere); collider.transform.position = playerObject.transform.position; Physics.SyncTransforms();
        MapController.runConfig = new RunConfig { mapData = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset") };
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/RootAttack.prefab");
        var attackObject = UnityEngine.Object.Instantiate(source); var attack = attackObject.GetComponent<EnemySpecialAttackPrefabSingle>();
        if (attack == null) throw new Exception("Original single attack prefab not loaded");
        attackObject.transform.position = Vector3.zero;
        var enemyObject = new GameObject("enemy"); enemyObject.SetActive(false); var enemy = enemyObject.AddComponent<Enemy>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Miniboss_Pig.asset") });
        var config = new EnemySpecialAttack { attackRadius = 3f, damageMultiplier = 1f };
        typeof(EnemySpecialAttackPrefab).GetProperty("specialAttack").GetSetMethod(true).Invoke(attack, new object[] { config });
        typeof(EnemySpecialAttackPrefab).GetField("enemy", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(attack, enemy);
        PoolManager.Instance.enemyAttacksFxPools = new Dictionary<EEnemyAttack, ObjectPool<GameObject>>();
        PoolManager.Instance.enemyAttacksFxPools[attack.eAttack] = new ObjectPool<GameObject>(() => new GameObject("effect"));
        MyTime.time = 0f; MyTime.difficultyTimer = 0f; MyTime.finalSwarmTimer = 0f;
        Physics.SyncTransforms();
        if (!Physics.CheckSphere(attack.transform.position, config.attackRadius, GameManager.Instance.whatIsPlayer)) throw new Exception("Fixture player collider is outside attack sphere");
        typeof(EnemySpecialAttackPrefabSingle).GetMethod("SpawnHitEffect", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(attack, null);
        if (hits != 1 || health.hp >= 1000f || force != 25f || direction != Vector3.up)
            throw new Exception("Single attack must damage an offset player with upward knockback; hits=" + hits + ", hp=" + health.hp + ", force=" + force + ", direction=" + direction);
        Debug.Log("EnemySingleAttackKnockbackPortCheck passed");
    }
}
