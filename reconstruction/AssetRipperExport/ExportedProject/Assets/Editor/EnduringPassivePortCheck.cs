using System;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
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

public static class EnduringPassivePortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("EnduringPassivePortCheck: " + message); }
    static void Near(float actual, float expected, string message) => Require(Mathf.Abs(actual - expected) < .0001f, message + ": " + actual);
    public static void Run()
    {
        var root = new GameObject("Enduring test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory();
        inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory();
        var passive = new PassiveAbilityEnduring(); inventory.passiveAbility = passive;
        inventory.playerStats = new PlayerStatsNew(inventory, true); inventory.playerXp = new PlayerXp();
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string text = passive.GetDescription(new LocalizedString("PassiveAbilities", "ENDURING_DESC"));
            Require(text.Contains("0.75%") && text.Contains("frozen enemy") && !text.Contains("{"), "localized description: " + text);
            float baseSize = inventory.playerStats.GetRawStat(EStat.SizeMultiplier), baseDamage = inventory.playerStats.GetRawStat(EStat.DamageMultiplier);
            DebuffIce.numFrozenEnemies = 5; passive.Init(); Require(DebuffIce.numFrozenEnemies == 0, "init resets frozen count");
            XpUtility.Init(); inventory.playerXp.AddXp(XpUtility.LevelToXp(3)); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.SizeMultiplier), baseSize + .0225f, "size per level");
            PlayerXp.A_LevelUp?.Invoke(1000); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.SizeMultiplier), baseSize + 4f, "size cap");
            DebuffIce.numFrozenEnemies = 2; passive.Tick(); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseDamage * 1.03f, "frozen enemies multiply damage");
            DebuffIce.numFrozenEnemies = 200; passive.Tick(); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseDamage * 3f, "frozen damage cap");
            GameManager.A_StageStarted?.Invoke(); passive.Tick(); inventory.playerStats.Tick();
            Require(DebuffIce.numFrozenEnemies == 0, "stage resets frozen count");
            Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseDamage, "stage reset clears damage multiplier");
            passive.Cleanup(); DebuffIce.numFrozenEnemies = 7; GameManager.A_StageStarted?.Invoke();
            Require(DebuffIce.numFrozenEnemies == 7, "cleanup removes stage subscription");
            Debug.Log("EnduringPassivePortCheck passed: level size, frozen damage, caps and stage reset");
        }
        finally { passive.Cleanup(); DebuffIce.numFrozenEnemies = 0; inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; }
    }
}
