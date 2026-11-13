using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class AegisPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, P).GetValue(o);
    static void Call(object o, string name) => o.GetType().GetMethod(name, P).Invoke(o, null);
    static void Require(bool ok, string why) { if (!ok) throw new Exception("AegisPortCheck: " + why); }
    public static void Run()
    {
        var gm = new GameObject("game").AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var manager = new GameObject("enemies").AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
        PoolManager.Instance = new GameObject("pool").AddComponent<PoolManager>(); EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>();
        var save = new GameObject("save").AddComponent<SaveManager>(); save.Init(); save.stats = new StatsSaveFile();
        var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.width = 1f;
        player.playerConstantAttacks = new GameObject("auras").AddComponent<AuraAttacks>();
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory(); player.inventory.statusEffects = new PlayerStatusEffects();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f; stats.stats[EStat.Evasion] = 0f; player.inventory.playerStats = stats;
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponAegis.asset"); data.Init();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/AegisAttack.prefab"); var aegis = UnityEngine.Object.Instantiate(prefab).GetComponent<AegisAttack>(); aegis.transform.position = Vector3.zero; player.playerConstantAttacks.aegisAttack = aegis;
        MyTime.time = 10f; aegis.Set(new WeaponBase(data)); Require(aegis.IsActive() && Field<int>(aegis, "currentAmount") == 1, "initial shield");
        // Attack-speed changes can shorten either pending deadline, but must
        // not postpone one that was already earlier.
        var readyField = typeof(AegisAttack).GetField("shieldReadyAtTime", P);
        var amountField = typeof(AegisAttack).GetField("nextAmountTime", P);
        float savedReady = (float)readyField.GetValue(aegis), savedAmount = (float)amountField.GetValue(aegis);
        float expectedDeadline = MyTime.time + WeaponUtility.GetWeaponCooldown(aegis.weaponBase) * 1.5f;
        readyField.SetValue(aegis, expectedDeadline + 3f); amountField.SetValue(aegis, expectedDeadline - 1f);
        var onStat = typeof(AegisAttack).GetMethod("OnStatUpdate", P);
        onStat.Invoke(aegis, new object[] { EStat.DamageMultiplier });
        Require((float)readyField.GetValue(aegis) == expectedDeadline + 3f && (float)amountField.GetValue(aegis) == expectedDeadline - 1f, "unrelated stat leaves shield deadlines unchanged");
        onStat.Invoke(aegis, new object[] { EStat.AttackSpeed });
        Require(Mathf.Abs((float)readyField.GetValue(aegis) - expectedDeadline) < .0001f && (float)amountField.GetValue(aegis) == expectedDeadline - 1f, "attack speed shortens only later shield deadline");
        readyField.SetValue(aegis, savedReady); amountField.SetValue(aegis, savedAmount);
        MyTime.time = 12.26f; Call(aegis, "FixedUpdate"); Require(Field<int>(aegis, "currentAmount") == 2, "additional shield regenerates after original interval");
        var target = new GameObject("enemy"); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = Vector3.forward * 3f;
        var collider = target.AddComponent<CapsuleCollider>(); var enemy = target.AddComponent<Enemy>(); enemy.collider = collider; enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 1000f); enemy.renderer = target.AddComponent<MeshRenderer>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = target.AddComponent<Rigidbody>(); enemy.enemyMovement = target.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enemy = enemy; enemy.enemyMovement.rb = enemy.rb; manager.collidersToEnemies.Add(collider, enemy);
        Physics.SyncTransforms(); var health = new PlayerHealth(stats);
        var dc = new DamageContainer(0f, "test") { damage = 40f, knockback = 9f, direction = Vector3.back, flags = DcFlags.BypassEvade };
        Require(!health.CheckStopDamage(dc, false) && Mathf.Abs(dc.damage - 2f) < .001f && dc.knockback == 0f, "normal block reduces damage and knockback");
        Require(enemy.hp < 1000f && Field<int>(aegis, "currentAmount") == 1, "shield retaliation hits enemy in cone and spends charge");
        dc = new DamageContainer(0f, "boss") { damage = 40f, direction = Vector3.back, flags = DcFlags.BossDamage | DcFlags.BypassEvade };
        Require(!health.CheckStopDamage(dc, false) && Mathf.Abs(dc.damage - 13.2f) < .001f && !aegis.IsActive(), "boss block uses weak reduction and spends last shield");
        MyTime.time = 13.76f; Call(aegis, "FixedUpdate"); Require(!aegis.IsActive(), "cooldown boundary remains inactive");
        MyTime.time = 13.77f; Call(aegis, "FixedUpdate"); Require(aegis.IsActive(), "shield regenerates after cooldown");
        player.character = ECharacter.Athena; dc = new DamageContainer(0f, "test") { damage = 40f, direction = Vector3.back, flags = DcFlags.BypassEvade };
        Require(health.CheckStopDamage(dc, false), "Athena completely blocks ordinary damage");
        MyTime.time = 15.28f; Call(aegis, "FixedUpdate"); Require(aegis.IsActive(), "shield regenerates for remaining cases");
        dc = new DamageContainer(0f, "bypass") { damage = 40f, direction = Vector3.back, flags = DcFlags.BypassAegis | DcFlags.BypassEvade };
        Require(!health.CheckStopDamage(dc, false) && dc.damage == 40f && aegis.IsActive(), "bypass leaves shield and damage unchanged");
        dc = new DamageContainer(0f, "final boss") { damage = 40f, direction = Vector3.back, flags = DcFlags.BossDamage | DcFlags.FinalBossDamage | DcFlags.BypassEvade };
        Require(!health.CheckStopDamage(dc, false) && Mathf.Abs(dc.damage - 30f) < .001f && !aegis.IsActive(), "final boss uses original reduced block even for Athena");
        Debug.Log("AegisPortCheck passed: original shield regeneration, retaliation, normal and boss mitigation, Athena block");
    }
}
