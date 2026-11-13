using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class BeefyRingPortCheck
{
    const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool b,string s) { if(!b) throw new Exception("BeefyRingPortCheck: "+s); }
    static void Near(float a,float b,string s) { Check(Mathf.Abs(a-b)<.0001f,s+": "+a+" expected "+b); }
    public static void Run()
    {
        var root=new GameObject("beefy ring test"); root.SetActive(false); var player=root.AddComponent<MyPlayer>(); MyPlayer.Instance=player; GameManager.Instance=root.AddComponent<GameManager>(); var data=root.AddComponent<DataManager>(); DataManager.Instance=data;
        var inv=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory=inv; inv.characterData=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); inv.tomeInventory=new TomeInventory(); inv.itemInventory=new ItemInventory(); inv.statusEffects=new PlayerStatusEffects(); inv.statInventory=new StatInventory(); inv.playerXp=new PlayerXp{level=1}; inv.playerStats=new PlayerStatsNew(inv,true);
        inv.playerHealth=new PlayerHealth(inv.playerStats);
        var asset=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Beefy Ring.asset"); Check(asset!=null && asset.eItem==EItem.BeefyRing,"original item asset"); ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData",P).GetValue(data)).Add(asset.eItem,asset);
        try
        {
            LocalizationSettings.SelectedLocale=AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset"); string desc=asset.GetDummyItem().GetDescription(asset.localizedDescription); Check(desc.Contains("20%") && desc.Contains("100") && !desc.Contains("{"),"description: "+desc);
            float baseHp=inv.playerStats.GetStat(EStat.MaxHealth), baseDamage=inv.playerStats.GetStat(EStat.DamageMultiplier); MyTime.time=10f;
            inv.itemInventory.AddItem(EItem.BeefyRing); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.MaxHealth),baseHp+10,"one ring health");
            inv.itemInventory.Tick(); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseDamage,"no early damage update");
            MyTime.time=11f; inv.itemInventory.Tick(); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseDamage*(1f+inv.playerHealth.GetCombinedMaxHp()*.002f),"damage update at boundary");
            inv.itemInventory.AddItem(EItem.BeefyRing); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.MaxHealth),baseHp+20,"two rings health"); MyTime.time=12f; inv.itemInventory.Tick(); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseDamage*(1f+inv.playerHealth.GetCombinedMaxHp()*.004f),"stacked damage");
            inv.itemInventory.RemoveItem(EItem.BeefyRing); inv.playerStats.Tick(); MyTime.time=13f; inv.itemInventory.Tick(); inv.playerStats.Tick();
            Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseDamage*(1f+inv.playerHealth.GetCombinedMaxHp()*.002f),"one ring after partial removal");
            inv.itemInventory.RemoveItem(EItem.BeefyRing); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.MaxHealth),baseHp,"health restored"); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseDamage,"damage restored after final removal");
            inv.itemInventory.AddItem(EItem.BeefyRing); inv.playerStats.Tick(); MyTime.time=14f; inv.itemInventory.Tick(); inv.playerStats.Tick();
            Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseDamage*(1f+inv.playerHealth.GetCombinedMaxHp()*.002f),"damage after reacquisition");
            inv.itemInventory.RemoveItem(EItem.BeefyRing); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseDamage,"damage restored after reacquired ring removed");
            Debug.Log("BeefyRingPortCheck passed: original item, localization, timing, health, scaling, stacking, removal and reacquisition");
        }
        finally { inv.playerHealth.OnDestroy(); inv.playerStats.OnDestroy(); inv.statusEffects.OnDestroy(); inv.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance=null; GameManager.Instance=null; DataManager.Instance=null; }
    }
}
