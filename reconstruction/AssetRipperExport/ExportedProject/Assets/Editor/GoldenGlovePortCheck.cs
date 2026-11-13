using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using UnityEditor;
using UnityEngine;
using Utility;

public static class GoldenGlovePortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Rolls(ItemGoldenGlove glove, float chance, int extra, int seed)
    {
        MyRandom.random = new System.Random(seed); var expected = new System.Random(seed);
        for (int i = 0; i < 1000; i++)
        {
            int value = extra + (expected.NextDouble() < chance ? 1 : 0);
            Require(glove.GetExtraGold() == value, "Mismatch at roll " + i + " with chance " + chance + " and extra " + extra);
        }
    }
    public static void Run()
    {
        var root = new GameObject("golden glove fixture"); root.SetActive(false);
        var previousData = DataManager.Instance; var previousRandom = MyRandom.random;
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inventory = new ItemInventory();
        try
        {
            var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Golden Glove.asset");
            Require(asset != null && asset.eItem == EItem.GoldenGlove && asset.isEnabled, "Original enabled Golden Glove asset");
            var allItems = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            allItems.Add(EItem.GoldenGlove, asset);
            inventory.AddItem(EItem.GoldenGlove);
            var glove = inventory.GetItem(EItem.GoldenGlove) as ItemGoldenGlove;
            Require(glove != null, "Factory creates Golden Glove");
            Rolls(glove, .15f, 0, 139);
            inventory.AddItem(EItem.GoldenGlove, 6); Rolls(glove, 7 * .15f - 1, 1, 140);
            inventory.AddItem(EItem.GoldenGlove, 7); Rolls(glove, 14 * .15f - 2, 2, 141);
            // The shipped routine only writes the overload field when chance exceeds one.
            for (int i = 0; i < 8; i++) inventory.RemoveItem(EItem.GoldenGlove, false);
            Rolls(glove, 6 * .15f, 1, 142);
            for (int i = 0; i < 6; i++) inventory.RemoveItem(EItem.GoldenGlove, false);
            Require(inventory.GetItem(EItem.GoldenGlove) == null, "Removal discards item");
            inventory.AddItem(EItem.GoldenGlove); glove = inventory.GetItem(EItem.GoldenGlove) as ItemGoldenGlove;
            Rolls(glove, .15f, 0, 143);
            Debug.Log("GoldenGlovePortCheck passed: real asset and inventory, deterministic rolls, overload, reduction and reacquisition");
        }
        finally { inventory.Cleanup(); MyRandom.random = previousRandom; DataManager.Instance = previousData; UnityEngine.Object.DestroyImmediate(root); }
    }
}
