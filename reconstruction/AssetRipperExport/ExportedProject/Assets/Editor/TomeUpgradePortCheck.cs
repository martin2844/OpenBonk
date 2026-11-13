using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts._Data.Tomes;
using UnityEngine;

public static class TomeUpgradePortCheck
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static void Run()
    {
        var manager = new GameObject("tome-check-data").AddComponent<DataManager>();
        DataManager.Instance = manager;
        var weaponSlot = ScriptableObject.CreateInstance<ShopItemData>();
        weaponSlot.eShopItem = EShopItem.Weapons;
        var tomeSlot = ScriptableObject.CreateInstance<ShopItemData>();
        tomeSlot.eShopItem = EShopItem.Tomes;
        manager.shopItems.Add(EShopItem.Weapons, weaponSlot);
        manager.shopItems.Add(EShopItem.Tomes, tomeSlot);
        var saves = new GameObject("tome-check-saves").AddComponent<SaveManager>();
        saves.Init();
        saves.progression = new ProgressionSaveFile();
        saves.progression.shopItems.Add(EShopItem.Weapons, 0);
        saves.progression.shopItems.Add(EShopItem.Tomes, 0);
        var data = ScriptableObject.CreateInstance<TomeData>();
        data.eTome = ETome.Damage;
        data.statModifier = new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = 0.1f };
        var dictionary = (Dictionary<ETome, TomeData>)typeof(DataManager).GetField("tomeData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
        dictionary.Add(data.eTome, data);
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        inv.tomeInventory = new TomeInventory();
        inv.characterData = ScriptableObject.CreateInstance<CharacterData>();
        inv.characterData.statModifiers = new List<StatModifier>();
        inv.itemInventory = new ItemInventory();
        inv.statusEffects = new PlayerStatusEffects();
        inv.statInventory = new StatInventory();
        inv.playerStats = new PlayerStatsNew(inv, true);
        Require(InventoryUtility.GetNumAvailableTomeSlots() == 2 && InventoryUtility.GetNumAvailableWeaponSlots() == 2, "base slots");
        saves.progression.shopItems[EShopItem.Weapons] = 1;
        saves.progression.shopItems[EShopItem.Tomes] = 2;
        Require(InventoryUtility.GetNumAvailableWeaponSlots() == 3 && InventoryUtility.GetNumAvailableTomeSlots() == 4, "saved slot upgrades");
        Require(data.GetMaxLevel() == 99, "tome max level");
        Require(Math.Abs(Rarity.GetMultiplier(ERarity.Legendary) - 2f) < 0.0001f, "rarity multiplier");
        var first = data.GetUpgradeOffer(ERarity.New);
        var second = data.GetUpgradeOffer(ERarity.Rare);
        Require(first.Count == 1 && ReferenceEquals(first[0], data.statModifier), "new offer");
        Require(second.Count == 1 && !ReferenceEquals(second[0], data.statModifier) && Math.Abs(second[0].modification - 0.14f) < 0.0001f, "rare offer");
        int events = 0;
        Action<ETome, EStat> onUpgrade = (t, s) => { if (t == data.eTome && s == data.statModifier.stat) events++; };
        TomeInventory.A_TomeUpgrade += onUpgrade;
        inv.AddUpgrade(data, first, ERarity.New);
        inv.AddUpgrade(data, second, ERarity.Rare);
        TomeInventory.A_TomeUpgrade -= onUpgrade;
        Require(inv.tomeInventory.GetTomeLevel(data.eTome) == 2 && inv.tomeInventory.GetNumTomes() == 1, "tome levels");
        Require(inv.tomeInventory.statToTomes[EStat.DamageMultiplier].Contains(data.eTome), "stat-to-tome mapping");
        Require(Math.Abs(inv.tomeInventory.tomeUpgrade[data.eTome].modification - 0.24f) < 0.0001f && events == 2, "accumulated stat and events");
        inv.playerStats.ForceUpdateStats();
        Require(Math.Abs(inv.playerStats.GetStat(EStat.DamageMultiplier) - 1.24f) < 0.0001f, "player damage stat update");
        Require(!inv.tomeInventory.IsMaxed(), "unfilled tome slots");
        inv.playerStats.OnDestroy();
        inv.statusEffects.OnDestroy();
        inv.itemInventory.Cleanup();
        var balance = ScriptableObject.CreateInstance<TomeData>();
        balance.eTome = ETome.Balance;
        Require(balance.GetUpgradeOffer(ERarity.Legendary).Count == 0, "balance special offer");
        Debug.Log("TomeUpgradePortCheck passed: rarity, saved slots, offers, dispatch and player stat update");
    }
}
