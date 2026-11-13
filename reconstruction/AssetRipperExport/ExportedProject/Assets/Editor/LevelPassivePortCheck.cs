using System;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class LevelPassivePortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("LevelPassivePortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("level passive test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory();
        inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory();
        LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
        XpUtility.Init();
        var cases = new (PassiveAbility passive, EStat stat, float perLevel)[] {
            (new PassiveAbilityCritHappens(), EStat.CritChance, .01f), (new PassiveAbilityReinforced(), EStat.Armor, .01f),
            (new PassiveAbilityWarrior(), EStat.DamageMultiplier, .015f), (new PassiveAbilityCurse(), EStat.Difficulty, .01f),
            (new PassiveAbilityVampire(), EStat.Lifesteal, .01f), (new PassiveAbilityQuantumXp(), EStat.XpIncreaseMultiplier, .01f),
            (new PassiveAbilityFlowstate(), EStat.AttackSpeed, .01f)
        };
        try
        {
            foreach (var entry in cases)
            {
                inventory.passiveAbility = entry.passive; inventory.playerXp = new PlayerXp();
                inventory.playerStats = new PlayerStatsNew(inventory, true);
                float initial = inventory.playerStats.GetRawStat(entry.stat);
                entry.passive.Init(); inventory.playerXp.AddXp(XpUtility.LevelToXp(3)); inventory.playerStats.Tick();
                Require(inventory.playerXp.level == 3 && Mathf.Abs(inventory.playerStats.GetRawStat(entry.stat) - (initial + 3f * entry.perLevel)) < .0001f, entry.passive.GetType().Name + " stat");
                string expected = "Gain " + (entry.perLevel * 100f).ToString() + "% " + EnumUtility.EnumToReadable(entry.stat) + " per level.";
                string description = entry.passive.GetDescription(new LocalizedString("PassiveAbilities", "STAT_CHANGE_DESC"));
                Require(description == expected, entry.passive.GetType().Name + " description: " + description);
                entry.passive.Cleanup(); inventory.playerXp.AddXp(XpUtility.LevelToXp(4) - inventory.playerXp.xp); inventory.playerStats.Tick();
                Require(Mathf.Abs(inventory.playerStats.GetRawStat(entry.stat) - (initial + 3f * entry.perLevel)) < .0001f, entry.passive.GetType().Name + " cleanup");
                inventory.playerStats.OnDestroy();
            }
            Debug.Log("LevelPassivePortCheck passed: seven passives, XP stat updates, descriptions and cleanup");
        }
        finally { inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; }
    }
}
