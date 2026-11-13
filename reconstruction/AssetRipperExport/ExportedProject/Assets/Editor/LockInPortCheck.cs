using System;
using System.Linq;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;

public static class LockInPortCheck
{
    static void Near(float actual, float expected, string label) { if (Mathf.Abs(actual - expected) > .0001f) throw new Exception(label + ": " + actual + " expected " + expected); }
    static float Modifier(PassiveAbilityLockIn p, EStat stat) => p.statModifiers[stat].GetModifiers().Single().modification;
    public static void Run()
    {
        var root = new GameObject("Lock In test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 20 };
        var passive = new PassiveAbilityLockIn(); inventory.passiveAbility = passive; inventory.playerStats = new PlayerStatsNew(inventory, true);
        inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
        inventory.playerHealth.maxHp = 100; inventory.playerHealth.hp = 50;
        try
        {
            passive.Init(); PlayerXp.A_LevelUp?.Invoke(20); inventory.playerStats.Tick(); Near(Modifier(passive, EStat.Thorns), 40f, "level thorns");
            MyTime.time = 0f; passive.Tick(); inventory.playerStats.Tick(); Near(Modifier(passive, EStat.DamageMultiplier), .6f, "half health bonus");
            inventory.playerHealth.hp = 25; MyTime.time = .5f; passive.Tick(); Near(Modifier(passive, EStat.DamageMultiplier), .6f, "damage update cooldown");
            MyTime.time = 1f; passive.Tick(); Near(Modifier(passive, EStat.DamageMultiplier), .9f, "lower health bonus");
            inventory.playerHealth.hp = 100; MyTime.time = 2f; passive.Tick(); Near(Modifier(passive, EStat.DamageMultiplier), 0f, "full health");
            AegisAttack.A_Used?.Invoke(0); Near(Modifier(passive, EStat.AttackSpeed), 1.5f, "empty aegis attack speed");
            AegisAttack.A_Regen?.Invoke(1); Near(Modifier(passive, EStat.AttackSpeed), 0f, "regenerated aegis attack speed");
            passive.Cleanup(); AegisAttack.A_Used?.Invoke(0); PlayerXp.A_LevelUp?.Invoke(30);
            Near(Modifier(passive, EStat.AttackSpeed), 0f, "aegis cleanup"); Near(Modifier(passive, EStat.Thorns), 40f, "level cleanup");
            Debug.Log("LockInPortCheck passed: health scaling, cooldown, thorns, aegis and cleanup");
        }
        finally { passive.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; }
    }
}
