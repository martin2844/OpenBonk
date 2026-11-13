using System;
using System.Runtime.Serialization;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEditor;
using UnityEngine;

public static class MaxedTomePortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("MaxedTomePortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("maxed tome test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory = inventory; inventory.itemInventory = new ItemInventory();
        var tome = AssetDatabase.LoadAssetAtPath<TomeData>("Assets/MonoBehaviour/TomeDamage.asset");
        var hat = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/WizardsHat.asset");
        Require(tome != null && hat != null, "original tome and hat assets");
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var items = (System.Collections.Generic.Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(data);
        items.Add(hat.eItem, hat);
        var tomes = new TomeInventory();
        var originalEvent = TomeInventory.A_TomeUpgrade;
        int calls = 0;
        TomeInventory.A_TomeUpgrade = (kind, stat) => { Require(kind == tome.eTome && stat == tome.statModifier.stat, "event arguments"); calls++; };
        try
        {
            tomes.AddMaxedTome(tome);
            Require(tomes.GetTomeLevel(tome.eTome) == 99, "base cap");
            Require(tomes.statToTomes[tome.statModifier.stat].Contains(tome.eTome), "stat index");
            Require(tomes.tomeUpgrade.Count == 0 && calls == 1, "native method emits event without creating a modifier");
            inventory.itemInventory.AddItem(EItem.WizardsHat);
            tomes.AddMaxedTome(tome);
            Require(tomes.GetTomeLevel(tome.eTome) == 109 && calls == 2, "existing tome reset to extended cap and notified");
            Require(tomes.statToTomes[tome.statModifier.stat].Count == 1, "repeat does not duplicate stat index");
            Debug.Log("MaxedTomePortCheck passed: original asset, base and extended caps, stat index and event");
        }
        finally { TomeInventory.A_TomeUpgrade = originalEvent; inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; DataManager.Instance = null; }
    }
}
