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

public static class PoisonGlovePortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("PoisonGlovePortCheck: " + message); }
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
        EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>(); EffectManager.Instance.glovePoison = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PoisonGlovesFx.prefab");
        Require(EffectManager.Instance.glovePoison != null, "original effect prefab");
        var near = Target(manager, "near", Vector3.zero); var nearby = Target(manager, "nearby", new Vector3(14f, 0f, 0f)); var far = Target(manager, "far", new Vector3(17f, 0f, 0f));
        var inventory = player.inventory.itemInventory; inventory.AddItem(EItem.GlovePoison, 2); var glove = (ItemGlovesPoison)inventory.GetItem(EItem.GlovePoison);
        Require(glove.HasOnHitEffectProc(), "inventory registers original on-hit proc"); MyTime.time = 10f; Physics.SyncTransforms();
        var hit = new DamageContainer(1f, "test") { enemy = near }; inventory.PostDamage(hit);
        Require(Mathf.Abs(near.hp - 985f) < .001f && Mathf.Abs(nearby.hp - 985f) < .001f && far.hp == 1000f, "two gloves damage enemies within fifteen units");
        var poison = near.debuffsToAdd[EDebuff.Poison]; Require(poison.stacks == 10 && poison.duration == 5f && nearby.debuffsToAdd[EDebuff.Poison].stacks == 10, "original poison duration and fixed stack count");
        near.debuffsToAdd.Clear(); typeof(Enemy).GetMethod("AddDebuffImplementation", Private).Invoke(near, new object[] { poison });
        Require(near.HasDebuff(EDebuff.Poison), "poison becomes active"); near.DebuffTick(); Require(near.hp == 975f, "poison tick damages enemy");
        Require(Mathf.Abs(glove.readyAtTime - 18.5f) < .001f, "eight-and-a-half-second cooldown");
        var fx = (EffectPlayer)typeof(ItemGlovesPoison).GetField("fx", Private).GetValue(glove); Require(fx != null && fx.gameObject.activeSelf && fx.transform.position == near.transform.position, "original effect spawned at hit");
        MyTime.time = 18.4f; inventory.PostDamage(hit); Require(nearby.hp == 985f, "cooldown blocks repeats");
        near.hp = 0f; MyTime.time = 18.5f; inventory.PostDamage(hit); Require(nearby.hp == 970f && glove.readyAtTime == 27f, "dead struck enemy still triggers nearby poison at cooldown boundary");
        near.hp = 975f; near.GetComponent<Collider>().enabled = false; nearby.GetComponent<Collider>().enabled = false; far.GetComponent<Collider>().enabled = false; Physics.SyncTransforms();
        MyTime.time = 28f; inventory.PostDamage(hit); Require(Mathf.Abs(glove.readyAtTime - 28.02f) < .001f, "empty scan uses short retry delay");
        var dataManager = new GameObject("item data").AddComponent<DataManager>(); DataManager.Instance = dataManager;
        var itemData = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", Private).GetValue(dataManager);
        itemData.Add(EItem.GlovePoison, AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Glove Poison.asset"));
        itemData.Add(EItem.GloveLightning, AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Glove Lightning.asset"));
        EffectManager.Instance.gloveLightning = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/LightningGloveFx.prefab");
        near.GetComponent<Collider>().enabled = true; nearby.GetComponent<Collider>().enabled = true; near.hp = nearby.hp = 1000f;
        typeof(Enemy).GetField("controlHp", Private).SetValue(near, 1000f); typeof(Enemy).GetField("controlHp", Private).SetValue(nearby, 1000f);
        inventory.AddItem(EItem.GloveLightning); MyTime.time = 40f; Physics.SyncTransforms(); inventory.PostDamage(hit);
        Require(near.hp == 970f && nearby.hp == 985f && near.debuffsToAdd.ContainsKey(EDebuff.Stun) && near.debuffsToAdd.ContainsKey(EDebuff.Poison), "both glove procs preserve target and apply their distinct radius effects");
        inventory.RemoveItem(EItem.GloveLightning);
        inventory.RemoveItem(EItem.GlovePoison); inventory.RemoveItem(EItem.GlovePoison); Require(inventory.GetItem(EItem.GlovePoison) == null, "last removal unregisters item");
        Debug.Log("PoisonGlovePortCheck passed: inventory proc, area damage, poison tick, cooldown and effect prefab");
    }
}
