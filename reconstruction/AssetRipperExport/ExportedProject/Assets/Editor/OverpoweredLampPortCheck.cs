using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEditor;
using UnityEngine;

public static class OverpoweredLampPortCheck
{
    static void Require(bool ok,string message) { if (!ok) throw new Exception(message); }
    public static void Run()
    {
        var root=new GameObject("lamp test data"); root.SetActive(false);
        var data=root.AddComponent<DataManager>(); DataManager.Instance=data;
        var items=(Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(data);
        var cheese=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Moldy Cheese.asset");
        var lamp=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Overpowered Lamp.asset");
        Require(cheese!=null && cheese.eItem==EItem.MoldyCheese && lamp!=null && lamp.eItem==EItem.OverpoweredLamp,"original item assets");
        items.Add(cheese.eItem,cheese); items.Add(lamp.eItem,lamp);
        var target=new GameObject("lamp poison target"); var enemy=target.AddComponent<Enemy>();
        var enemyData=ScriptableObject.CreateInstance<EnemyData>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy,new object[]{enemyData});
        var inventory=new ItemInventory();
        try
        {
            inventory.AddItem(EItem.MoldyCheese,5);
            var hit=new DamageContainer(1f,"test") { enemy=enemy,procCoefficient=1f };
            inventory.PostDamage(hit); Require(enemy.debuffsToAdd[EDebuff.Poison].stacks==2,"baseline poison stacks"); enemy.debuffsToAdd.Clear();
            inventory.AddItem(EItem.OverpoweredLamp); inventory.PostDamage(hit); Require(enemy.debuffsToAdd[EDebuff.Poison].stacks==4,"one lamp repeats hit effect"); enemy.debuffsToAdd.Clear();
            inventory.AddItem(EItem.OverpoweredLamp); inventory.PostDamage(hit); Require(enemy.debuffsToAdd[EDebuff.Poison].stacks==6,"two lamps repeat twice"); enemy.debuffsToAdd.Clear();
            hit.procCoefficient=0f; inventory.PostDamage(hit); Require(!enemy.debuffsToAdd.ContainsKey(EDebuff.Poison),"zero coefficient blocks repeated effects");
            hit.procCoefficient=1f; inventory.RemoveItem(EItem.OverpoweredLamp); inventory.PostDamage(hit); Require(enemy.debuffsToAdd[EDebuff.Poison].stacks==4,"removing one lamp restores single repeat"); enemy.debuffsToAdd.Clear();
            inventory.RemoveItem(EItem.OverpoweredLamp); inventory.PostDamage(hit); Require(enemy.debuffsToAdd[EDebuff.Poison].stacks==2,"removing lamps restores baseline");
            Debug.Log("OverpoweredLampPortCheck passed: original item assets, on-hit repetition, stacking, zero coefficient and removal");
        }
        finally { inventory.Cleanup(); UnityEngine.Object.DestroyImmediate(enemyData); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(root); DataManager.Instance=null; }
    }
}
