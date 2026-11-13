using System;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class FoxPassivePortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("FoxPassivePortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("Fox passive test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        GameManager.Instance = root.AddComponent<GameManager>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory();
        inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory();
        inventory.passiveAbility = PassiveAbilityFactory.CreatePassiveAbility(inventory.characterData.passive);
        inventory.playerStats = new PlayerStatsNew(inventory, true); inventory.playerXp = new PlayerXp();
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            inventory.characterData.passive.Init();
            string description = inventory.characterData.passive.GetDescription();
            Require(description == "Gain 1.5% Luck per level.", "original passive description: " + description);
            Require(inventory.HasPassive(EPassive.RngBlessing), "Fox asset creates RNG Blessing");
            float initialLuck = inventory.playerStats.GetStat(EStat.Luck);
            XpUtility.Init(); inventory.passiveAbility.Init();
            inventory.playerXp.AddXp(XpUtility.LevelToXp(3)); inventory.playerStats.Tick();
            Require(inventory.playerXp.level == 3 && Mathf.Abs(inventory.playerStats.GetStat(EStat.Luck) - (initialLuck + .045f)) < .00001f, "level three grants 4.5% luck");
            inventory.passiveAbility.Cleanup();
            inventory.playerXp.AddXp(XpUtility.LevelToXp(4) - inventory.playerXp.xp); inventory.playerStats.Tick();
            Require(inventory.playerXp.level == 4 && Mathf.Abs(inventory.playerStats.GetStat(EStat.Luck) - (initialLuck + .045f)) < .00001f, "cleanup removes subscription");
            Debug.Log("FoxPassivePortCheck passed: original Fox passive, XP, luck and localized description");
        }
        finally
        {
            inventory.passiveAbility.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup();
            UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null;
        }
    }
}
