using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Menu.Shop;

// An external weapon declares a host effect and supplies its behavior in Lua.
// This first effect is intentionally limited; it is not a complete mod API.
public sealed class OpenBonkLuaWeaponData : WeaponData
{
    public string effect = "lightning";
    public string externalId;
    public string displayName;
    public string description;
    [TextArea] public string luaSource;
    public float chainRange = 7f;
    public float chainProcCoefficient = .5f;
    public bool hasLightningColor;
    public Color lightningColor = Color.white;
    public AudioClip lightningSound;
    public override string GetName() => displayName ?? string.Empty;
    public override string GetDescription() => description ?? string.Empty;
    public override string GetInternalName() => externalId ?? string.Empty;

    // Lightning's visual effect does not use projectile speed or duration;
    // offer only stats that this host effect actually reads.
    public static UpgradeData CreateUpgrades(UpgradeData template, bool projectile = false)
    {
        var result = ScriptableObject.CreateInstance<UpgradeData>();
        result.upgradeModifiers = new List<StatModifier>();
        if (template != null && template.upgradeModifiers != null)
            foreach (var modifier in template.upgradeModifiers)
                if (modifier != null && (modifier.stat == EStat.DamageMultiplier || modifier.stat == EStat.AttackSpeed ||
                    modifier.stat == EStat.Projectiles || modifier.stat == EStat.ProjectileBounces || modifier.stat == EStat.SizeMultiplier ||
                    modifier.stat == EStat.CritChance || modifier.stat == EStat.CritDamage || modifier.stat == EStat.KnockbackMultiplier ||
                    (projectile && (modifier.stat == EStat.DurationMultiplier || modifier.stat == EStat.ProjectileSpeedMultiplier))))
                    result.upgradeModifiers.Add(modifier);
        if (result.upgradeModifiers.Count == 0)
            result.upgradeModifiers.Add(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = .1f });
        return result;
    }
}
