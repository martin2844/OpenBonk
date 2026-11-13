using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;

public static class ScarfItemPortCheck
{
    static void Near(float actual, float expected, string label) { if (Mathf.Abs(actual - expected) > .0001f) throw new Exception(label + ": " + actual + " expected " + expected); }
    public static void Run()
    {
        var root = new GameObject("scarf item check"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.playerMovement = root.AddComponent<PlayerMovement>(); player.playerMovement.grounded = true;
        GameManager.Instance = root.AddComponent<GameManager>();
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 1 };
        inventory.playerStats = new PlayerStatsNew(inventory, true);
        try
        {
            var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Scarf.asset");
            if (asset == null || asset.eItem != EItem.Scarf) throw new Exception("original Scarf asset");
            ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data)).Add(EItem.Scarf, asset);
            float baseline = inventory.playerStats.GetStat(EStat.DamageMultiplier);
            var damage = typeof(WeaponUtility).GetMethod("GetNewDamage", BindingFlags.Static | BindingFlags.NonPublic);
            Action<float, string> check = (bonus, label) => { inventory.playerStats.Tick(); Near(inventory.playerStats.GetStat(EStat.DamageMultiplier), baseline + bonus, label); Near((float)damage.Invoke(null, new object[] { 100f, new StatComponents() }), 100f * (baseline + bonus), label + " weapon damage"); };
            inventory.itemInventory.AddItem(EItem.Scarf); check(0, "grounded");
            player.playerMovement.grounded = false; PlayerMovement.A_Grounded?.Invoke(false); check(.5f, "airborne");
            inventory.itemInventory.AddItem(EItem.Scarf); check(1f, "two airborne");
            player.playerMovement.grounded = true; PlayerMovement.A_Grounded?.Invoke(true); check(0, "landed");
            player.playerMovement.grounded = false; PlayerMovement.A_Grounded?.Invoke(false); check(1f, "airborne again");
            inventory.itemInventory.RemoveItem(EItem.Scarf); check(.5f, "one remaining");
            inventory.itemInventory.RemoveItem(EItem.Scarf); check(0, "removed");
            PlayerMovement.A_Grounded?.Invoke(true); PlayerMovement.A_Grounded?.Invoke(false); check(0, "unsubscribed");
            Debug.Log("ScarfItemPortCheck passed: original item, grounded events, stacked airborne damage, removal and weapon calculation");
        }
        finally { inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; }
    }
}
