using System;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEditor;
using UnityEngine;

public static class MoldyCheeseInventoryPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("MoldyCheeseInventoryPortCheck: " + message); }
    public static void Run()
    {
        var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Moldy Cheese.asset");
        Require(asset != null && asset.eItem == EItem.MoldyCheese && asset.isEnabled, "enabled original asset");
        var target = new GameObject("poison target");
        var enemy = target.AddComponent<Enemy>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        var inventory = new ItemInventory();
        var previousRandom = global::Utility.MyRandom.random;
        try
        {
            int samples = 0;
            foreach (int amount in new[] { 1, 3, 5, 7 })
            {
                inventory.AddItem(EItem.MoldyCheese, amount);
                foreach (float coefficient in new[] { .25f, .5f, 1f })
                {
                    int seed = 1200 + amount * 10 + (int)(coefficient * 4);
                    global::Utility.MyRandom.random = new System.Random(seed);
                    var oracle = new System.Random(seed);
                    float chance = .4f * amount;
                    int guaranteed = Mathf.FloorToInt(chance);
                    float fractional = chance - guaranteed;
                    var fractionalHit = new DamageContainer(coefficient, "fractional_test") { enemy = enemy, procCoefficient = coefficient };
                    for (int i = 0; i < 100; i++)
                    {
                        enemy.debuffsToAdd.Clear();
                        int expected = guaranteed + (fractional > 0f && oracle.NextDouble() < coefficient * fractional ? 1 : 0);
                        inventory.PostDamage(fractionalHit);
                        int actual = enemy.debuffsToAdd.TryGetValue(EDebuff.Poison, out var queued) ? queued.stacks : 0;
                        Require(actual == expected, $"amount={amount} coefficient={coefficient} roll={i}: {actual} expected {expected}");
                        samples++;
                    }
                }
                for (int i = 0; i < amount; i++) inventory.RemoveItem(EItem.MoldyCheese, false);
            }
            enemy.debuffsToAdd.Clear();
            inventory.AddItem(EItem.MoldyCheese, 5);
            var hit = new DamageContainer(1f, "test") { enemy = enemy, procCoefficient = 0f };
            inventory.PostDamage(hit);
            Require(!enemy.debuffsToAdd.ContainsKey(EDebuff.Poison), "zero coefficient blocks inventory procs");
            hit.procCoefficient = 1f;
            inventory.PostDamage(hit);
            var poison = enemy.debuffsToAdd[EDebuff.Poison];
            Require(poison.stacks == 2 && poison.duration == 5f, "five items queue two stacks for five seconds");
            enemy.debuffsToAdd.Clear();
            for (int i = 0; i < 5; i++) inventory.RemoveItem(EItem.MoldyCheese, false);
            Require(inventory.GetItem(EItem.MoldyCheese) == null, "last removal unregisters item");
            inventory.PostDamage(hit);
            Require(!enemy.debuffsToAdd.ContainsKey(EDebuff.Poison), "removed item cannot apply poison");
            Debug.Log($"MoldyCheeseInventoryPortCheck passed: {samples} seeded fractional/guaranteed rolls, original asset, zero coefficient, five-item poison and removal");
        }
        finally { inventory.Cleanup(); global::Utility.MyRandom.random = previousRandom; UnityEngine.Object.DestroyImmediate(target); }
    }
}
