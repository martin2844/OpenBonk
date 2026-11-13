using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using UnityEngine;
using Utility;

public static class WeaponOfferPortCheck
{
    private static void Require(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
    }
    private static void Close(float actual, float expected, string name)
    {
        Require(Math.Abs(actual - expected) < 0.0001f, name + ": " + actual + " != " + expected);
    }
    public static void Run()
    {
        float[] baseWeights = { 1.5f, 0.3f, 0.15f, 0.04f, 0.0085f };
        var noLuck = (float[])baseWeights.Clone();
        Rarity.CalculateRarityWeights(noLuck, 0f);
        float total = 1.9985f;
        for (int i = 0; i < noLuck.Length; i++) Close(noLuck[i], baseWeights[i] / total, "zero-luck weight " + i);
        var lucky = (float[])baseWeights.Clone();
        Rarity.CalculateRarityWeights(lucky, 9f);
        Require(lucky[4] > noLuck[4] && lucky[0] < noLuck[0], "luck changes rarity distribution");
        for (int seed = 0; seed < 200; seed++)
        {
            UnityEngine.Random.InitState(seed);
            float roll = UnityEngine.Random.Range(0f, 1f);
            float cumulative = 0f;
            ERarity expected = ERarity.Common;
            for (int i = 0; i < noLuck.Length; i++) { cumulative += noLuck[i]; if (cumulative >= roll) { expected = (ERarity)(i + 1); break; } }
            UnityEngine.Random.InitState(seed);
            Require(Rarity.GetUpgradeOfferRarity(0f) == expected, "weighted rarity seed " + seed);
        }
        var player = new GameObject("offer-test-player").AddComponent<MyPlayer>();
        MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory.itemInventory = new ItemInventory();
        player.inventory.weaponInventory = new WeaponInventory();
        var data = ScriptableObject.CreateInstance<UpgradeData>();
        data.upgradeModifiers = new List<StatModifier>
        {
            new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Flat, modification = 1f },
            new StatModifier { stat = EStat.AttackSpeed, modifyType = EStatModifyType.Addition, modification = 0.1f },
            new StatModifier { stat = EStat.SizeMultiplier, modifyType = EStatModifyType.Addition, modification = 0.2f },
            new StatModifier { stat = EStat.KnockbackMultiplier, modifyType = EStatModifyType.Flat, modification = 2f }
        };
        for (int seed = 0; seed < 32; seed++)
        {
            UnityEngine.Random.InitState(seed);
            int expectedCount = UnityEngine.Random.Range(0, 2) == 1 ? 2 : 1;
            UnityEngine.Random.InitState(seed);
            MyRandom.random = new System.Random(seed);
            Require(data.GetUpgradeOffer(ERarity.Common, EWeapon.FireStaff).Count == expectedCount, "common count seed " + seed);
        }
        MyRandom.random = new System.Random(11);
        var rare = data.GetUpgradeOffer(ERarity.Rare, EWeapon.FireStaff);
        Require(rare.Count == 2 && rare[0].stat != rare[1].stat, "rare selects two distinct modifiers");
        foreach (var selected in rare)
        {
            var original = data.upgradeModifiers.Find(m => m.stat == selected.stat);
            Require(!ReferenceEquals(selected, original) && selected.modifyType == original.modifyType, "offer is a copy");
            Close(selected.modification, (float)Math.Round(original.modification * 1.4f, 2), "rare scales and rounds");
        }
        var anvil = new ItemAnvil(player.inventory.itemInventory) { amount = 5 };
        player.inventory.itemInventory.items.Add(EItem.Anvil, anvil);
        Require(player.inventory.itemInventory.GetAmount(EItem.Anvil) == 5, "anvil amount");
        MyRandom.random = new System.Random(17);
        var expanded = data.GetUpgradeOffer(ERarity.Legendary, EWeapon.FireStaff);
        Require(expanded.Count == 4, "anvil additions capped by available modifiers");
        var selectedStats = new HashSet<EStat>();
        foreach (var modifier in expanded) Require(selectedStats.Add(modifier.stat), "no duplicate stats");
        var weaponData = ScriptableObject.CreateInstance<WeaponData>();
        weaponData.eWeapon = EWeapon.FireStaff;
        weaponData.damage = 10f;
        weaponData.knockback = 1f;
        weaponData.upgradeData = data;
        weaponData.Init();
        var weapon = new WeaponBase(weaponData);
        float before = weapon.GetValue(EStat.DamageMultiplier);
        MyRandom.random = new System.Random(17);
        var offered = weaponData.GetUpgradeOffer(ERarity.Legendary);
        weapon.Upgrade(offered);
        var damage = offered.Find(m => m.stat == EStat.DamageMultiplier);
        Require(weapon.level == 2 && damage != null, "weapon offer applied");
        Close(weapon.GetValue(EStat.DamageMultiplier), before + damage.modification, "weapon damage stat after offer");
        player.inventory.itemInventory.Cleanup();
        Debug.Log("WeaponOfferPortCheck passed: rarity weights and rolls, distinct modifiers, Anvil and applied weapon stats");
    }
}
