package com.megabonk.launcher;

import java.util.Arrays;
import java.util.HashSet;
import org.json.JSONArray;
import org.json.JSONObject;

final class LauncherModHookValidation {
    static boolean validModHooks(JSONObject manifest) {
        int api = manifest.optInt("api", 0);
        if (manifest.has("refresh") && !manifest.isNull("refresh") &&
            (!(manifest.opt("refresh") instanceof String) || api != 2 || !"level_up".equals(manifest.optString("refresh")))) return false;
        if (api == 1) return startingTome(manifest.optString("hook", "")) || "unlock_all".equals(manifest.optString("hook", "")) || "collect_spawned_xp".equals(manifest.optString("hook", "")) || "auto_open_pots".equals(manifest.optString("hook", "")) || "item_lab".equals(manifest.optString("hook", "")) || "chest_tracker".equals(manifest.optString("hook", "")) || hookStat(manifest.optString("hook", "")) != null;
        JSONArray hooks = manifest.optJSONArray("hooks");
        if (api != 2 || hooks == null || hooks.length() < 1 || hooks.length() > 112) return false;
        HashSet<String> seen = new HashSet<>();
        for (int i = 0; i < hooks.length(); i++) {
            String hook = hooks.optString(i, "");
            String stat = hookStat(hook);
            if (stat == null || !seen.add(stat + (hook.startsWith("stat_add:") ? ":add" : ":mul"))) return false;
        }
        return true;
    }

    private static final HashSet<String> TOMES = new HashSet<>(Arrays.asList(
        "Damage", "Agility", "Cooldown", "Quantity", "Knockback", "Armor", "Health", "Regeneration", "Size", "ProjectileSpeed", "Duration", "Evasion", "Attraction", "Luck", "Xp", "Golden", "Precision", "Shield", "Blood", "Thorns", "Bounce", "Cursed", "Silver", "Balance", "Chaos", "Gambler", "Hoarder"));
    private static boolean startingTome(String hook) {
        return hook.startsWith("starting_tome:") && TOMES.contains(hook.substring("starting_tome:".length()));
    }

    private static final HashSet<String> LUA_STATS = new HashSet<>(Arrays.asList(
        "MaxHealth", "HealthRegen", "Shield", "Thorns", "Armor", "Evasion", "Evolve", "DamageReductionMultiplier", "DamageCooldownMultiplier",
        "SizeMultiplier", "DurationMultiplier", "ProjectileSpeedMultiplier", "DamageMultiplier", "EffectDurationMultiplier", "AttackSpeed",
        "Projectiles", "Lifesteal", "CritChance", "CritDamage", "FireDamage", "IceDamage", "LightningDamage", "EliteDamageMultiplier",
        "KnockbackMultiplier", "MoveSpeedMultiplier", "JumpHeight", "FallDamageReduction", "Slam", "PickupRange", "Luck", "GoldIncreaseMultiplier",
        "XpIncreaseMultiplier", "ChestIncreaseMultiplier", "ChestPriceMultiplier", "ShopPriceReduction", "Holiness", "Wickedness", "Difficulty",
        "EliteSpawnIncrease", "PowerupBoostMultiplier", "PowerupChance", "BurnChance", "FreezeChance", "WeaponBurstCooldown", "ProjectileBounces",
        "ExtraJumps", "Overheal", "HealingMultiplier", "SilverIncreaseMultiplier", "EnemyAmountMultiplier", "EnemySizeMultiplier", "EnemySpeedMultiplier",
        "EnemyHpMultiplier", "EnemyDamageMultiplier", "EnemyScalingMultiplier", "PoisonDamageMultiplier"));

    private static String hookStat(String hook) {
        switch (hook) {
            case "move_speed_multiplier": return "MoveSpeedMultiplier";
            case "damage_multiplier": return "DamageMultiplier";
            case "attack_speed_multiplier": return "AttackSpeed";
            case "xp_multiplier": return "XpIncreaseMultiplier";
            case "gold_multiplier": return "GoldIncreaseMultiplier";
            case "pickup_range_multiplier": return "PickupRange";
            default:
                if (hook.startsWith("stat_add:") || hook.startsWith("stat_mul:")) {
                    String stat = hook.substring(9);
                    return LUA_STATS.contains(stat) ? stat : null;
                }
                return null;
        }
    }

}
