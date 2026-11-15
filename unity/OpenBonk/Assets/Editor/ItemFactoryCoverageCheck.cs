using System;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using UnityEngine;

public static class ItemFactoryCoverageCheck
{
    public static void Run()
    {
        var oldData = DataManager.Instance;
        ItemInventory inventory = null;
        try
        {
            DataManager.Instance = null;
            inventory = new ItemInventory();
            var ids = (EItem[])Enum.GetValues(typeof(EItem));
            if (ids.Length != 88) throw new Exception("Unexpected item enum size");
            int passive = 0;
            foreach (var id in ids)
            {
                var item = ItemFactory.CreateItem(id, inventory);
                if (item == null) throw new Exception("Item factory returned null for " + id);
                bool expectedPassive = id == EItem.CageKey || id == EItem.GoldenRing || id == EItem.CryptKey;
                if ((item is ItemNoImplementation) != expectedPassive)
                    throw new Exception("Unexpected no-effect item implementation for " + id);
                if (expectedPassive) passive++;
            }
            if (passive != 3) throw new Exception("Unexpected no-effect item count");
            Debug.Log("ItemFactoryCoverageCheck passed: 88 item IDs constructed");
        }
        finally
        {
            inventory?.Cleanup();
            DataManager.Instance = oldData;
        }
    }
}
