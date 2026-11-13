using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

public static class ToxicBarrelPortCheck
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string reason) { if (!ok) throw new Exception("ToxicBarrelPortCheck: " + reason); }
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
        var oldGame = GameManager.Instance; var oldPlayer = MyPlayer.Instance; var oldEnemies = EnemyManager.Instance; var oldEffect = EffectManager.Instance; float oldTime = MyTime.time;
        var gameObj = new GameObject("toxic game"); var playerObj = new GameObject("toxic player"); var managerObj = new GameObject("toxic manager"); var effectObj = new GameObject("toxic visual"); effectObj.SetActive(false);
        Enemy near = null, far = null; PlayerInventory inv = null; ToxicBarrelEffect visual = null;
        try
        {
            var game = gameObj.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
            var manager = managerObj.AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
            var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects();
            inv.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); inv.playerStats.stats = new Dictionary<EStat, float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) inv.playerStats.stats[stat] = 1f;
            inv.playerStats.stats[EStat.MaxHealth] = 100f; inv.playerStats.stats[EStat.Shield] = 0f; inv.playerStats.stats[EStat.Armor] = 0f; inv.playerStats.stats[EStat.DamageReductionMultiplier] = 0f; inv.playerStats.stats[EStat.Evasion] = 0f;
            inv.playerHealth = new PlayerHealth(inv.playerStats);
            visual = effectObj.AddComponent<ToxicBarrelEffect>(); var effectPlayer = effectObj.AddComponent<EffectPlayer>(); effectPlayer.ps = effectObj.AddComponent<ParticleSystem>(); visual.effectPlayer = effectPlayer;
            typeof(ToxicBarrelEffect).GetMethod("Awake", Private).Invoke(visual, null);
            int visualPlays = 0; effectPlayer.A_Played += () => visualPlays++;
            near = Target(manager, "toxic near", 8f); far = Target(manager, "toxic far", 12f); Physics.SyncTransforms();
            inv.itemInventory.AddItem(EItem.ToxicBarrel, 2); Require(effectObj.activeSelf, "item addition enables effect");
            var item = (ItemToxicBarrel)inv.itemInventory.GetItem(EItem.ToxicBarrel);
            MyTime.time = 10f; inv.playerHealth.DamagePlayerExternal(5, 0, Vector3.zero, true, "fixture", DcFlags.BypassAll);
            bool hasPoison = near.debuffsToAdd.TryGetValue(EDebuff.Poison, out var poison);
            Require(inv.playerHealth.hp == 95 && hasPoison, "real hit queues nearby poison");
            Require(poison.stacks == 10 && poison.duration == 5f && poison.dc == null && !far.debuffsToAdd.ContainsKey(EDebuff.Poison), "two stacks apply ten poison inside radius nine");
            Require(visualPlays == 1 && Mathf.Abs(effectPlayer.transform.localScale.x - 9f) < .001f, "effect receives scaled radius");
            near.debuffsToAdd.Clear(); MyTime.time = 10.1f; inv.playerHealth.DamagePlayerExternal(5, 0, Vector3.zero, true, "fixture", DcFlags.BypassAll);
            Require(!near.debuffsToAdd.ContainsKey(EDebuff.Poison) && visualPlays == 1, "cooldown blocks repeat");
            MyTime.time = 10.25f; inv.playerHealth.DamagePlayerExternal(5, 0, Vector3.zero, true, "fixture", DcFlags.BypassAll);
            Require(near.debuffsToAdd.ContainsKey(EDebuff.Poison) && visualPlays == 2, "cooldown boundary allows repeat");
            typeof(Enemy).GetMethod("AddDebuffImplementation", Private).Invoke(near, new object[] { near.debuffsToAdd[EDebuff.Poison] }); near.debuffsToAdd.Clear();
            Require(near.HasDebuff(EDebuff.Poison) && near.debuffs[EDebuff.Poison].GetStacks() == 10, "queued poison activates");
            inv.itemInventory.RemoveItem(EItem.ToxicBarrel); inv.itemInventory.RemoveItem(EItem.ToxicBarrel);
            near.debuffsToAdd.Clear(); MyTime.time = 11f; inv.playerHealth.DamagePlayerExternal(5, 0, Vector3.zero, true, "fixture", DcFlags.BypassAll);
            Require(!near.debuffsToAdd.ContainsKey(EDebuff.Poison) && visualPlays == 2, "last removal unsubscribes damage handler");
            Debug.Log("ToxicBarrelPortCheck passed: damage event, poison, radius, cooldown, effect and removal");
        }
        finally
        {
            if (visual != null) typeof(ToxicBarrelEffect).GetMethod("OnDestroy", Private).Invoke(visual, null);
            if (near != null) { near.ClearAllDebuffs(); UnityEngine.Object.DestroyImmediate(near.enemyData); UnityEngine.Object.DestroyImmediate(near.gameObject); }
            if (far != null) { UnityEngine.Object.DestroyImmediate(far.enemyData); UnityEngine.Object.DestroyImmediate(far.gameObject); }
            inv?.playerHealth?.OnDestroy(); inv?.statusEffects?.OnDestroy(); inv?.itemInventory?.Cleanup();
            UnityEngine.Object.DestroyImmediate(effectObj); UnityEngine.Object.DestroyImmediate(playerObj); UnityEngine.Object.DestroyImmediate(managerObj); UnityEngine.Object.DestroyImmediate(gameObj);
            GameManager.Instance = oldGame; MyPlayer.Instance = oldPlayer; EnemyManager.Instance = oldEnemies; EffectManager.Instance = oldEffect; MyTime.time = oldTime;
        }
    }
}
