using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class CreditCardsChestPortCheck
{
    const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool condition,string message) { if(!condition) throw new Exception("CreditCardsChestPortCheck: "+message); }
    static void Near(float x,float y,string message) { Require(Mathf.Abs(x-y)<.0001f,message+": "+x+" expected "+y); }
    public static void Run()
    {
        var root=new GameObject("credit cards test"); root.SetActive(false); var player=root.AddComponent<MyPlayer>(); MyPlayer.Instance=player; GameManager.Instance=root.AddComponent<GameManager>();
        var data=root.AddComponent<DataManager>(); DataManager.Instance=data; var ui=root.AddComponent<UiManager>(); UiManager.Instance=ui; ui.encounterWindows=root.AddComponent<EncounterWindows>();
        typeof(EncounterWindows).GetProperty("encounterInProgress").GetSetMethod(true).Invoke(ui.encounterWindows,new object[]{true});
        var save=root.AddComponent<SaveManager>(); save.Init(); save.config=new ConfigSaveFile(); save.config.cfGameSettings.skip_chest_animation=0;
        var inv=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory=inv;
        inv.characterData=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); inv.tomeInventory=new TomeInventory(); inv.itemInventory=new ItemInventory(); inv.statusEffects=new PlayerStatusEffects(); inv.statInventory=new StatInventory(); inv.playerXp=new PlayerXp { level=1 }; inv.playerStats=new PlayerStatsNew(inv,true);
        var items=(Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData",P).GetValue(data);
        foreach(var n in new[]{"CreditCardGreen","CreditCardRed"}) { var a=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/"+n+".asset"); Require(a!=null,"original "+n+" asset"); items.Add(a.eItem,a); }
        var pickupObj=new GameObject("dropped chest"); pickupObj.transform.SetParent(root.transform,false); var pickup=pickupObj.AddComponent<OpenChest>(); pickup.chestType=EChest.Ghost;
        var colliderObj=new GameObject("player collider"); colliderObj.transform.SetParent(root.transform,false); var collider=colliderObj.AddComponent<SphereCollider>(); colliderObj.layer=LayerMask.NameToLayer("Player");
        var chestObj=new GameObject("normal chest"); chestObj.transform.SetParent(root.transform,false); var chest=chestObj.AddComponent<InteractableChest>(); chest.chestType=EChest.Free;
        int opened=0; Action count=()=>opened++; OpenChest.A_Open+=count;
        try
        {
            LocalizationSettings.SelectedLocale=AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string gd=items[EItem.CreditCardGreen].GetDummyItem().GetDescription(items[EItem.CreditCardGreen].localizedDescription); string rd=items[EItem.CreditCardRed].GetDummyItem().GetDescription(items[EItem.CreditCardRed].localizedDescription);
            Require(gd.Contains("+2%") && gd.Contains("+10%") && !gd.Contains("{"),"green description: "+gd); Require(rd.Contains("+2.5%") && !rd.Contains("{"),"red description: "+rd);
            float luck=inv.playerStats.GetStat(EStat.Luck), damage=inv.playerStats.GetStat(EStat.DamageMultiplier), price=inv.playerStats.GetStat(EStat.ChestPriceMultiplier);
            inv.itemInventory.AddItem(EItem.CreditCardGreen,2); inv.itemInventory.AddItem(EItem.CreditCardRed); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.ChestPriceMultiplier),price+.2f,"green price penalty");
            Require(chest.Interact(),"free chest interaction"); inv.statInventory.Tick(); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.Luck),luck+.04f,"normal chest luck"); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),damage*1.025f,"normal chest damage");
            MyTime.time=10f; typeof(OpenChest).GetMethod("Awake",P).Invoke(pickup,null); var trigger=typeof(OpenChest).GetMethod("OnTriggerStay",P);
            MyTime.time=10.6f; trigger.Invoke(pickup,new object[]{collider}); Require(opened==0 && pickupObj.activeSelf,"pickup waits through boundary");
            MyTime.time=10.61f; colliderObj.layer=0; trigger.Invoke(pickup,new object[]{collider}); Require(opened==0 && pickupObj.activeSelf,"non-player ignored");
            colliderObj.layer=LayerMask.NameToLayer("Player"); trigger.Invoke(pickup,new object[]{collider}); Require(opened==1 && !pickupObj.activeSelf,"player collects dropped chest once");
            inv.statInventory.Tick(); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.Luck),luck+.08f,"dropped chest luck"); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),damage*1.05f,"dropped chest damage");
            inv.itemInventory.RemoveItem(EItem.CreditCardGreen); inv.itemInventory.RemoveItem(EItem.CreditCardGreen); inv.itemInventory.RemoveItem(EItem.CreditCardRed); inv.playerStats.Tick();
            InteractableChest.A_ChestOpened?.Invoke(); OpenChest.A_Open?.Invoke(); inv.statInventory.Tick(); inv.playerStats.Tick(); Near(inv.playerStats.GetStat(EStat.Luck),luck+.08f,"accumulated luck persists and listener removed"); Near(inv.playerStats.GetStat(EStat.DamageMultiplier),damage*1.05f,"accumulated damage persists and listener removed"); Near(inv.playerStats.GetStat(EStat.ChestPriceMultiplier),price,"price penalty removed");
            Debug.Log("CreditCardsChestPortCheck passed: original cards, real chest interaction, dropped chest trigger, event accumulation and removal");
        }
        finally { OpenChest.A_Open-=count; inv.playerStats.OnDestroy(); inv.statusEffects.OnDestroy(); inv.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance=null; GameManager.Instance=null; DataManager.Instance=null; UiManager.Instance=null; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{null}); }
    }
}
