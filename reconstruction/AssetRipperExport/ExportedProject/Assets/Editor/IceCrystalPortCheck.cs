using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class IceCrystalPortCheck
{
    sealed class FixedRandom : System.Random { readonly double value; public FixedRandom(double v) { value=v; } public override double NextDouble() => value; }
    const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message) { if(!ok) throw new Exception("IceCrystalPortCheck: "+message); }
    static void Near(float a,float b,string message) { Check(Mathf.Abs(a-b)<.0001f,message+": "+a+" expected "+b); }
    public static void Run()
    {
        var oldRandom=global::Utility.MyRandom.random; var oldData=DataManager.Instance; var root=new GameObject("ice crystal fixture"); root.SetActive(false); var manager=root.AddComponent<DataManager>(); DataManager.Instance=manager; var enemy=root.AddComponent<Enemy>(); var inventory=new ItemInventory();
        try
        {
            var asset=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/IceCrystal.asset"); Check(asset!=null && asset.eItem==EItem.IceCrystal,"original item"); ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData",P).GetValue(manager)).Add(asset.eItem,asset);
            LocalizationSettings.SelectedLocale=AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset"); var desc=asset.GetDummyItem().GetDescription(asset.localizedDescription); Check(desc.Contains("7.5%") && !desc.Contains("{"),"description: "+desc);
            inventory.AddItem(EItem.IceCrystal); var item=(ItemIceCrystal)inventory.GetItem(EItem.IceCrystal); Check(item.HasOnHitEffectProc(),"registered for on-hit effects");
            var chanceField=typeof(ItemIceCrystal).GetField("procChance",P); var durationField=typeof(ItemIceCrystal).GetField("freezeTime",P); float chance=.075f/(.75f+.075f); Near((float)chanceField.GetValue(item),chance,"one-item chance"); Near((float)durationField.GetValue(item),2.6f,"one-item duration");
            var dc=new DamageContainer(1f,"test") { enemy=enemy }; global::Utility.MyRandom.random=new FixedRandom(chance+.001); inventory.PostDamage(dc); Check(!enemy.debuffsToAdd.ContainsKey(EDebuff.Freeze),"roll above chance misses");
            global::Utility.MyRandom.random=new FixedRandom(chance-.001); inventory.PostDamage(dc); Check(enemy.debuffsToAdd.TryGetValue(EDebuff.Freeze,out var pending),"roll below chance queues freeze"); Near(pending.duration,2.6f,"queued duration"); Check(pending.stacks==1 && pending.dc.enemy==enemy && pending.dc.damageSource=="test","freeze carries original damage context");
            enemy.debuffsToAdd.Clear(); dc.procCoefficient=0f; global::Utility.MyRandom.random=new FixedRandom(0); inventory.PostDamage(dc); Check(!enemy.debuffsToAdd.ContainsKey(EDebuff.Freeze),"zero proc coefficient cannot freeze");
            inventory.AddItem(EItem.IceCrystal); Near((float)chanceField.GetValue(item),.15f/.9f,"two-item chance"); Near((float)durationField.GetValue(item),2.7f,"two-item duration");
            inventory.AddItem(EItem.IceCrystal,40); Near((float)durationField.GetValue(item),6f,"duration cap"); Check((float)chanceField.GetValue(item)<1f,"chance approaches but does not reach one");
            for(int i=0;i<42;i++) inventory.RemoveItem(EItem.IceCrystal); Check(inventory.GetItem(EItem.IceCrystal)==null,"removal");
            Debug.Log("IceCrystalPortCheck passed: item localization, on-hit chance, proc coefficient, freeze duration, stacking and cap");
        }
        finally { inventory.Cleanup(); global::Utility.MyRandom.random=oldRandom; DataManager.Instance=oldData; UnityEngine.Object.DestroyImmediate(root); }
    }
}
