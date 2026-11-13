using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Menu.Shop;
using UnityEditor;
using UnityEngine;

public static class WeaponMathPortCheck
{
    class PreAttackItem : ItemBase
    {
        public int calls;
        public PreAttackItem(ItemInventory inventory) : base(inventory) { }
        public override void Init() { }
        public override void Cleanup() { }
        protected override void OnInitOrAmountChanged() { }
        public override void Tick() { }
        public override void PreAttack(DamageContainer dc, StatComponents modifier) { calls++; modifier.AddAdditive(0.3f); }
        public override void ProcOnHitEffects(DamageContainer dc) { }
        public override bool HasOnHitEffectProc() { return false; }
        public override bool HasPreAttackProc() { return true; }
    }
    static void Equal(float actual, float expected, string name)
    {
        if (Mathf.Abs(actual - expected) > 0.0001f) throw new Exception(name + ": expected " + expected + ", got " + actual);
    }
    public static void Run()
    {
        var manager = new GameObject("manager").AddComponent<GameManager>();
        GameManager.Instance = manager;
        var player = new GameObject("player").AddComponent<MyPlayer>();
        MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
        player.inventory.playerStats = stats;
        stats.stats = new Dictionary<EStat, float> {
            {EStat.DamageMultiplier, 1.5f}, {EStat.Thorns, 4f}, {EStat.ProjectileBounces, 2.8f},
            {EStat.ProjectileSpeedMultiplier, 1.25f}, {EStat.CritChance, 0.2f}, {EStat.CritDamage, 1.5f},
            {EStat.KnockbackMultiplier, 2f}
        };
        stats.statValuesMap = new Dictionary<EStat, StatComponents>();
        var components = new StatComponents(); components.SetValues(1f, 1f, 1.5f);
        stats.statValuesMap[EStat.DamageMultiplier] = components;
        var data = ScriptableObject.CreateInstance<WeaponData>();
        data.eWeapon = EWeapon.FireStaff; data.damage = 10f; data.knockback = 3f;
        data.critChance = 0.1f; data.projectileBounces = 1; data.projectileSpeed = 0.5f; data.Init();
        var weapon = new WeaponBase(data);
        Equal(WeaponUtility.GetDamage(weapon), 15f, "weapon damage");
        Equal(WeaponUtility.GetProjectileBounces(weapon), 3, "projectile bounces");
        Equal(WeaponUtility.GetProjectileSpeed(weapon), 0.625f, "projectile speed");
        Equal(WeaponUtility.GetCritChance(weapon), 0.3f, "critical chance");
        Equal(WeaponUtility.GetCritDamageMultiplier(weapon), 1.5f, "critical damage");
        Equal(WeaponUtility.GetKnockback(weapon), 6f, "knockback");
        if (DamageUtility.GetCritDamageMultiplier(0f, out float noCrit) || noCrit != 1f) throw new Exception("zero critical chance");
        if (!DamageUtility.GetCritDamageMultiplier(1f, out float crit1)) throw new Exception("one critical level");
        if (!DamageUtility.GetCritDamageMultiplier(2f, out float crit2)) throw new Exception("two critical levels");
        if (!DamageUtility.GetCritDamageMultiplier(3f, out float crit3)) throw new Exception("three critical levels");
        Equal(crit1, 2f, "critical multiplier 1");
        Equal(crit2, 4f, "critical multiplier 2");
        Equal(crit3, 6.25f, "critical multiplier 3");
        int toggles = 0;
        WeaponInventory.A_WeaponToggled += _ => toggles++;
        var inventory = new WeaponInventory();
        inventory.weapons.Add(EWeapon.FireStaff, weapon);
        inventory.ToggleWeapon(EWeapon.FireStaff, false);
        if (weapon.enabled || toggles != 1) throw new Exception("weapon disable event");
        inventory.ToggleWeapon(EWeapon.FireStaff, true);
        inventory.ToggleWeapon(EWeapon.Bone, false);
        if (!weapon.enabled || toggles != 2) throw new Exception("weapon enable or missing weapon");
        data.baseStats[EStat.DamageMultiplier] = 12f;
        weapon.WeaponPassiveChanged(EStat.DamageMultiplier);
        Equal(weapon.GetValue(EStat.DamageMultiplier), 12f, "passive stat refresh");
        data.baseStats[EStat.DamageMultiplier] = 10f;
        weapon.WeaponPassiveChanged(EStat.DamageMultiplier);
        var projectile = new GameObject("projectile").AddComponent<ProjectileBasic>();
        projectile.weaponBase = weapon;
        Equal(WeaponUtility.GetDamageProjectile(projectile), 15f, "projectile damage");
        data.eWeapon = EWeapon.Aegis;
        Equal(WeaponUtility.GetDamage(weapon), 21f, "aegis thorns");
        var forced = typeof(WeaponUtility).GetMethod("GetWeaponDamage", BindingFlags.Static | BindingFlags.NonPublic);
        Equal((float)forced.Invoke(null, new object[] {weapon, 7f}), 7f, "forced damage");
        Equal((float)forced.Invoke(null, new object[] {weapon, -0.995f}), 14f, "forced damage near sentinel above");
        Equal((float)forced.Invoke(null, new object[] {weapon, -1.005f}), 14f, "forced damage near sentinel below");
        Equal((float)forced.Invoke(null, new object[] {weapon, -0.98f}), -0.98f, "forced damage outside sentinel above");
        Equal((float)forced.Invoke(null, new object[] {weapon, -1.02f}), -1.02f, "forced damage outside sentinel below");
        var modifier = new StatComponents(); modifier.Recycle(); modifier.AddAdditive(0.2f);
        var newDamage = typeof(WeaponUtility).GetMethod("GetNewDamage", BindingFlags.Static | BindingFlags.NonPublic);
        Equal((float)newDamage.Invoke(null, new object[] {10f, modifier}), 18f, "item-modified damage");
        var items = new ItemInventory();
        var item = new PreAttackItem(items);
        items.items.Add(default(EItem), item);
        var preAttackItems = (HashSet<EItem>)typeof(ItemInventory).GetField("itemsWithPreAttackProcs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(items);
        preAttackItems.Add(default(EItem));
        modifier.AddFlat(99f);
        var returned = items.PreAttack(new DamageContainer(1f, "test"), modifier);
        if (!ReferenceEquals(returned, modifier) || item.calls != 1 || !modifier.hasModifications) throw new Exception("item pre-attack dispatch");
        Equal(modifier.baseValue, 0f, "item modifier reset");
        Equal(modifier.additiveValue, 0.3f, "item pre-attack modifier");
        player.inventory.itemInventory = items;
        data.eWeapon = EWeapon.FireStaff;
        data.element = EElement.Fire;
        data.procCoefficient = 0.7f;
        stats.stats[EStat.CritChance] = 0.9f;
        var enemy = new GameObject("enemy").AddComponent<Enemy>();
        enemy.transform.position = new Vector3(10f, 20f, 30f);
        typeof(Enemy).GetProperty("meshHeight").GetSetMethod(true).Invoke(enemy, new object[] {4f});
        enemy.renderer = new GameObject("enemy-renderer").AddComponent<MeshRenderer>();
        enemy.renderer.transform.position = new Vector3(10f, 18f, 30f);
        Equal(enemy.GetCenterPosition().y, 20f, "enemy center");
        Equal(enemy.GetFeetPosition().y, 18f, "enemy feet");
        Equal(enemy.GetHeadPosition().y, 22.8f, "enemy head");
        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        enemyData.canBeExecuted = true;
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] {enemyData});
        enemy.hp = 23f; enemy.maxHp = 100f;
        var weaponDc = WeaponUtility.GetDamageContainer(weapon, projectile, enemy, new Vector3(3f, 4f, 5f));
        if (!weaponDc.crit || !weaponDc.canProcJoe || weaponDc.enemy != enemy || weaponDc.element != EElement.Fire || weaponDc.damageSource != "FireStaff") throw new Exception("weapon container fields");
        Equal(weaponDc.direction.x, 3f, "weapon direction x");
        Equal(weaponDc.direction.y, 0f, "weapon direction y");
        Equal(weaponDc.direction.z, 5f, "weapon direction z");
        Equal(weaponDc.procCoefficient, 0.7f, "weapon proc coefficient");
        Equal(weaponDc.knockback, 6f, "weapon container knockback");
        Equal(weaponDc.damage, 58.5f, "weapon container critical damage");
        var reusedWeaponDc = WeaponUtility.GetDamageContainer(weapon, projectile, enemy, Vector3.up, 4f);
        if (!ReferenceEquals(weaponDc, reusedWeaponDc)) throw new Exception("weapon container reuse");
        Equal(reusedWeaponDc.damage, 23.4f, "forced weapon container damage");
        // Exercise the actual weapon damage path with both outcomes of a
        // fractional crit chance, not just the guaranteed critical case.
        stats.stats[EStat.CritChance] = .4f;
        var savedRandom = Utility.MyRandom.random;
        var expectedRandom = new System.Random(64123);
        Utility.MyRandom.random = new System.Random(64123);
        int criticalHits = 0;
        try
        {
            for (int i = 0; i < 200; i++)
            {
                bool expectedCritical = expectedRandom.NextDouble() < WeaponUtility.GetCritChance(weapon);
                var rolled = WeaponUtility.GetDamageContainer(weapon, projectile, enemy, Vector3.forward);
                if (rolled.crit != expectedCritical) throw new Exception("fractional weapon crit at " + i);
                Equal(rolled.damage, expectedCritical ? 58.5f : 19.5f, "fractional weapon damage " + i);
                if (rolled.crit) criticalHits++;
            }
            if (criticalHits == 0 || criticalHits == 200) throw new Exception("fractional crit did not exercise both outcomes");
        }
        finally { Utility.MyRandom.random = savedRandom; }
        stats.stats[EStat.CritChance] = 0f;
        Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.thornsDamageSource = "test-thorns";
        var supplied = new DamageContainer(0f, "supplied");
        var other = WeaponUtility.GetDamageContainer(supplied, 4f, 0.25f, "test-thorns", new Vector3(1f, 2f, 3f), enemy);
        if (ReferenceEquals(other, supplied) || other.crit || !other.canProcJoe || other.enemy != enemy) throw new Exception("other container fields");
        Equal(other.direction.y, 2f, "other direction preserved");
        Equal(other.damage, 7.8f, "other container damage");
        var reusedOther = WeaponUtility.GetDamageContainer(null, 2f, 1f, "other", Vector3.zero, enemy);
        if (!ReferenceEquals(other, reusedOther) || reusedOther.canProcJoe) throw new Exception("other container reset");
        DamageUtility.ApplyExecute(reusedOther);
        if (!reusedOther.isExecute || reusedOther.damageEffect != EDamageEffect.Execute) throw new Exception("execute flag");
        Equal(reusedOther.damage, 23f, "execute eligible enemy");
        enemyData.canBeExecuted = false;
        DamageUtility.ApplyExecute(reusedOther);
        Equal(reusedOther.damage, 2f, "execute protected enemy");
        // Original IL2CPP truncates weapon and player bounces independently.
        data.baseStats[EStat.ProjectileBounces] = 1.8f;
        weapon.WeaponPassiveChanged(EStat.ProjectileBounces);
        stats.stats[EStat.ProjectileBounces] = 2.8f;
        Equal(WeaponUtility.GetProjectileBounces(weapon), 3, "fractional bounces truncate separately");
        // The original formats the raw upgrade value, then truncates at attack time.
        data.baseStats[EStat.Projectiles] = 2.2f;
        weapon.WeaponPassiveChanged(EStat.Projectiles);
        stats.stats[EStat.Projectiles] = 0f;
        Equal(WeaponUtility.GetAttackQuantity(weapon), 2, "displayed fractional projectile fires two");
        var oldCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            string displayed = StatUtility.GetWeaponModificationString(EStatModifyType.Flat, EStat.Projectiles, 2.2f);
            if (displayed != "2.2") throw new Exception("projectile preview value: " + displayed);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = oldCulture; }
        data.baseStats[EStat.Projectiles] = 2.8f;
        weapon.WeaponPassiveChanged(EStat.Projectiles);
        stats.stats[EStat.Projectiles] = 2.8f;
        Equal(WeaponUtility.GetAttackQuantity(weapon), 4, "fractional projectile quantity");
        stats.stats[EStat.Projectiles] = -9f;
        Equal(WeaponUtility.GetAttackQuantity(weapon), 1, "projectile quantity minimum");
        data.eWeapon = EWeapon.Shotgun;
        Equal(WeaponUtility.GetAttackQuantity(weapon), 2, "shotgun fixed quantity");
        data.eWeapon = EWeapon.FireStaff;
        stats.stats[EStat.SizeMultiplier] = 1.5f;
        data.maxSizeMultiplier = 1.2f;
        Equal(WeaponUtility.GetAttackSizeMultiplier(weapon), 1.2f, "size cap");
        data.maxSizeMultiplier = 1f;
        Equal(WeaponUtility.GetAttackSizeMultiplier(weapon), 1.5f, "size cap disabled at one");
        data.baseStats[EStat.DurationMultiplier] = 2f;
        weapon.WeaponPassiveChanged(EStat.DurationMultiplier);
        stats.stats[EStat.DurationMultiplier] = 1.5f;
        data.maxDuration = 2.5f;
        Equal(WeaponUtility.GetDuration(weapon), 2.5f, "duration cap");
        data.maxDuration = 0f;
        Equal(WeaponUtility.GetDuration(weapon), 3f, "duration cap disabled at zero");
        stats.stats[EStat.AttackSpeed] = 2f;
        data.baseStats[EStat.AttackSpeed] = 0f;
        weapon.WeaponPassiveChanged(EStat.AttackSpeed);
        data.endCooldown = 2f;
        data.burstTime = 0f;
        Equal(WeaponUtility.GetWeaponCooldown(weapon), 100f, "minimum weapon attack speed");
        data.baseStats[EStat.AttackSpeed] = -0.5f;
        weapon.WeaponPassiveChanged(EStat.AttackSpeed);
        Equal(WeaponUtility.GetWeaponCooldown(weapon), 100f, "negative weapon attack speed minimum");
        data.baseStats[EStat.AttackSpeed] = 1f;
        weapon.WeaponPassiveChanged(EStat.AttackSpeed);
        Equal(WeaponUtility.GetWeaponCooldown(weapon), 1f, "ordinary weapon cooldown");
        stats.stats[EStat.Projectiles] = 2f;
        data.burstTime = 2f;
        data.minBurstInterval = .1f;
        Equal(WeaponUtility.GetBurstInterval(weapon), .25f, "attack burst interval");
        Equal(WeaponUtility.GetWeaponCooldown(weapon), 2f, "weapon cooldown with burst");
        data.minBurstInterval = 2f;
        Equal(WeaponUtility.GetBurstInterval(weapon), 1f, "minimum burst interval");
        Equal(WeaponUtility.GetWeaponCooldown(weapon), 5f, "weapon cooldown with minimum burst");
        data.spawnProjectileRange = 30f;
        bool oldFinalFight = FinalFightController.isFightingFinalBoss;
        try {
            FinalFightController.isFightingFinalBoss = false;
            Equal(WeaponUtility.GetWeaponRange(weapon), 30f, "ordinary weapon range");
            FinalFightController.isFightingFinalBoss = true;
            Equal(WeaponUtility.GetWeaponRange(weapon), 39.9f, "final boss weapon range");
        } finally { FinalFightController.isFightingFinalBoss = oldFinalFight; }
        items.Cleanup();
        Debug.Log("WEAPON_MATH_PORT_CHECK_OK");
    }
}
