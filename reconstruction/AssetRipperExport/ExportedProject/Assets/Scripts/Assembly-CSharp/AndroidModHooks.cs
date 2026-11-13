using Assets.Scripts.Menu.Shop;
using System;
using System.Collections.Generic;

// Version 1 hooks have multiplicative semantics and are applied at player init.
public static class AndroidModHooks
{
    // API 1 remains a single hook; API 2 is a bounded ordered list.
    public static bool TryGetStats(int api, string hook, string[] hooks, out EStat[] stats)
    {
        stats = null;
        if (api == 1)
        {
            if (!TryGetStat(hook, out var stat)) return false;
            stats = new[] { stat }; return true;
        }
        if (api != 2 || hooks == null || hooks.Length < 1 || hooks.Length > 112) return false;
        var seen = new HashSet<(EStat, bool)>(); var result = new EStat[hooks.Length];
        for (int i = 0; i < hooks.Length; i++)
            if (!TryGetStat(hooks[i], out result[i]) || !seen.Add((result[i], IsAdditive(hooks[i])))) return false;
        stats = result; return true;
    }

    public static bool TryGetStat(string hook, out EStat stat)
    {
        if (hook != null && (hook.StartsWith("stat_add:", StringComparison.Ordinal) || hook.StartsWith("stat_mul:", StringComparison.Ordinal)))
        {
            string name = hook.Substring(9);
            if (Enum.TryParse(name, false, out stat) && Enum.IsDefined(typeof(EStat), stat) && stat != EStat.Unused0 && Enum.GetName(typeof(EStat), stat) == name) return true;
            stat = default; return false;
        }
        switch (hook)
        {
            case "move_speed_multiplier": stat = EStat.MoveSpeedMultiplier; return true;
            case "damage_multiplier": stat = EStat.DamageMultiplier; return true;
            case "attack_speed_multiplier": stat = EStat.AttackSpeed; return true;
            case "xp_multiplier": stat = EStat.XpIncreaseMultiplier; return true;
            case "gold_multiplier": stat = EStat.GoldIncreaseMultiplier; return true;
            case "pickup_range_multiplier": stat = EStat.PickupRange; return true;
            default: stat = default; return false;
        }
    }

    public static bool IsAdditive(string hook) => hook != null && hook.StartsWith("stat_add:", StringComparison.Ordinal);
}
