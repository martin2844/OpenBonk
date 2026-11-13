using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class CursedDollPortCheck
{
    const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool value,string message) { if(!value) throw new Exception("CursedDollPortCheck: "+message); }
    static void Near(float a,float b,string message) { Require(Mathf.Abs(a-b)<.0001f,message+": "+a+" expected "+b); }
    public static void Run()
    {
        var root=new GameObject("cursed doll test"); root.SetActive(false); var player=root.AddComponent<MyPlayer>(); MyPlayer.Instance=player; GameManager.Instance=root.AddComponent<GameManager>(); var manager=root.AddComponent<EnemyManager>(); EnemyManager.Instance=manager; var data=root.AddComponent<DataManager>(); DataManager.Instance=data;
        var pool=root.AddComponent<PoolManager>(); PoolManager.Instance=pool; pool.cursedHit=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/CurseHitFx.prefab"); Require(pool.cursedHit!=null,"original cursed hit effect"); typeof(PoolManager).GetMethod("Start",P).Invoke(pool,null);
        var inv=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory=inv; inv.itemInventory=new ItemInventory();
        var asset=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Cursed Doll.asset"); Require(asset!=null && asset.eItem==EItem.CursedDoll,"original item"); ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData",P).GetValue(data)).Add(asset.eItem,asset);
        var targets=new List<Enemy>();
        for(int i=0;i<7;i++) { var o=new GameObject("curse target "+i); o.SetActive(false); var e=o.AddComponent<Enemy>(); e.hp=e.maxHp=1000; typeof(Enemy).GetField("controlHp",P).SetValue(e,1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(e,new object[]{ScriptableObject.CreateInstance<EnemyData>()}); manager.enemies.Add((uint)i,e); targets.Add(e); }
        typeof(Enemy).GetField("enemyFlag",P).SetValue(targets[0],EEnemyFlag.Boss);
        var hits=new Dictionary<Enemy,int>(); Action<Enemy,DamageContainer> observer=(e,dc)=> { if(dc.damageSource!="CursedDoll") return; Require(dc.damageEffect==EDamageEffect.Cursed && dc.procCoefficient==1f,"damage metadata"); hits[e]=hits.TryGetValue(e,out int n)?n+1:1; };
        try
        {
            LocalizationSettings.SelectedLocale=AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset"); string desc=asset.GetDummyItem().GetDescription(asset.localizedDescription); Require(desc.Contains("30%") && !desc.Contains("{"),"description: "+desc);
            inv.itemInventory.AddItem(EItem.CursedDoll,3); Enemy.A_Damage+=observer; MyTime.time=10; inv.itemInventory.Tick();
            Require(hits.Count==5 && pool.cursedHitPool.CountActive==5,"at most five newly cursed and effects per second"); Near(targets[0].hp,1000f-player.GetBaseDamage()*.7f,"boss damage"); for(int i=1;i<5;i++) Near(targets[i].hp,700f,"normal damage"); Near(targets[5].hp,1000f,"sixth target waits");
            MyTime.time=10.99f; inv.itemInventory.Tick(); Require(hits.Count==5 && hits[targets[1]]==1,"cooldown holds");
            MyTime.time=11f; inv.itemInventory.Tick(); Require(hits.Count==6 && hits[targets[1]]==2,"six-target limit and second tick"); Near(targets[5].hp,700f,"sixth target added"); Near(targets[6].hp,1000f,"seventh excluded by cap");
            Enemy.A_EnemyReleasedFromPool?.Invoke(targets[1]); manager.enemies.Remove(1); MyTime.time=12f; inv.itemInventory.Tick(); Require(hits.ContainsKey(targets[6]),"released enemy replaced"); Require(hits[targets[1]]==2,"released enemy no longer damaged");
            inv.itemInventory.RemoveItem(EItem.CursedDoll); inv.itemInventory.RemoveItem(EItem.CursedDoll); inv.itemInventory.RemoveItem(EItem.CursedDoll); int before=hits[targets[6]]; MyTime.time=13f; inv.itemInventory.Tick(); Enemy.A_EnemyReleasedFromPool?.Invoke(targets[6]); Require(hits[targets[6]]==before,"removed item stops ticking");
            Debug.Log("CursedDollPortCheck passed: original item, target cap, per-check limit, boss damage, cooldown, release and cleanup");
        }
        finally { Enemy.A_Damage-=observer; inv.itemInventory.Cleanup(); foreach(var fx in UnityEngine.Object.FindObjectsOfType<ReturnToPool>()) if(fx.name.StartsWith("CurseHitFx")) UnityEngine.Object.DestroyImmediate(fx.gameObject); foreach(var e in targets) { UnityEngine.Object.DestroyImmediate(e.enemyData); UnityEngine.Object.DestroyImmediate(e.gameObject); } UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance=null; GameManager.Instance=null; EnemyManager.Instance=null; DataManager.Instance=null; PoolManager.Instance=null; }
    }
}
