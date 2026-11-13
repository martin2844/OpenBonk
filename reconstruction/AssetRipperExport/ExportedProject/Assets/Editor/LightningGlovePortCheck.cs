using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class LightningGlovePortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("LightningGlovePortCheck: " + message); }
    static Enemy Target(EnemyManager manager, string name, Vector3 position)
    {
        var target = new GameObject(name); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = position;
        var collider = target.AddComponent<SphereCollider>(); var enemy = target.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 1000f); enemy.renderer = target.AddComponent<MeshRenderer>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = target.AddComponent<Rigidbody>(); enemy.enemyMovement = target.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enemy = enemy; enemy.enemyMovement.rb = enemy.rb;
        manager.collidersToEnemies.Add(collider, enemy); return enemy;
    }
    public static void Run()
    {
        var game = new GameObject("game").AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var manager = new GameObject("enemies").AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
        var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player; typeof(MyPlayer).GetField("baseDamage", Private).SetValue(player, 5f);
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; player.inventory.playerStats = stats;
        EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>(); EffectManager.Instance.gloveLightning = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/LightningGloveFx.prefab");
        Require(EffectManager.Instance.gloveLightning != null, "original effect prefab");
        var near = Target(manager, "near", Vector3.zero); var nearby = Target(manager, "nearby", new Vector3(7f, 0f, 0f)); var far = Target(manager, "far", new Vector3(10f, 0f, 0f));
        var inventory = player.inventory.itemInventory; inventory.AddItem(EItem.GloveLightning, 2); var glove = (ItemGlovesLightning)inventory.GetItem(EItem.GloveLightning);
        Require(glove.HasOnHitEffectProc(), "inventory registers original on-hit proc"); MyTime.time = 10f; Physics.SyncTransforms();
        var hit = new DamageContainer(1f, "test") { enemy = near }; inventory.PostDamage(hit);
        Require(Mathf.Abs(near.hp - 970f) < .001f && Mathf.Abs(nearby.hp - 970f) < .001f && far.hp == 1000f, "two stacks damage both enemies within eight units");
        Require(near.debuffsToAdd.ContainsKey(EDebuff.Stun) && nearby.debuffsToAdd.ContainsKey(EDebuff.Stun), "stun queued for each enemy");
        Require(Mathf.Abs(glove.readyAtTime - 20f) < .001f, "ten-second cooldown");
        var fx = (EffectPlayer)typeof(ItemGlovesLightning).GetField("fx", Private).GetValue(glove); Require(fx != null && fx.gameObject.activeSelf && fx.transform.position == near.transform.position, "original effect spawned at hit");
        MyTime.time = 19.9f; inventory.PostDamage(hit); Require(near.hp == 970f, "cooldown blocks repeats");
        MyTime.time = 20f; inventory.PostDamage(hit); Require(near.hp == 940f && nearby.hp == 940f && glove.readyAtTime == 30f, "cooldown boundary allows another proc");
        near.hp = 0f; MyTime.time = 30f; inventory.PostDamage(hit); Require(glove.readyAtTime == 30f && nearby.hp == 940f, "dead target does not activate");
        near.hp = 940f; near.GetComponent<Collider>().enabled = false; nearby.GetComponent<Collider>().enabled = false; far.GetComponent<Collider>().enabled = false; Physics.SyncTransforms();
        MyTime.time = 31f; inventory.PostDamage(hit); Require(Mathf.Abs(glove.readyAtTime - 31.02f) < .001f && nearby.hp == 940f, "empty scan uses short retry delay");
        inventory.RemoveItem(EItem.GloveLightning); inventory.RemoveItem(EItem.GloveLightning); Require(inventory.GetItem(EItem.GloveLightning) == null, "last removal unregisters item");
        Debug.Log("LightningGlovePortCheck passed: original inventory proc, area damage, stun, cooldown and effect prefab");
    }
}
