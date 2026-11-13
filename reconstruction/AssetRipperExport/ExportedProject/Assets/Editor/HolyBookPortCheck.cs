using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

public static class HolyBookPortCheck
{
    static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool ok, string reason) { if (!ok) throw new Exception("HolyBookPortCheck: " + reason); }
    static Enemy Target(EnemyManager manager, string name, float x)
    {
        var obj = new GameObject(name); obj.layer = LayerMask.NameToLayer("Enemy"); obj.transform.position = Vector3.right * x;
        var collider = obj.AddComponent<SphereCollider>(); var enemy = obj.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 100f;
        typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 100f);
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        manager.collidersToEnemies.Add(collider, enemy); return enemy;
    }
    public static void Run()
    {
        var oldGame = GameManager.Instance; var oldPlayer = MyPlayer.Instance; var oldEnemy = EnemyManager.Instance; var oldEffect = EffectManager.Instance; float oldTime = MyTime.time; bool oldPaused = MyTime.paused;
        var gameObj = new GameObject("holy game"); var playerObj = new GameObject("holy player"); var managerObj = new GameObject("holy manager"); var effectObj = new GameObject("holy visual"); effectObj.SetActive(false);
        Enemy near = null, far = null; PlayerInventory inv = null; CharacterData character = null; HolyBookEffect visual = null;
        try
        {
            var game = gameObj.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
            var manager = managerObj.AddComponent<EnemyManager>(); EnemyManager.Instance = manager; EffectManager.Instance = null;
            var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player; typeof(MyPlayer).GetField("baseDamage", Private).SetValue(player, 10f);
            inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            character = ScriptableObject.CreateInstance<CharacterData>(); character.statModifiers = new List<StatModifier>(); inv.characterData = character;
            inv.tomeInventory = new TomeInventory(); inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects(); inv.statInventory = new StatInventory();
            inv.playerStats = new PlayerStatsNew(inv, true);
            visual = effectObj.AddComponent<HolyBookEffect>(); var fx = effectObj.AddComponent<EffectPlayer>(); fx.ps = effectObj.AddComponent<ParticleSystem>(); visual.effectPlayer = fx;
            typeof(HolyBookEffect).GetMethod("Awake", Private).Invoke(visual, null); int plays = 0; fx.A_Played += () => plays++;
            near = Target(manager, "holy near", 6f); far = Target(manager, "holy far", 10f); Physics.SyncTransforms();
            inv.itemInventory.AddItem(EItem.HolyBook, 2); Require(effectObj.activeSelf, "book enables effect"); inv.playerStats.Tick();
            Require(inv.playerStats.GetStat(EStat.MaxHealth) == 300f && inv.playerStats.GetStat(EStat.HealthRegen) == 110f && Mathf.Abs(inv.playerStats.GetStat(EStat.Overheal) - .5f) < .001f, "two stacks add original stats");
            inv.playerHealth = new PlayerHealth(inv.playerStats); inv.playerHealth.hp = 50; MyTime.paused = false; MyTime.time = 10f;
            PlayerHealth.A_Heal?.Invoke(inv.playerHealth, 20f, true); inv.itemInventory.Tick(); Require(near.hp == 100f && plays == 0, "shield healing does not trigger damage");
            Require(inv.playerHealth.Heal(4, false) == 4, "real health healing succeeds"); inv.itemInventory.Tick();
            Require(Mathf.Abs(near.hp - 80f) < .001f && far.hp == 100f && plays == 1 && Mathf.Abs(fx.transform.localScale.x - 7f) < .001f, "healing damages nearby enemy and plays radius effect");
            inv.playerHealth.Heal(2, false); MyTime.time = 11f; inv.itemInventory.Tick(); Require(near.hp == 80f && plays == 1, "cooldown retains healing");
            MyTime.time = 11.5f; inv.itemInventory.Tick(); Require(Mathf.Abs(near.hp - 70f) < .001f && plays == 2, "retained healing triggers at cooldown boundary");
            inv.itemInventory.RemoveItem(EItem.HolyBook); inv.itemInventory.RemoveItem(EItem.HolyBook); inv.playerStats.Tick();
            Require(inv.playerStats.GetStat(EStat.MaxHealth) == 100f && inv.playerStats.GetStat(EStat.HealthRegen) == 10f, "last removal restores health stats");
            float prior = near.hp; inv.playerHealth.Heal(1, false); MyTime.time = 20f; inv.itemInventory.Tick(); Require(near.hp == prior && plays == 2, "last removal unsubscribes heal event");
            Debug.Log("HolyBookPortCheck passed: inventory stats, healing damage, radius, cooldown and cleanup");
        }
        finally
        {
            if (visual != null) typeof(HolyBookEffect).GetMethod("OnDestroy", Private).Invoke(visual, null);
            inv?.playerHealth?.OnDestroy(); inv?.playerStats?.OnDestroy(); inv?.statusEffects?.OnDestroy(); inv?.itemInventory?.Cleanup();
            if (near != null) { UnityEngine.Object.DestroyImmediate(near.enemyData); UnityEngine.Object.DestroyImmediate(near.gameObject); }
            if (far != null) { UnityEngine.Object.DestroyImmediate(far.enemyData); UnityEngine.Object.DestroyImmediate(far.gameObject); }
            UnityEngine.Object.DestroyImmediate(character); UnityEngine.Object.DestroyImmediate(effectObj); UnityEngine.Object.DestroyImmediate(playerObj); UnityEngine.Object.DestroyImmediate(managerObj); UnityEngine.Object.DestroyImmediate(gameObj);
            GameManager.Instance = oldGame; MyPlayer.Instance = oldPlayer; EnemyManager.Instance = oldEnemy; EffectManager.Instance = oldEffect; MyTime.time = oldTime; MyTime.paused = oldPaused;
        }
    }
}
