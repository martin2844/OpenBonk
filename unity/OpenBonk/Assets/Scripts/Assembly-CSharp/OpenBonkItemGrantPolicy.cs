using System;
using Assets.Scripts.Inventory__Items__Pickups.Items;

/// <summary>Bounded, opt-in use of the game's normal item-inventory path.</summary>
public static class OpenBonkItemGrantPolicy
{
    public const int MaxPerAction = 50;
    public const int MaxPerItem = 999;
    public static bool Enabled { get; private set; }
    public static void SetEnabled(bool enabled) { Enabled = enabled; }

    // Reports the amount actually delivered, including if a native item effect
    // throws midway. Rolling back arbitrary item effects is not generally safe.
    public static bool TryGrant(ItemInventory inventory, DataManager data, EItem id, int count, out int delivered)
    {
        delivered = 0;
        if (!Enabled || inventory == null || data == null || count < 1 || count > MaxPerAction ||
            !data.TryGetItem(id, out var item) || item == null || !item.isEnabled) return false;
        int before = inventory.GetAmount(id);
        if (before < 0 || before > MaxPerItem - count) return false;
        try
        {
            inventory.AddItem(id, count);
            delivered = Math.Max(0, inventory.GetAmount(id) - before);
            return delivered == count;
        }
        catch (Exception)
        {
            delivered = Math.Max(0, inventory.GetAmount(id) - before);
            return false;
        }
    }
}
