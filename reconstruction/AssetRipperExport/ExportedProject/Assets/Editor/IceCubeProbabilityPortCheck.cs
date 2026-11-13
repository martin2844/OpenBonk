using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using UnityEditor;
using UnityEngine;

public static class IceCubeProbabilityPortCheck
{
    sealed class FixedRandom : System.Random
    {
        readonly double value;
        public FixedRandom(double value) { this.value = value; }
        public override double NextDouble() => value;
    }
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception("IceCubeProbabilityPortCheck: " + message); }
    static void Near(float value, float expected, string message) { Check(Mathf.Abs(value - expected) < .00001f, message + ": " + value + " != " + expected); }

    public static void Run()
    {
        var previousRandom = global::Utility.MyRandom.random;
        var previousData = DataManager.Instance;
        var root = new GameObject("Ice Cube probability fixture");
        root.SetActive(false);
        var data = root.AddComponent<DataManager>();
        DataManager.Instance = data;
        var enemy = root.AddComponent<Enemy>();
        var inventory = new ItemInventory();
        Action onFreeze = null;
        try
        {
            var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Ice Cube.asset");
            Check(asset != null && asset.eItem == EItem.IceCube, "original Ice Cube asset");
            ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", Private).GetValue(data)).Add(asset.eItem, asset);
            inventory.AddItem(EItem.IceCube);
            var item = (ItemIceCube)inventory.GetItem(EItem.IceCube);
            var proc = typeof(ItemIceCube).GetField("procChance", Private);
            var freeze = typeof(ItemIceCube).GetField("freezeChance", Private);
            Near((float)proc.GetValue(item), .2f / .7f, "one stack damage chance");
            Near((float)freeze.GetValue(item), .4f, "one stack freeze chance");
            Near(item.damageRatio, .4f, "one stack damage ratio");
            inventory.AddItem(EItem.IceCube);
            Near((float)proc.GetValue(item), .4f / .9f, "two stack damage chance");
            Near((float)freeze.GetValue(item), .8f / 1.4f, "two stack freeze chance");
            Near(item.damageRatio, .8f, "two stack damage ratio");
            var threshold = (float)freeze.GetValue(item) * .5f;
            proc.SetValue(item, 0f); // Isolate the original ice-hit freeze branch from bonus damage and its effect pool.
            var hit = new DamageContainer(10f, "fixture") { enemy = enemy, element = EElement.Ice, procCoefficient = .5f };
            int events = 0;
            onFreeze = () => events++;
            ItemIceCube.A_FreezeEnemy += onFreeze;
            global::Utility.MyRandom.random = new FixedRandom(threshold + .001);
            inventory.PostDamage(hit);
            Check(!enemy.debuffsToAdd.ContainsKey(EDebuff.Freeze) && events == 0, "roll above scaled chance misses");
            global::Utility.MyRandom.random = new FixedRandom(threshold - .001);
            inventory.PostDamage(hit);
            Check(enemy.debuffsToAdd.TryGetValue(EDebuff.Freeze, out var pending), "roll below scaled chance queues freeze");
            Near(pending.duration, 3f, "freeze duration");
            Check(events == 1 && pending.dc.enemy == enemy && pending.dc.damageSource == hit.damageSource, "event and copied damage context");
            enemy.debuffsToAdd.Clear();
            hit.procCoefficient = 0f;
            global::Utility.MyRandom.random = new FixedRandom(0);
            inventory.PostDamage(hit);
            Check(!enemy.debuffsToAdd.ContainsKey(EDebuff.Freeze) && events == 1, "zero coefficient misses at zero roll");
            Debug.Log("IceCubeProbabilityPortCheck passed: two-stack scaling and ice-hit freeze probability boundaries");
        }
        finally
        {
            if (onFreeze != null) ItemIceCube.A_FreezeEnemy -= onFreeze;
            inventory.Cleanup();
            global::Utility.MyRandom.random = previousRandom;
            DataManager.Instance = previousData;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
