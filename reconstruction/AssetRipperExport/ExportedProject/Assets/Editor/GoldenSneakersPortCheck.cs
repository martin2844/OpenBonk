using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class GoldenSneakersPortCheck
{
    const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string m) { if(!ok) throw new Exception("GoldenSneakersPortCheck: "+m); }
    static void Near(float a,float b,string m) { Check(Mathf.Abs(a-b)<.0001f,m+": "+a+" expected "+b); }
    public static void Run()
    {
        var oldPlayer=MyPlayer.Instance; var oldGame=GameManager.Instance; var oldPool=PoolManager.Instance; var oldData=DataManager.Instance; float oldTime=MyTime.time; int oldFrameGold=PlayerInventory.goldThisFrame;
        var root=new GameObject("sneakers test"); root.SetActive(false); var drops=new List<GameObject>(); ItemInventory items=null;
        try
        {
            GameManager.Instance=root.AddComponent<GameManager>(); var player=root.AddComponent<MyPlayer>(); MyPlayer.Instance=player; var data=root.AddComponent<DataManager>(); DataManager.Instance=data;
            var asset=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Golden Sneakers.asset"); Check(asset!=null && asset.eItem==EItem.GoldenSneakers,"original item asset"); ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData",P).GetValue(data)).Add(asset.eItem,asset);
            var inv=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory=inv; items=inv.itemInventory=new ItemInventory(); inv.playerStats=(PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); inv.playerStats.stats=new Dictionary<EStat,float> {{EStat.GoldIncreaseMultiplier,1f}}; PlayerInventory.goldThisFrame=0;
            var pool=root.AddComponent<PoolManager>(); PoolManager.Instance=pool; var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/MoneyFlying.prefab"); Check(prefab!=null,"original money prefab"); pool.goldPool=new ObjectPool<GameObject>(()=>{var o=UnityEngine.Object.Instantiate(prefab); drops.Add(o); return o;},o=>o.SetActive(true),o=>o.SetActive(false),null,true,10,30);
            typeof(MoneyUtility).GetMethod("OnNewRun",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            int Value() => drops.Where(o=>o.activeSelf).Sum(o=>o.GetComponent<MoneyFlying>().value);
            MyTime.time=10f; player.transform.position=Vector3.zero; items.AddItem(EItem.GoldenSneakers); var item=(ItemGoldenSneakers)items.GetItem(EItem.GoldenSneakers); var type=item.GetType(); Near((float)type.GetField("goldPerMeter",P).GetValue(item),.1f,"one-stack rate");
            player.transform.position=new Vector3(3,0,0); items.Tick(); Check(Value()==0,"fractional distance does not drop coins"); Near((float)type.GetField("accumulatedGold",P).GetValue(item),.3f,"fraction preserved");
            player.transform.position=new Vector3(11,0,0); MyTime.time=10.49f; items.Tick(); Check(Value()==0,"no early sample"); MyTime.time=10.5f; items.Tick(); Check(Value()==1,"boundary spawns one coin"); Near((float)type.GetField("accumulatedGold",P).GetValue(item),.1f,"remainder after payout");
            player.transform.position=new Vector3(20,0,0); items.AddItem(EItem.GoldenSneakers); Near((float)type.GetField("goldPerMeter",P).GetValue(item),.15f,"second-stack rate"); player.transform.position=new Vector3(27,0,0); MyTime.time=11f; items.Tick(); Check(Value()==2,"stack change resets origin and retains fraction"); Near((float)type.GetField("accumulatedGold",P).GetValue(item),.15f,"stacked remainder");
            foreach(var o in drops.Where(o=>o.activeSelf).ToArray()) { player.transform.position=o.transform.position; typeof(MoneyFlying).GetMethod("FixedUpdate",P).Invoke(o.GetComponent<MoneyFlying>(),null); } Check(inv.goldInt==2,"spawned coins collect into player inventory");
            items.RemoveItem(EItem.GoldenSneakers); items.RemoveItem(EItem.GoldenSneakers); player.transform.position+=Vector3.right*100; MyTime.time=12f; items.Tick(); Check(inv.goldInt==2 && Value()==0,"removal stops drops");
            Debug.Log("GoldenSneakersPortCheck passed: movement sampling, fractional gold, stacking, coin spawning and collection");
        }
        finally { items?.Cleanup(); typeof(MoneyUtility).GetMethod("OnNewRun",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null); foreach(var o in drops) UnityEngine.Object.DestroyImmediate(o); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance=oldPlayer; GameManager.Instance=oldGame; PoolManager.Instance=oldPool; DataManager.Instance=oldData; MyTime.time=oldTime; PlayerInventory.goldThisFrame=oldFrameGold; }
    }
}
