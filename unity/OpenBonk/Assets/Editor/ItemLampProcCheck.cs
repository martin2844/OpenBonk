using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;

public static class ItemLampProcCheck
{
    sealed class CounterItem : ItemBase
    {
        public int calls;
        public CounterItem(ItemInventory inventory) : base(inventory) { }
        public override void Init() { }
        public override void Cleanup() { }
        protected override void OnInitOrAmountChanged() { }
        public override void Tick() { }
        public override void PreAttack(DamageContainer dc, StatComponents stats) { }
        public override void ProcOnHitEffects(DamageContainer dc) { calls++; }
        public override bool HasOnHitEffectProc() => true;
        public override bool HasPreAttackProc() => false;
    }

    public static void Run()
    {
        var root = new GameObject("lamp proc check");
        root.SetActive(false);
        var inventory = new ItemInventory();
        try
        {
            var counter = new CounterItem(inventory);
            var lamp = new ItemOverpoweredLamp(inventory);
            inventory.items.Add(EItem.MoldyCheese, counter);
            inventory.items.Add(EItem.OverpoweredLamp, lamp);
            var sorted = (List<EItem>)typeof(ItemInventory).GetField("itemsWithOnHitSorted", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inventory);
            sorted.Add(EItem.MoldyCheese);
            var hit = new DamageContainer(1f, "test") { enemy = root.AddComponent<Enemy>() };
            for (int count = 0; count <= 2; count++)
            {
                lamp.amount = count;
                counter.calls = 0;
                inventory.PostDamage(hit);
                if (counter.calls != count + 1) throw new Exception("Unexpected on-hit repetitions with " + count + " lamps");
            }
            hit.procCoefficient = 0f;
            counter.calls = 0;
            inventory.PostDamage(hit);
            if (counter.calls != 0) throw new Exception("Zero proc coefficient triggered an effect");
            hit.procCoefficient = 1f;
            hit.enemy = null;
            inventory.PostDamage(hit);
            if (counter.calls != 0) throw new Exception("Missing enemy triggered an effect");
            Debug.Log("ItemLampProcCheck passed: stacking, zero coefficient and missing target");
        }
        finally
        {
            inventory.Cleanup();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
