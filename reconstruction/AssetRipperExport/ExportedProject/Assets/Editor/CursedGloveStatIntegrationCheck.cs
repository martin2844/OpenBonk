using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEditor;
using UnityEngine;

public static class CursedGloveStatIntegrationCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("CursedGloveStatIntegrationCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("stat integration"); root.SetActive(false); GameManager.Instance = root.AddComponent<GameManager>(); MyPlayer.Instance = root.AddComponent<MyPlayer>();
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); MyPlayer.Instance.inventory = inv;
        inv.characterData = ScriptableObject.CreateInstance<CharacterData>(); inv.characterData.statModifiers = new List<StatModifier>();
        inv.tomeInventory = new TomeInventory(); inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects(); inv.statInventory = new StatInventory();
        inv.playerStats = new PlayerStatsNew(inv, true); inv.playerHealth = new PlayerHealth(inv.playerStats);
        Require(inv.playerStats.GetStat(EStat.MaxHealth) == 100f && inv.playerHealth.hp == 100 && inv.playerHealth.maxHp == 100, "initial player health");
        inv.itemInventory.AddItem(EItem.GloveCurse, 2);
        Require(inv.playerHealth.maxHp == 100, "stat updates are queued until player stats tick");
        inv.playerStats.Tick();
        Require(Mathf.Abs(inv.playerStats.GetStat(EStat.MaxHealth) - 64f) < .001f && Mathf.Abs(inv.playerStats.GetStat(EStat.Difficulty) - .2f) < .001f, "two stacks update computed health and difficulty");
        Require(inv.playerHealth.maxHp == 64 && inv.playerHealth.hp == 64, "health tracks reduced maximum");
        inv.itemInventory.RemoveItem(EItem.GloveCurse); inv.playerStats.Tick();
        Require(Mathf.Abs(inv.playerStats.GetStat(EStat.MaxHealth) - 80f) < .001f && Mathf.Abs(inv.playerStats.GetStat(EStat.Difficulty) - .1f) < .001f && inv.playerHealth.maxHp == 80 && inv.playerHealth.hp == 80, "one remaining stack updates stats and health");
        inv.itemInventory.RemoveItem(EItem.GloveCurse); inv.playerStats.Tick();
        Require(inv.playerStats.GetStat(EStat.MaxHealth) == 100f && inv.playerStats.GetStat(EStat.Difficulty) == 0f && inv.playerHealth.maxHp == 100 && inv.playerHealth.hp == 100, "final removal restores base stats and maximum health");
        inv.playerHealth.OnDestroy(); inv.playerStats.OnDestroy(); inv.statusEffects.OnDestroy(); inv.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("CursedGloveStatIntegrationCheck passed: actual queued stat updates and player health across acquisition and removal");
    }
}
