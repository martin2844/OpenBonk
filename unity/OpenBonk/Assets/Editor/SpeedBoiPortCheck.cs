using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;

public static class SpeedBoiPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Near(float value, float expected, string message) { Require(Mathf.Abs(value-expected)<.0001f, message+": "+value+" expected "+expected); }
    public static void Run()
    {
        var root = new GameObject("Speed Boi test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
        inv.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inv.tomeInventory = new TomeInventory(); inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects(); inv.statInventory = new StatInventory(); inv.playerXp = new PlayerXp { level = 1 };
        inv.playerStats = new PlayerStatsNew(inv, true); inv.playerHealth = new PlayerHealth(inv.playerStats);
        var items = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
        var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Speed Boi.asset");
        Require(asset != null && asset.eItem == EItem.SpeedBoi, "original Speed Boi asset"); items.Add(EItem.SpeedBoi, asset);
        int procs=0; Action onProc=()=>procs++; ItemSpeedBoi.A_Slowdown += onProc;
        var remaining=typeof(MyTime).GetField("timescaleTimeRemaining",BindingFlags.Static|BindingFlags.NonPublic);
        try
        {
            MyTime.SetTimeScale(1f,0f); MyTime.time=100f;
            inv.itemInventory.AddItem(EItem.SpeedBoi,1); inv.playerStats.Tick();
            float initialDamage=inv.playerStats.GetStat(EStat.DamageMultiplier);
            inv.playerHealth.maxHp=100; inv.playerHealth.hp=50; inv.playerHealth.overheal=0;
            PlayerHealth.A_TakeDamage?.Invoke(inv.playerHealth,null,false);
            Require(procs==0,"exactly half health must not trigger"); Near(MyTime.timeScale,1f,"half-health timescale");
            inv.playerHealth.hp=49; PlayerHealth.A_TakeDamage?.Invoke(inv.playerHealth,null,false); inv.playerStats.Tick();
            Require(procs==1,"below half health triggers"); Near(MyTime.timeScale,.3f,"slowdown scale"); Near((float)remaining.GetValue(null),10f,"one stack duration");
            Near(inv.playerStats.GetStat(EStat.DamageMultiplier),initialDamage*2f,"damage during slowdown");
            MyTime.SetTimeScale(1f,0f); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),initialDamage,"damage resets on normal time");
            MyTime.time=119.99f; PlayerHealth.A_TakeDamage?.Invoke(inv.playerHealth,null,false); Require(procs==1,"cooldown before deadline");
            inv.itemInventory.AddItem(EItem.SpeedBoi,1); MyTime.time=120f; PlayerHealth.A_TakeDamage?.Invoke(inv.playerHealth,null,false);
            Require(procs==2,"cooldown deadline"); Near((float)remaining.GetValue(null),12f,"two stack duration");
            MyTime.SetTimeScale(1f,0f); inv.itemInventory.RemoveItem(EItem.SpeedBoi); inv.itemInventory.RemoveItem(EItem.SpeedBoi);
            MyTime.time=200f; PlayerHealth.A_TakeDamage?.Invoke(inv.playerHealth,null,false); Require(procs==2,"removed item unsubscribes");
            Debug.Log("SpeedBoiPortCheck passed: health threshold, cooldown, stacked duration, damage, reset and removal");
        }
        finally { ItemSpeedBoi.A_Slowdown -= onProc; MyTime.SetTimeScale(1f,0f); inv.playerHealth.OnDestroy(); inv.playerStats.OnDestroy(); inv.statusEffects.OnDestroy(); inv.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance=null; GameManager.Instance=null; DataManager.Instance=null; }
    }
}
