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

public static class IdleJuicePortCheck
{
    const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool b,string s) { if(!b) throw new Exception("IdleJuicePortCheck: "+s); }
    static void Near(float a,float b,string s) { Check(Mathf.Abs(a-b)<.0001f,s+": "+a+" expected "+b); }
    public static void Run()
    {
        var root=new GameObject("idle juice test"); root.SetActive(false); var player=root.AddComponent<MyPlayer>(); MyPlayer.Instance=player; GameManager.Instance=root.AddComponent<GameManager>(); var data=root.AddComponent<DataManager>(); DataManager.Instance=data;
        var inv=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory=inv; inv.characterData=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); inv.tomeInventory=new TomeInventory(); inv.itemInventory=new ItemInventory(); inv.statusEffects=new PlayerStatusEffects(); inv.statInventory=new StatInventory(); inv.playerXp=new PlayerXp{level=1}; inv.playerStats=new PlayerStatsNew(inv,true);
        var asset=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/IdleJuice.asset"); if(asset==null) asset=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Idle Juice.asset"); Check(asset!=null && asset.eItem==EItem.IdleJuice,"original item asset"); ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData",P).GetValue(data)).Add(asset.eItem,asset);
        try
        {
            LocalizationSettings.SelectedLocale=AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset"); string desc=asset.GetDummyItem().GetDescription(asset.localizedDescription); Check(desc.Contains("+100%") && !desc.Contains("{"),"description: "+desc);
            float baseline=inv.playerStats.GetStat(EStat.DamageMultiplier); MyTime.time=10f; player.transform.position=new Vector3(10,0,0); inv.itemInventory.AddItem(EItem.IdleJuice); var item=inv.itemInventory.GetItem(EItem.IdleJuice); var type=item.GetType(); inv.itemInventory.Tick(); Check(!(bool)type.GetField("isCampActive",P).GetValue(item),"movement begins setup");
            MyTime.time=(float)type.GetField("startCampfireTime",P).GetValue(item); inv.itemInventory.Tick(); Check(!(bool)type.GetField("isCampActive",P).GetValue(item),"setup boundary is exclusive");
            MyTime.time+=.01f; inv.itemInventory.Tick(); inv.playerStats.Tick(); Check((bool)type.GetField("isCampActive",P).GetValue(item),"camp activates after setup"); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseline+.04f,"immediate first increment");
            MyTime.time=(float)type.GetField("nextUpdateDamageTime",P).GetValue(item); inv.itemInventory.Tick(); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseline+.04f,"increment boundary is exclusive");
            for(int i=0;i<35;i++) { MyTime.time+=1.01f; inv.itemInventory.Tick(); } inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseline+1f,"one-item damage cap");
            inv.itemInventory.AddItem(EItem.IdleJuice); for(int i=0;i<30;i++) { MyTime.time+=1.01f; inv.itemInventory.Tick(); } inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseline+2f,"two-item damage cap");
            inv.itemInventory.RemoveItem(EItem.IdleJuice); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseline+1f,"removing one item lowers active cap");
            inv.itemInventory.AddItem(EItem.IdleJuice); for(int i=0;i<30;i++) { MyTime.time+=1.01f; inv.itemInventory.Tick(); } inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseline+2f,"restacked damage cap");
            player.transform.position=new Vector3(11.75f,0,0); inv.itemInventory.Tick(); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseline+2f,"threshold distance remains active");
            player.transform.position=new Vector3(11.76f,0,0); inv.itemInventory.Tick(); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseline,"movement resets bonus"); Check(!(bool)type.GetField("isCampActive",P).GetValue(item),"movement stops camp");
            MyTime.time+=2f; inv.itemInventory.Tick(); inv.playerStats.Tick(); Check(inv.playerStats.GetStat(EStat.DamageMultiplier)>baseline,"bonus active before final removal");
            inv.itemInventory.RemoveItem(EItem.IdleJuice); inv.itemInventory.RemoveItem(EItem.IdleJuice); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),baseline,"active removal restores stats"); Debug.Log("IdleJuicePortCheck passed: original item, setup, strict timing, stacking cap, movement threshold and active cleanup");
        }
        finally { inv.playerStats.OnDestroy(); inv.statusEffects.OnDestroy(); inv.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance=null; GameManager.Instance=null; DataManager.Instance=null; }
    }
}
