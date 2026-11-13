using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class PowerGlovePortCheck
{
    sealed class FixedRandom : System.Random { readonly double value; public FixedRandom(double value) { this.value = value; } public override double NextDouble() => value; }
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("PowerGlovePortCheck: " + message); }
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
    static Enemy Target(EnemyManager manager, string name, Vector3 position)
    {
        var target = new GameObject(name); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = position;
        var collider = target.AddComponent<SphereCollider>(); var enemy = target.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 1000f); enemy.renderer = target.AddComponent<MeshRenderer>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = target.AddComponent<Rigidbody>(); enemy.enemyMovement = target.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enemy = enemy; enemy.enemyMovement.rb = enemy.rb;
        typeof(EnemyMovementRb).GetField("lastFoundKnockbackResTime", Private).SetValue(enemy.enemyMovement, 1000f);
        manager.collidersToEnemies.Add(collider, enemy); return enemy;
    }
    public static void Run()
    {
        var game = new GameObject("game").AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var manager = new GameObject("enemies").AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
        var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = new Vector3(-2,0,0); player.feet = new GameObject("feet").transform; player.feet.SetParent(player.transform);
        typeof(MyPlayer).GetField("baseDamage", Private).SetValue(player, 5f);
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; player.inventory.playerStats = stats;
        EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>(); EffectManager.Instance.glovePower = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PowerGlovesFx.prefab");
        Require(EffectManager.Instance.glovePower != null, "original effect prefab");
        var near = Target(manager, "near", Vector3.zero); var nearby = Target(manager, "nearby", new Vector3(19f, 0f, 0f)); var far = Target(manager, "far", new Vector3(22f, 0f, 0f));
        var inventory = player.inventory.itemInventory; inventory.AddItem(EItem.GlovePower, 2); var glove = (ItemGlovesPower)inventory.GetItem(EItem.GlovePower);
        Require(glove.HasOnHitEffectProc(), "inventory registers original on-hit proc");
        Require(Mathf.Abs(Field<float>(glove, "procChance") - (.16f / .66f)) < .0001f && Field<float>(glove, "radius") == 20f && Field<float>(glove, "cooldown") == 1.5f, "two-stack chance, radius and cooldown");
        var hit = new DamageContainer(1f, "test") { enemy = near }; MyTime.time = 10f; Physics.SyncTransforms();
        global::Utility.MyRandom.random = new FixedRandom(.99); inventory.PostDamage(hit); Require(near.hp == 1000f && Field<float>(glove, "readyAtTime") == 0f, "failed chance roll does not consume cooldown");
        global::Utility.MyRandom.random = new FixedRandom(0); inventory.PostDamage(hit);
        Require(Mathf.Abs(near.hp - 987.5f) < .001f && nearby.hp == 987.5f && far.hp == 1000f, "two stacks hit enemies within twenty units");
        Require(Field<Vector3>(near.enemyMovement, "knockbackVelocity").x > 0f && Field<Vector3>(nearby.enemyMovement, "knockbackVelocity").x > 0f, "knockback pushes away from player");
        Require(Field<float>(glove, "readyAtTime") == 11.5f, "successful proc starts cooldown");
        var fx = Field<EffectPlayer>(glove, "fx"); Require(fx != null && fx.transform.parent == player.feet && fx.transform.localPosition == Vector3.up * .01f && fx.transform.localScale == Vector3.one * 20f, "original effect attached and scaled to radius");
        MyTime.time = 11.4f; inventory.PostDamage(hit); Require(near.hp == 987.5f, "cooldown blocks repeat");
        MyTime.time = 11.5f; inventory.PostDamage(hit); Require(near.hp == 975f && Field<float>(glove, "readyAtTime") == 13f, "cooldown boundary allows repeat");
        near.GetComponent<Collider>().enabled = false; nearby.GetComponent<Collider>().enabled = false; far.GetComponent<Collider>().enabled = false; Physics.SyncTransforms();
        MyTime.time = 14f; inventory.PostDamage(hit); Require(Field<float>(glove, "readyAtTime") == 13f, "empty scan does not consume cooldown");
        inventory.RemoveItem(EItem.GlovePower); Require(Field<float>(glove, "radius") == 15f, "removal updates radius"); inventory.RemoveItem(EItem.GlovePower); Require(inventory.GetItem(EItem.GlovePower) == null, "last removal unregisters item");
        Debug.Log("PowerGlovePortCheck passed: chance, area hit, knockback, cooldown, effect and stacking");
    }
}
