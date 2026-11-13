using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class ScarfPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("ScarfPortCheck: " + message); }
    static void Near(float actual, float expected, string message) => Require(Mathf.Abs(actual - expected) < .0001f, message + ": " + actual + " expected " + expected);
    public static void Run()
    {
        var root = new GameObject("scarf test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerMovement = root.AddComponent<PlayerMovement>(); player.playerMovement.grounded = true;
        GameManager.Instance = root.AddComponent<GameManager>(); var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 1 }; inventory.playerStats = new PlayerStatsNew(inventory, true);
        var itemData = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
        var scarf = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Scarf.asset"); Require(scarf != null, "original asset"); itemData.Add(scarf.eItem, scarf);
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            var description = scarf.GetDummyItem().GetDescription(scarf.localizedDescription); Require(description.Contains("50%") && !description.Contains("{"), "description: " + description);
            inventory.itemInventory.AddItem(EItem.Scarf, 2); inventory.playerStats.Tick(); Near(inventory.playerStats.GetStat(EStat.DamageMultiplier), 1f, "grounded");
            typeof(PlayerMovement).GetMethod("StopGrounded", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player.playerMovement, null); inventory.playerStats.Tick(); Near(inventory.playerStats.GetStat(EStat.DamageMultiplier), 2f, "airborne two-stack bonus");
            inventory.itemInventory.RemoveItem(EItem.Scarf, false); inventory.playerStats.Tick(); Near(inventory.playerStats.GetStat(EStat.DamageMultiplier), 1.5f, "one stack removed in air");
            typeof(PlayerMovement).GetMethod("StartGrounded", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player.playerMovement, null); inventory.playerStats.Tick(); Near(inventory.playerStats.GetStat(EStat.DamageMultiplier), 1f, "landing removes bonus");
            inventory.itemInventory.RemoveItem(EItem.Scarf, false); inventory.playerStats.Tick(); typeof(PlayerMovement).GetMethod("StopGrounded", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player.playerMovement, null); inventory.playerStats.Tick(); Near(inventory.playerStats.GetStat(EStat.DamageMultiplier), 1f, "removed scarf unsubscribed");
            inventory.itemInventory.AddItem(EItem.Scarf); inventory.playerStats.Tick(); Near(inventory.playerStats.GetStat(EStat.DamageMultiplier), 1.5f, "acquired while airborne");
            Debug.Log("ScarfPortCheck passed: original description, actual grounded events, stacking, removal and airborne acquisition");
        }
        finally { inventory.itemInventory.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; }
    }
}
