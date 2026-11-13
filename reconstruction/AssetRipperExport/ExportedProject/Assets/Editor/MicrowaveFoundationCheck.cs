using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MicrowaveFoundationCheck {
    static void Check(bool ok,string message) { if(!ok)throw new Exception("MicrowaveFoundationCheck: "+message); }
    public static void Run() {
        var root=new GameObject("microwave foundation fixture");root.SetActive(false);
        var game=root.AddComponent<GameManager>();GameManager.Instance=game;
        var player=root.AddComponent<MyPlayer>();MyPlayer.Instance=player;player.inventory=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var stats=(PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));stats.stats=new Dictionary<EStat,float>();stats.stats[EStat.ChestPriceMultiplier]=1f;player.inventory.playerStats=stats;
        var microwave=root.AddComponent<InteractableMicrowave>();var rarity=typeof(InteractableMicrowave).GetField("<rarity>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
        var uses=typeof(InteractableMicrowave).GetMethod("GetUses",BindingFlags.Instance|BindingFlags.NonPublic);var time=typeof(InteractableMicrowave).GetMethod("GetCookTime",BindingFlags.Instance|BindingFlags.NonPublic);
        int[] expected={3,2,2,1,3,3};for(int i=0;i<expected.Length;i++){rarity.SetValue(microwave,(EItemRarity)i);Check((int)uses.Invoke(microwave,new object[]{(EItemRarity)i})==expected[i],"rarity use count "+i);Check(Mathf.Approximately((float)time.Invoke(microwave,null),4+2*i),"rarity cook time "+i);}
        var purchased=typeof(MoneyUtility).GetField("chestsPurchased",BindingFlags.Static|BindingFlags.NonPublic);int old=(int)purchased.GetValue(null);
        foreach(int n in new[]{0,1,12}) { purchased.SetValue(null,n);int chest=MoneyUtility.GetChestPrice();Check(microwave.GetPrice()==Mathf.RoundToInt(chest*.34f),"scaled price at chest count "+n); }purchased.SetValue(null,old);
        var data=root.AddComponent<DataManager>();DataManager.Instance=data;var clover=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Clover.asset");Check(clover!=null,"Clover asset");typeof(DataManager).GetField("itemData",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(data,new Dictionary<EItem,ItemData>{{clover.eItem,clover}});
        var go=new GameObject("button",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));go.transform.SetParent(root.transform);var button=go.AddComponent<MicrowaveItemButton>();button.icon=go.GetComponent<RawImage>();var ui=root.AddComponent<MicrowaveUi>();button.Set(ui,clover.eItem);
        Check(button.icon.texture==clover.icon,"original item icon");var hover=typeof(MyButton).GetField("isHovering",BindingFlags.Instance|BindingFlags.NonPublic);button.StartHover();Check((bool)hover.GetValue(button),"hover begins");button.StopHover();Check(!(bool)hover.GetValue(button),"hover ends");
        UnityEngine.Object.DestroyImmediate(root);Debug.Log("MicrowaveFoundationCheck PASSED: price, use counts, cooking times, item icon and hover");
    }
}
