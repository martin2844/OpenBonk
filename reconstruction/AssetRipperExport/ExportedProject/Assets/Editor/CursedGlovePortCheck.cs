using System;
using System.Collections.Generic;
using System.Linq;
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

public static class CursedGlovePortCheck
{
    sealed class FixedRandom : System.Random { readonly double value; public FixedRandom(double value) { this.value = value; } public override double NextDouble() => value; }
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("CursedGlovePortCheck: " + message); }
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
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
        EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>(); EffectManager.Instance.gloveCurse = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GlovesCursed.prefab");
        Require(EffectManager.Instance.gloveCurse != null, "original effect prefab");
        var near = Target(manager, "near", Vector3.zero); var nearby = Target(manager, "nearby", new Vector3(3f, 0f, 0f)); var far = Target(manager, "far", new Vector3(6f, 0f, 0f));
        typeof(Enemy).GetField("armor", Private).SetValue(near, .5f);
        var inventory = player.inventory.itemInventory; inventory.AddItem(EItem.GloveCurse, 2); var glove = (ItemGlovesCursed)inventory.GetItem(EItem.GloveCurse);
        var hp = glove.statModifiers[EStat.MaxHealth].GetModifiers().Single(); var difficulty = glove.statModifiers[EStat.Difficulty].GetModifiers().Single();
        Require(hp.modifyType == EStatModifyType.Multiplication && Mathf.Abs(hp.modification - .64f) < .0001f && difficulty.modifyType == EStatModifyType.Flat && Mathf.Abs(difficulty.modification - .2f) < .0001f, "two-stack health and difficulty modifiers");
        Require(glove.HasOnHitEffectProc() && Mathf.Abs(Field<float>(glove, "procChance") - (.1f / 8.1f * .5f)) < .00001f && Field<int>(glove, "maxProcsPerTick") == 250, "original chance and tick cap");
        var hit = new DamageContainer(1f, "test") { enemy = near }; MyTime.time = 10f; Physics.SyncTransforms();
        global::Utility.MyRandom.random = new FixedRandom(.99); inventory.PostDamage(hit); Require(near.hp == 1000f && Field<int>(glove, "numProcsThisTick") == 0, "failed chance roll does not use tick cap");
        global::Utility.MyRandom.random = new FixedRandom(0); inventory.PostDamage(hit);
        Require(Mathf.Abs(near.hp - 995.75f) < .001f && Mathf.Abs(nearby.hp - 991.5f) < .001f && far.hp == 1000f, "four-unit area and per-target armor damage");
        var fx = Field<EffectPlayer>(glove, "fx"); Require(fx != null && fx.gameObject.activeSelf && fx.transform.position == near.transform.position, "original effect spawned at hit");
        typeof(ItemGlovesCursed).GetField("numProcsThisTick", Private).SetValue(glove, 249); inventory.PostDamage(hit); float after = nearby.hp;
        Require(Field<int>(glove, "numProcsThisTick") == 250 && after == 983f, "250th proc is allowed"); inventory.PostDamage(hit); Require(nearby.hp == after, "cap blocks further procs");
        MyTime.paused = false; inventory.Tick(); Require(Field<int>(glove, "numProcsThisTick") == 0, "inventory tick resets cap"); inventory.PostDamage(hit); Require(nearby.hp == 974.5f, "proc resumes on next tick");
        inventory.RemoveItem(EItem.GloveCurse); hp = glove.statModifiers[EStat.MaxHealth].GetModifiers().Single(); Require(Mathf.Abs(hp.modification - .8f) < .0001f, "removal updates health multiplier");
        inventory.RemoveItem(EItem.GloveCurse); Require(inventory.GetItem(EItem.GloveCurse) == null, "last removal unregisters item");
        Debug.Log("CursedGlovePortCheck passed: stat modifiers, chance, area damage, armor and tick cap");
    }
}
