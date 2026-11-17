using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;

// Shared by the Android content loader and editor verification. Omitted fields
// preserve the historical Fox defaults; an explicit empty array replaces them.
public static class OpenBonkCharacterBaseConfig
{
    [Serializable, UnityEngine.Scripting.Preserve]
    public sealed class StatSpec { public string stat, mode; public double? amount; }

    [Serializable, UnityEngine.Scripting.Preserve]
    public sealed class ColliderSpec { public double? height, width; }

    public sealed class Validated
    {
        internal List<StatModifier> stats;
        internal float? height, width;
    }

    private static bool Between(double value, double minimum, double maximum) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value >= minimum && value <= maximum;

    public static Validated Validate(StatSpec[] specs, ColliderSpec collider)
    {
        var result = new Validated();
        if (specs != null)
        {
            if (specs.Length > 64) throw new InvalidDataException("too many character base stats");
            result.stats = new List<StatModifier>(specs.Length);
            var seen = new HashSet<(EStat, EStatModifyType)>();
            foreach (var spec in specs)
            {
                if (spec == null || !Enum.TryParse(spec.stat, false, out EStat stat) ||
                    !Enum.IsDefined(typeof(EStat), stat) || stat == EStat.Unused0 || stat.ToString() != spec.stat ||
                    !Enum.TryParse(spec.mode, false, out EStatModifyType mode) ||
                    !Enum.IsDefined(typeof(EStatModifyType), mode) || mode.ToString() != spec.mode ||
                    !seen.Add((stat, mode)) || !spec.amount.HasValue)
                    throw new InvalidDataException("invalid character base stat");
                double amount = spec.amount.Value;
                double min = mode == EStatModifyType.Multiplication ? 0 : mode == EStatModifyType.Addition ? -1 : -10000;
                double max = mode == EStatModifyType.Flat ? 10000 : 1000;
                if (!Between(amount, min, max)) throw new InvalidDataException("invalid character base stat amount");
                result.stats.Add(new StatModifier { stat = stat, modifyType = mode, modification = (float)amount });
            }
        }
        if (collider != null)
        {
            if (!collider.height.HasValue || !collider.width.HasValue ||
                !Between(collider.height.Value, .1, 20) || !Between(collider.width.Value, .05, 10) ||
                collider.height.Value < 2 * collider.width.Value)
                throw new InvalidDataException("invalid character collider");
            result.height = (float)collider.height.Value;
            result.width = (float)collider.width.Value;
        }
        return result;
    }

    public static void Apply(CharacterData target, CharacterData fallback, Validated config)
    {
        if (target == null || fallback == null || config == null) throw new ArgumentNullException();
        target.statModifiers = config.stats ?? (fallback.statModifiers == null ? new List<StatModifier>() : new List<StatModifier>(fallback.statModifiers));
        target.colliderHeight = config.height ?? fallback.colliderHeight;
        target.colliderWidth = config.width ?? fallback.colliderWidth;
    }
}
