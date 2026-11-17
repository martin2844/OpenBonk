using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;

// Per-inventory idempotency: stage transitions can call Spawn again on the same run.
public static class OpenBonkStartingTomes
{
    private static readonly ConditionalWeakTable<TomeInventory, HashSet<string>> Granted = new ConditionalWeakTable<TomeInventory, HashSet<string>>();

    public static bool TryParseHook(int api, string hook, out ETome tome)
    {
        tome = default;
        const string prefix = "starting_tome:";
        if (api != 1 || hook == null || !hook.StartsWith(prefix, StringComparison.Ordinal)) return false;
        string name = hook.Substring(prefix.Length);
        return Enum.TryParse(name, false, out tome) && Enum.IsDefined(typeof(ETome), tome) && Enum.GetName(typeof(ETome), tome) == name;
    }

    public static bool ValidValues(double bonus, double levels) =>
        !double.IsNaN(bonus) && !double.IsInfinity(bonus) && bonus >= -10000 && bonus <= 10000 &&
        !double.IsNaN(levels) && !double.IsInfinity(levels) && levels >= 1 && levels <= 99 && levels == Math.Truncate(levels);

    public static bool Grant(TomeInventory inventory, string modId, TomeData tome, double bonus, double levels)
    {
        if (inventory == null || string.IsNullOrEmpty(modId) || tome == null || tome.statModifier == null || !ValidValues(bonus, levels)) return false;
        var granted = Granted.GetOrCreateValue(inventory);
        if (!granted.Add(modId)) return false;
        var first = new List<StatModifier> { new StatModifier { stat = tome.statModifier.stat, modifyType = EStatModifyType.Flat, modification = (float)bonus } };
        inventory.AddTome(tome, first, ERarity.New);
        for (int i = 1; i < (int)levels; i++) inventory.AddTome(tome, new List<StatModifier>(), ERarity.Legendary);
        return true;
    }
}
