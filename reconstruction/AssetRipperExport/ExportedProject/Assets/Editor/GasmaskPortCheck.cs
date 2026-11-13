using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

public static class GasmaskPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool condition, string text) { if (!condition) throw new Exception("GasmaskPortCheck: " + text); }
    static float Value(ItemGasmask item, EStat stat) => item.statModifiers[stat].GetModifiers().Single().modification;
    static void Poison(Enemy enemy) {
        enemy.AddDebuff(EDebuff.Poison, null, 5, 1);
        typeof(Enemy).GetMethod("AddDebuffImplementation", P).Invoke(enemy, new object[] { enemy.debuffsToAdd[EDebuff.Poison] });
        enemy.debuffsToAdd.Clear();
    }
    public static void Run() {
        var inventory = new ItemInventory(); var root = new GameObject("gas mask fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = inventory;
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.DurationMultiplier] = 1f }; player.inventory.playerStats = stats;
        var first = root.AddComponent<Enemy>(); var second = root.AddComponent<Enemy>(); var previous = DebuffPoison.numPoisonedEnemies;
        try {
            DebuffPoison.numPoisonedEnemies = 0; MyTime.time = 10; inventory.AddItem(EItem.Gasmask); var mask = (ItemGasmask)inventory.GetItem(EItem.Gasmask);
            Poison(first); Poison(second); Require(DebuffPoison.numPoisonedEnemies == 2, "real poison registration");
            MyTime.time = 10.99f; inventory.Tick(); Require(!mask.statModifiers.ContainsKey(EStat.Armor), "no update before interval");
            MyTime.time = 11; inventory.Tick(); Require(Mathf.Abs(Value(mask, EStat.Armor) - .01f) < .00001f && Mathf.Abs(Value(mask, EStat.Overheal) - .01f) < .00001f, "two poisoned enemies grant stats");
            first.RemoveDebuff(EDebuff.Poison, false); Require(DebuffPoison.numPoisonedEnemies == 1, "real poison removal");
            MyTime.time = 12; inventory.Tick(); Require(Mathf.Abs(Value(mask, EStat.Armor) - .005f) < .00001f, "poison removal updates bonuses");
            second.RemoveDebuff(EDebuff.Poison, false); DebuffPoison.numPoisonedEnemies = 100;
            MyTime.time = 13; inventory.Tick(); Require(Mathf.Abs(Value(mask, EStat.Armor) - .4f) < .00001f && Mathf.Abs(Value(mask, EStat.Overheal) - .25f) < .00001f, "first-stack caps");
            inventory.AddItem(EItem.Gasmask); MyTime.time = 14; inventory.Tick();
            Require(Mathf.Abs(Value(mask, EStat.Armor) - .5f) < .00001f && Mathf.Abs(Value(mask, EStat.Overheal) - .5f) < .00001f, "increased caps update even when poisoned count is unchanged");
            GameManager.A_StageStarted?.Invoke(); Require(DebuffPoison.numPoisonedEnemies == 0, "stage resets counter"); MyTime.time = 15; inventory.Tick(); Require(Value(mask, EStat.Armor) == 0 && Value(mask, EStat.Overheal) == 0, "bonuses clear on next tick");
            DebuffPoison.numPoisonedEnemies = 2; MyTime.time = 16; inventory.Tick();
            inventory.RemoveItem(EItem.Gasmask); inventory.RemoveItem(EItem.Gasmask);
            Require(Value(mask, EStat.Armor) == 0 && Value(mask, EStat.Overheal) == 0, "last removal clears modifiers");
            DebuffPoison.numPoisonedEnemies = 7; GameManager.A_StageStarted?.Invoke(); Require(DebuffPoison.numPoisonedEnemies == 7, "removal unsubscribes stage event");
            var keys = (Dictionary<string,object>)typeof(ItemGasmask).GetMethod("GetLocalizationKeys", P).Invoke(mask, null); Require(keys.ContainsKey("stat1") && keys.ContainsKey("stat2"), "description keys");
            Debug.Log("GasmaskPortCheck passed: poison lifecycle, update interval, caps, stacking and stage reset");
        } finally { inventory.Cleanup(); DebuffFactory.Reset(); DebuffPoison.numPoisonedEnemies = previous; MyPlayer.Instance = null; GameManager.Instance = null; UnityEngine.Object.DestroyImmediate(root); }
    }
}
