using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using UnityEngine;

public static class ItemGrantPolicyCheck
{
    private static void Require(bool condition, string detail) { if (!condition) throw new Exception("Item grant: " + detail); }
    public static void Run()
    {
        var previous = DataManager.Instance;
        var root = new GameObject("item grant test");
        var item = ScriptableObject.CreateInstance<ItemData>();
        var inventory = new ItemInventory();
        Action<EItem> record = null, throws = null;
        HashSet<string> modifiers = null;
        bool alreadyRestricted = false;
        try
        {
            var manager = root.AddComponent<DataManager>(); DataManager.Instance = manager;
            item.eItem = EItem.Clover; item.isEnabled = true;
            var field = typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic);
            ((Dictionary<EItem, ItemData>)field.GetValue(manager)).Add(EItem.Clover, item);
            int events = 0; record = _ => events++; ItemInventory.A_ItemAdded += record;
            OpenBonkItemGrantPolicy.SetEnabled(false);
            Require(!OpenBonkItemGrantPolicy.TryGrant(inventory, manager, EItem.Clover, 1, out var off) && off == 0 && inventory.GetAmount(EItem.Clover) == 0, "disabled");
            OpenBonkItemGrantPolicy.SetEnabled(true);
            Require(!OpenBonkItemGrantPolicy.TryGrant(inventory, manager, EItem.Clover, 0, out _) &&
                    !OpenBonkItemGrantPolicy.TryGrant(inventory, manager, EItem.Clover, 51, out _) &&
                    !OpenBonkItemGrantPolicy.TryGrant(inventory, manager, (EItem)123456789, 1, out _) &&
                    inventory.GetAmount(EItem.Clover) == 0, "invalid requests leave inventory unchanged");
            Require(OpenBonkItemGrantPolicy.TryGrant(inventory, manager, EItem.Clover, 3, out var added) &&
                    added == 3 && inventory.GetAmount(EItem.Clover) == 3 && events == 3, "normal grants and events");
            var challengeField = typeof(ChallengesTracker).GetField("modifierNames", BindingFlags.Static | BindingFlags.NonPublic);
            modifiers = (HashSet<string>)challengeField.GetValue(null);
            alreadyRestricted = modifiers.Contains(ChallengesTracker.MODIFIER_NO_ITEMS);
            modifiers.Add(ChallengesTracker.MODIFIER_NO_ITEMS);
            Require(!OpenBonkItemGrantPolicy.TryGrant(inventory, manager, EItem.Clover, 1, out var denied) &&
                    denied == 0 && inventory.GetAmount(EItem.Clover) == 3 && events == 3, "challenge restrictions remain effective");
            if (!alreadyRestricted) modifiers.Remove(ChallengesTracker.MODIFIER_NO_ITEMS);
            inventory.items[EItem.Clover].amount = 999;
            Require(!OpenBonkItemGrantPolicy.TryGrant(inventory, manager, EItem.Clover, 1, out _) &&
                    inventory.GetAmount(EItem.Clover) == 999 && events == 3, "total limit");
            inventory.items[EItem.Clover].amount = 3;
            throws = _ => throw new InvalidOperationException("probe event failure"); ItemInventory.A_ItemAdded += throws;
            Require(!OpenBonkItemGrantPolicy.TryGrant(inventory, manager, EItem.Clover, 3, out var partial) &&
                    partial == 1 && inventory.GetAmount(EItem.Clover) == 4, "partial native failure reported, not rolled back");
            ItemInventory.A_ItemAdded -= throws; throws = null;
            Require(OpenBonkItemGrantPolicy.TryGrant(inventory, manager, EItem.Clover, 1, out var retry) &&
                    retry == 1 && inventory.GetAmount(EItem.Clover) == 5, "usable after exception");
            Debug.Log("ItemGrantPolicyCheck PASS: opt-in, bounds, native events and partial failure");
        }
        finally
        {
            if (record != null) ItemInventory.A_ItemAdded -= record;
            if (throws != null) ItemInventory.A_ItemAdded -= throws;
            if (modifiers != null && !alreadyRestricted) modifiers.Remove(ChallengesTracker.MODIFIER_NO_ITEMS);
            inventory.Cleanup(); OpenBonkItemGrantPolicy.SetEnabled(false); DataManager.Instance = previous;
            UnityEngine.Object.DestroyImmediate(item); UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
