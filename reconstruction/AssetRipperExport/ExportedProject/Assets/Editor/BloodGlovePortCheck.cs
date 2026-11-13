using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class BloodGlovePortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("BloodGlovePortCheck: " + message); }
    static Enemy Target(EnemyManager manager, string name, Vector3 position)
    {
        var target = new GameObject(name); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = position;
        var collider = target.AddComponent<SphereCollider>(); var enemy = target.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 1000f); enemy.renderer = target.AddComponent<MeshRenderer>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = target.AddComponent<Rigidbody>(); enemy.enemyMovement = target.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enemy = enemy; enemy.enemyMovement.rb = enemy.rb;
        manager.collidersToEnemies.Add(collider, enemy); return enemy;
    }
    static void Activate(Enemy enemy)
    {
        var pending = enemy.debuffsToAdd[EDebuff.Bloodmark]; enemy.debuffsToAdd.Clear();
        typeof(Enemy).GetMethod("AddDebuffImplementation", Private).Invoke(enemy, new object[] { pending });
    }
    public static void Run()
    {
        var game = new GameObject("game").AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var manager = new GameObject("enemies").AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
        var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player; typeof(MyPlayer).GetField("baseDamage", Private).SetValue(player, 5f);
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = 50; health.maxHp = 100; player.inventory.playerHealth = health;
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; player.inventory.playerStats = stats;
        EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>(); EffectManager.Instance.gloveBlood = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BloodyGlovesFx.prefab");
        Require(EffectManager.Instance.gloveBlood != null, "original effect prefab");
        var near = Target(manager, "near", Vector3.zero); var nearby = Target(manager, "nearby", new Vector3(9f, 0f, 0f)); var far = Target(manager, "far", new Vector3(12f, 0f, 0f));
        var inventory = player.inventory.itemInventory; inventory.AddItem(EItem.GloveBlood, 2); var glove = (ItemGlovesBlood)inventory.GetItem(EItem.GloveBlood);
        Require(glove.HasOnHitEffectProc(), "inventory registers original on-hit proc"); MyTime.time = 10f; Physics.SyncTransforms();
        var hit = new DamageContainer(1f, "test") { enemy = near }; inventory.PostDamage(hit);
        Require(Mathf.Abs(near.hp - 968.5f) < .001f && Mathf.Abs(nearby.hp - 968.5f) < .001f && far.hp == 1000f, "two gloves damage enemies within ten units");
        Require(health.hp == 66, "heals once per enemy using rounded maximum-health percentage");
        var pending = near.debuffsToAdd[EDebuff.Bloodmark]; Require(pending.stacks == 2 && pending.duration == 5f, "marks carry glove stacks and duration");
        Require(glove.readyAtTime == 19f, "nine-second cooldown");
        var fx = (EffectPlayer)typeof(ItemGlovesBlood).GetField("fx", Private).GetValue(glove); Require(fx != null && fx.gameObject.activeSelf && fx.transform.position == near.transform.position, "original effect spawned at hit");
        Activate(near); Activate(nearby); Require(near.HasDebuff(EDebuff.Bloodmark) && nearby.HasDebuff(EDebuff.Bloodmark), "factory activates bloodmarks");
        near.RemoveDebuff(EDebuff.Bloodmark, false); Require(Mathf.Abs(near.hp - 961f) < .001f, "bloodmark removal deals stored damage");
        nearby.hp = 20f; typeof(Enemy).GetField("controlHp", Private).SetValue(nearby, 20f);
        nearby.DamageExternal(new DamageContainer(0f, "test") { damage = 13f, enemy = nearby });
        Require(nearby.debuffsToRemove.Contains(EDebuff.Bloodmark), "damage reaching bloodmark threshold queues removal");
        nearby.RemoveDebuff(EDebuff.Bloodmark, true); Require(nearby.hp == 7f && nearby.A_DamageNonStatic == null, "death removal unsubscribes without extra damage");
        MyTime.time = 18.9f; inventory.PostDamage(hit); Require(near.hp == 961f && health.hp == 66, "cooldown blocks repeats");
        near.GetComponent<Collider>().enabled = false; nearby.GetComponent<Collider>().enabled = false; far.GetComponent<Collider>().enabled = false; Physics.SyncTransforms();
        MyTime.time = 20f; inventory.PostDamage(hit); Require(Mathf.Abs(glove.readyAtTime - 20.02f) < .001f, "empty scan uses short retry delay");
        inventory.RemoveItem(EItem.GloveBlood); inventory.RemoveItem(EItem.GloveBlood); Require(inventory.GetItem(EItem.GloveBlood) == null, "last removal unregisters item");
        Debug.Log("BloodGlovePortCheck passed: inventory proc, healing, bloodmarks, threshold, cooldown and effect prefab");
    }
}
