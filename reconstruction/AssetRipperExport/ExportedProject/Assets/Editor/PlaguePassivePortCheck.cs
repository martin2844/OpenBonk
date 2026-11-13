using System;
using System.Runtime.Serialization;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class PlaguePassivePortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("PlaguePassivePortCheck: " + message); }
    static Enemy Target(EnemyManager manager, string name, Vector3 position)
    {
        var obj = new GameObject(name); obj.layer = LayerMask.NameToLayer("Enemy"); obj.transform.position = position;
        var collider = obj.AddComponent<SphereCollider>(); var enemy = obj.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 100f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 100f);
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = obj.AddComponent<Rigidbody>(); enemy.enemyMovement = obj.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = enemy.rb; enemy.enemyMovement.enemy = enemy;
        enemy.renderer = obj.AddComponent<MeshRenderer>(); enemy.dissolve = obj.AddComponent<EnemyDissolve>(); enemy.dissolve.enemyRenderer = enemy.renderer; enemy.dissolve.Reset();
        manager.collidersToEnemies.Add(collider, enemy); return enemy;
    }
    public static void Run()
    {
        var root = new GameObject("Plague test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        GameManager.Instance.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var manager = root.AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
        var pools = root.AddComponent<PoolManager>(); PoolManager.Instance = pools;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PlagueFx.prefab"); Require(prefab != null, "original plague effect prefab");
        pools.poisonCloud = prefab;
        typeof(PoolManager).GetMethod("InitializePoisonPool", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pools, null);
        GameObject effect = null;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory();
        inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 10 };
        var passive = new PassiveAbilityPlague(); inventory.passiveAbility = passive; inventory.playerStats = new PlayerStatsNew(inventory, true);
        var dying = Target(manager, "dying", Vector3.zero); dying.gameObject.layer = LayerMask.NameToLayer("Default");
        var near = Target(manager, "near", Vector3.right * 3f); var far = Target(manager, "far", Vector3.right * 8f);
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string description = passive.GetDescription(new LocalizedString("PassiveAbilities", "PLAGUE_DESC"));
            Require(description.Contains("1%") && description.Contains("poison") && !description.Contains("{"), "localized description: " + description);
            passive.Init(); float baseDamage = inventory.playerStats.GetRawStat(EStat.PoisonDamageMultiplier);
            PlayerXp.A_LevelUp?.Invoke(10); inventory.playerStats.Tick();
            Require(Mathf.Abs(inventory.playerStats.GetRawStat(EStat.PoisonDamageMultiplier) - baseDamage - .1f) < .0001f, "poison damage per level");
            Physics.SyncTransforms(); MyTime.time = 10f; Enemy.A_EnemyDied?.Invoke(dying, new DamageContainer(0f, "test"));
            Require(near.debuffsToAdd.Count == 0 && effect == null, "unpoisoned death does not spread");
            dying.debuffs.Add(EDebuff.Poison, null); MyTime.time = 11f;
            dying.DamageFromPlayerOther(new DamageContainer(0f, "test") { enemy = dying, damage = 100f });
            Require(dying.hp == 0f && dying.gameObject.layer == LayerMask.NameToLayer("Default"), "real damage invokes enemy death");
            effect = GameObject.Find(prefab.name + "(Clone)");
            Require(near.debuffsToAdd.TryGetValue(EDebuff.Poison, out var poison) && poison.stacks == 3 && poison.duration == 4f, "nearby enemy receives level-scaled poison");
            Require(!far.debuffsToAdd.ContainsKey(EDebuff.Poison), "distant enemy is outside the spread radius");
            near.debuffsToAdd.Clear();
            typeof(Enemy).GetMethod("AddDebuffImplementation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(near, new object[] { poison });
            Require(near.HasDebuff(EDebuff.Poison), "spread poison becomes active");
            float expectedDamage = Mathf.Max(3f, 3f * inventory.playerStats.GetStat(EStat.DamageMultiplier) * inventory.playerStats.GetStat(EStat.PoisonDamageMultiplier));
            near.DebuffTick(); Require(Mathf.Abs(near.hp - (100f - expectedDamage)) < .0001f, "spread poison deals original tick damage");
            EnemyDebuff firstPoison = near.debuffs[EDebuff.Poison]; near.RemoveDebuff(EDebuff.Poison, false);
            near.AddDebuff(EDebuff.Poison, null, 2f, 1); var newPoison = near.debuffsToAdd[EDebuff.Poison]; near.debuffsToAdd.Clear();
            typeof(Enemy).GetMethod("AddDebuffImplementation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(near, new object[] { newPoison });
            Require(ReferenceEquals(firstPoison, near.debuffs[EDebuff.Poison]) && near.debuffs[EDebuff.Poison].GetStacks() == 1, "returned poison is reused with clean stacks");
            near.RemoveDebuff(EDebuff.Poison, false);
            DebuffFactory.Init(); GameManager.A_StageStarted?.Invoke(); DebuffFactory.Cleanup();
            var poolsField = typeof(DebuffFactory).GetField("debuffPools", BindingFlags.Static | BindingFlags.NonPublic);
            Require(((System.Collections.IDictionary)poolsField.GetValue(null)).Count == 0, "stage reset clears debuff pools");
            float size = inventory.playerStats.GetStat(EStat.SizeMultiplier);
            Require(effect != null && effect.transform.position == dying.transform.position && Mathf.Abs(effect.transform.localScale.x - 2f * size) < .0001f, "original visual is positioned and scaled");
            var returner = effect.GetComponent<ReturnToPool>(); Require(returner != null && returner.pool == pools.poisonPool, "effect is registered with original timeout pool");
            MyTime.time = 12.1f; typeof(ReturnToPool).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(returner, null);
            Require(!effect.activeSelf && pools.poisonPool.CountInactive == 1, "effect returns to pool after one second");
            passive.Cleanup(); near.debuffsToAdd.Clear(); MyTime.time = 12f; Enemy.A_EnemyDied?.Invoke(dying, new DamageContainer(0f, "test"));
            Require(near.debuffsToAdd.Count == 0, "cleanup removes death handler");
            Debug.Log("PlaguePassivePortCheck passed: real enemy death, poison tick, debuff reuse and stage reset, visual pool");
        }
        finally { passive.Cleanup(); near.ClearAllDebuffs(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); if (effect != null) UnityEngine.Object.DestroyImmediate(effect); UnityEngine.Object.DestroyImmediate(dying.enemyData); UnityEngine.Object.DestroyImmediate(near.enemyData); UnityEngine.Object.DestroyImmediate(far.enemyData); UnityEngine.Object.DestroyImmediate(dying.gameObject); UnityEngine.Object.DestroyImmediate(near.gameObject); UnityEngine.Object.DestroyImmediate(far.gameObject); UnityEngine.Object.DestroyImmediate(root); PoolManager.Instance = null; EnemyManager.Instance = null; MyPlayer.Instance = null; GameManager.Instance = null; }
    }
}
