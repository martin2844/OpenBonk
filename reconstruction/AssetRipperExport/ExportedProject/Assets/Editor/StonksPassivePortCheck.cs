using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class StonksPassivePortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool condition, string message) { if (!condition) throw new Exception("StonksPassivePortCheck: " + message); }
    static void Gold(PlayerInventory inventory, float value) => typeof(PlayerInventory).GetField("<gold>k__BackingField", Private).SetValue(inventory, value);
    static float Damage(PassiveAbilityStonks passive) => (float)typeof(PassiveAbilityStonks).GetMethod("GetDamage", Private).Invoke(passive, null);
    static void Near(float actual, float expected, string message) => Require(Mathf.Abs(actual - expected) < .0002f, message + ": " + actual);
    public static void Run()
    {
        var root = new GameObject("Stonks test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory();
        inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory();
        var passive = new PassiveAbilityStonks(); inventory.passiveAbility = passive;
        inventory.playerStats = new PlayerStatsNew(inventory, true); inventory.playerXp = new PlayerXp();
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string description = passive.GetDescription(new LocalizedString("PassiveAbilities", "STONKS_DESC"));
            Require(!description.Contains("{") && description.Contains("1%") && description.Contains("Gold") && description.Contains("Damage"), "localized description: " + description);
            Gold(inventory, 10000); Near(Damage(passive), 2f, "10k tier");
            Gold(inventory, 200000); Near(Damage(passive), 18.15f, "200k tier");
            Gold(inventory, 1000000); Near(Damage(passive), 66.15f, "1m tier");
            Gold(inventory, 2000000); Near(Damage(passive), 101.15f, "above 1m tier");
            float baseDamage = inventory.playerStats.GetRawStat(EStat.DamageMultiplier);
            Gold(inventory, 10000); MyTime.time = 10f; passive.Init(); passive.Tick(); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseDamage * 3f, "gold changes damage stat");
            Gold(inventory, 0); MyTime.time = 10.5f; passive.Tick(); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseDamage * 3f, "one second update delay");
            MyTime.time = 11f; passive.Tick(); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseDamage, "spending gold removes bonus");
            XpUtility.Init(); inventory.playerXp.AddXp(XpUtility.LevelToXp(3)); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.GoldIncreaseMultiplier), 1.03f, "gold income per level");
            passive.Cleanup();
            Debug.Log("StonksPassivePortCheck passed: tier thresholds, update delay, spending, XP and description");
        }
        finally { passive.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; }
    }
}
