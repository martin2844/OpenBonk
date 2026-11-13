using System;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class ItemRarityColorPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("ItemRarityColorPortCheck: " + message); }
    public static void Run()
    {
        var expected = new[] { "4DFF97FF", "0CA7FAFF", "FF00F4FF", "FFE300FF", "FF0000FF", "FFFFFFFF" };
        for (int i = 0; i < expected.Length; i++)
        {
            var rarity = (EItemRarity)i; var color = MyColorUtility.GetItemRarityColor(rarity);
            Require(ColorUtility.ToHtmlStringRGBA(color) == expected[i], "original color for " + rarity);
            Require(MyColorUtility.GetRarityColorBackground(rarity) == Color.Lerp(color, Color.black, .8f), "darkened background for " + rarity);
        }
        Require(MyColorUtility.GetItemRarityColor((EItemRarity)99) == Color.gray, "unknown rarity fallback");
        var beacon = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Beacon.asset"); var wrench = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Wrench.asset");
        Require(beacon.rarity == EItemRarity.Rare && beacon.GetColor() == MyColorUtility.GetItemRarityColor(EItemRarity.Rare), "original Beacon asset color");
        Require(wrench.rarity == EItemRarity.Common && wrench.GetColor() == MyColorUtility.GetItemRarityColor(EItemRarity.Common), "original Wrench asset color");
        Debug.Log("ItemRarityColorPortCheck passed: original item rarity palette, backgrounds and asset colors");
    }
}
