using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
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

public static class WallClimbPassivePortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("WallClimbPassivePortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("passive test state"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        GameManager.Instance = root.AddComponent<GameManager>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = ScriptableObject.CreateInstance<CharacterData>(); inventory.characterData.statModifiers = new List<StatModifier>();
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory();
        inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory();
        var passive = new PassiveAbilityWallClimb(); inventory.passiveAbility = passive;
        inventory.playerStats = new PlayerStatsNew(inventory, true); inventory.playerXp = new PlayerXp();
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string description = passive.GetDescription(new LocalizedString("PassiveAbilities", "WALL_CLIMB_DESC"));
            Require(description == "Climb up walls. Also gain +2 Max HP per level.", "original localized description: " + description);
            XpUtility.Init(); passive.Init();
            Require(inventory.playerStats.GetStat(EStat.MaxHealth) == 100f, "base health");
            inventory.playerXp.AddXp(XpUtility.LevelToXp(1)); inventory.playerStats.Tick();
            Require(inventory.playerXp.level == 1 && inventory.playerStats.GetStat(EStat.MaxHealth) == 102f, "first level raises max health by two");
            inventory.playerXp.AddXp(XpUtility.LevelToXp(3) - inventory.playerXp.xp); inventory.playerStats.Tick();
            Require(inventory.playerXp.level == 3 && inventory.playerStats.GetStat(EStat.MaxHealth) == 106f, "later levels replace the bonus with two per level");
            passive.Cleanup(); inventory.playerXp.AddXp(XpUtility.LevelToXp(4) - inventory.playerXp.xp); inventory.playerStats.Tick();
            Require(inventory.playerXp.level == 4 && inventory.playerStats.GetStat(EStat.MaxHealth) == 106f, "cleanup removes level-up subscription");
            Debug.Log("WallClimbPassivePortCheck passed: XP events, max-health stat and localized description");
        }
        finally
        {
            passive.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup();
            UnityEngine.Object.DestroyImmediate(inventory.characterData); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null;
        }
    }
}
