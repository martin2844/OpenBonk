using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using UnityEngine;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons
{
	public static class WeaponUtility
	{
		public static Action<DamageContainer> A_CreateDamageContainerPreAttack;

		private static StatComponents itemModifier = new StatComponents();

		private static DamageContainer weaponDc = new DamageContainer(0f, "");

		private static DamageContainer otherDc = new DamageContainer(0f, "");

		private static List<int> availableIndexes = new List<int>();

		public static DamageContainer GetDamageContainer(WeaponBase weaponBase, ProjectileBase projectile, Enemy enemy, Vector3 direction, float forceDamage = -1f)
		{
			WeaponData data = weaponBase.weaponData;
			weaponDc.Reuse(data.procCoefficient, data.damageSourceName);
			weaponDc.direction = new Vector3(direction.x, 0f, direction.z);
			weaponDc.enemy = enemy;
			weaponDc.element = data.element;
			weaponDc.knockback = GetKnockback(weaponBase);
			weaponDc.canProcJoe = true;
			float critMultiplier = GetDcCritMultiplier(GetCritChance(weaponBase), weaponDc);
			MyPlayer.Instance.inventory.itemInventory.PreAttack(weaponDc, itemModifier);
			weaponDc.damage = GetNewDamage(GetWeaponDamage(weaponBase, forceDamage) * (projectile != null ? projectile.damageMultiplier : 1f), itemModifier);
			if (weaponDc.crit) weaponDc.damage *= critMultiplier * GetCritDamageMultiplier(weaponBase);
			if (weaponDc.isExecute) DamageUtility.ApplyExecute(weaponDc);
			return weaponDc;
		}

		public static DamageContainer GetDamageContainer(DamageContainer recycleDc, float baseDamage, float procCoefficient, string damageSourceName, Vector3 direction, Enemy enemy)
		{
			otherDc.Reuse(procCoefficient, damageSourceName);
			otherDc.direction = direction;
			otherDc.enemy = enemy;
			if (damageSourceName == PlayerHealth.thornsDamageSource) otherDc.canProcJoe = true;
			float critMultiplier = GetDcCritMultiplier(GetCritChance(null), otherDc);
			MyPlayer.Instance.inventory.itemInventory.PreAttack(otherDc, itemModifier);
			otherDc.damage = GetNewDamage(baseDamage, itemModifier);
			if (otherDc.crit) otherDc.damage *= critMultiplier * GetCritDamageMultiplier(null);
			if (otherDc.isExecute) DamageUtility.ApplyExecute(otherDc);
			return otherDc;
		}

		private static float GetWeaponDamage(WeaponBase weaponBase, float forceDamage)
		{
			if (Mathf.Abs(forceDamage + 1f) > 0.01f) return forceDamage;
			float damage = weaponBase.GetValue(EStat.DamageMultiplier);
			if (weaponBase.weaponData.eWeapon == EWeapon.Aegis) damage += PlayerStats.GetStat(EStat.Thorns);
			return damage;
		}

		private static float GetNewDamage(float baseDamage, StatComponents itemModifierStatComponents)
		{
			float multiplier = itemModifierStatComponents.hasModifications
				? PlayerStats.GetStatComponents(EStat.DamageMultiplier).GetFinalValue(itemModifierStatComponents)
				: PlayerStats.GetStat(EStat.DamageMultiplier);
			return baseDamage * multiplier;
		}

		public static float GetDamage(WeaponBase weaponBase)
		{
			return GetWeaponDamage(weaponBase, -1f) * PlayerStats.GetStat(EStat.DamageMultiplier);
		}

		private static float GetDamage(float damage)
		{
			return damage * PlayerStats.GetStat(EStat.DamageMultiplier);
		}

		private static float GetDcCritMultiplier(float critChance, DamageContainer dc)
		{
			if (!DamageUtility.GetCritDamageMultiplier(critChance, out float multiplier)) return 1f;
			dc.crit = true;
			return multiplier;
		}

		public static float GetAttackSizeMultiplier(WeaponBase weaponBase)
		{
			float size = weaponBase.GetValue(EStat.SizeMultiplier) * PlayerStats.GetStat(EStat.SizeMultiplier);
			return weaponBase.weaponData.maxSizeMultiplier > 1f ? Mathf.Min(size, weaponBase.weaponData.maxSizeMultiplier) : size;
		}

		public static int GetAttackQuantity(WeaponBase weaponBase)
		{
			if (weaponBase.weaponData.eWeapon == EWeapon.Shotgun) return 2;
			return (int)Mathf.Max(1f, weaponBase.GetValue(EStat.Projectiles) + (int)PlayerStats.GetStat(EStat.Projectiles));
		}

		public static int GetProjectileBounces(WeaponBase weaponBase)
		{
			return (int)weaponBase.GetValue(EStat.ProjectileBounces) + (int)PlayerStats.GetStat(EStat.ProjectileBounces);
		}

		public static float GetProjectileSpeed(WeaponBase weaponBase)
		{
			return weaponBase.GetValue(EStat.ProjectileSpeedMultiplier) * PlayerStats.GetStat(EStat.ProjectileSpeedMultiplier);
		}

		public static float GetDuration(WeaponBase weaponBase)
		{
			float duration = weaponBase.GetValue(EStat.DurationMultiplier) * PlayerStats.GetStat(EStat.DurationMultiplier);
			return weaponBase.weaponData.maxDuration > 0f ? Mathf.Min(duration, weaponBase.weaponData.maxDuration) : duration;
		}

		public static float GetBurstInterval(WeaponBase weaponBase)
		{
			if (weaponBase.weaponData.burstTime <= 0f) return 0f;
			int quantity = GetAttackQuantity(weaponBase);
			float speed = PlayerStats.GetStat(EStat.AttackSpeed);
			float minimum = Mathf.Max(weaponBase.weaponData.minBurstInterval / speed, Time.fixedDeltaTime);
			return Mathf.Max(Time.fixedDeltaTime, Mathf.Max(weaponBase.weaponData.burstTime / quantity / speed, minimum));
		}

		public static float GetWeaponCooldown(WeaponBase weaponBase)
		{
			float speed = weaponBase.GetValue(EStat.AttackSpeed) * PlayerStats.GetStat(EStat.AttackSpeed);
			float burst = GetAttackQuantity(weaponBase) * GetBurstInterval(weaponBase);
			return Mathf.Max(Time.fixedDeltaTime, weaponBase.weaponData.endCooldown / speed + burst);
		}

		public static float GetCritChance(WeaponBase weaponBase)
		{
			return PlayerStats.GetStat(EStat.CritChance) + (weaponBase != null ? weaponBase.GetValue(EStat.CritChance) : 0f);
		}

		public static float GetCritDamageMultiplier(WeaponBase weaponBase)
		{
			return PlayerStats.GetStat(EStat.CritDamage) + (weaponBase != null ? weaponBase.GetValue(EStat.CritDamage) : 0f);
		}

		public static float GetKnockback(WeaponBase weaponBase)
		{
			return weaponBase.GetValue(EStat.KnockbackMultiplier) * PlayerStats.GetStat(EStat.KnockbackMultiplier);
		}

		public static float GetDamageProjectile(ProjectileBase projectile)
		{
			float damage = projectile.weaponBase.GetValue(EStat.DamageMultiplier) * PlayerStats.GetStat(EStat.DamageMultiplier);
			return projectile.bounces > 0 ? Mathf.Max(damage, 1f) : damage;
		}

		public static float GetWeaponRange(WeaponBase weaponBase)
		{
			return weaponBase.weaponData.spawnProjectileRange * (FinalFightController.isFightingFinalBoss ? 1.33f : 1f);
		}

		public static int GetMaxProjectilesPoolSize(EWeapon weapon)
		{
			return 250;
		}

		public static int GetMaxProjectileHitsPoolSize(EWeapon weapon)
		{
			return 10;
		}

		public static int GetMaxProjectileDonePoolSize(EWeapon weapon)
		{
			return 10;
		}

		public static int GetMaxAttacksPoolSize(EWeapon weapon)
		{
			return 100;
		}

		public static void WeaponAttack(WeaponBase weapon, float damageMultiplier = 1f)
		{
			if (weapon.weaponData.isAura) return;
			WeaponAttack attack = PoolManager.Instance.GetAttack(weapon);
			if (attack == null) return;
			attack.gameObject.SetActive(true);
			var player = GameManager.Instance.GetPlayer();
			attack.transform.position = player.transform.position + weapon.weaponData.spawnOffset;
			attack.SetAttack(weapon, player, damageMultiplier);
		}

		public static void LightningStrike(Enemy enemy, int bounces, DamageContainer dc, float bounceRange, float bounceProcCoefficient, Color? tint = null, AudioClip sound = null)
		{
			GameObject effect = PoolManager.Instance.lightningStrikePool.Get();
			if (effect != null)
			{
				if (tint.HasValue || sound != null) OpenBonkLightningAppearance.Apply(effect, tint, sound);
				Vector3 offset = UnityEngine.Random.insideUnitSphere;
				offset.y = 0f;
				effect.transform.position = enemy.GetCenterPosition() + offset * 0.25f;
			}
			enemy.DamageFromPlayerWeapon(dc);
			if (bounces > 0) ChainLightning(enemy, bounces, bounceRange, dc, bounceProcCoefficient, tint);
		}

		private static void ChainLightning(Enemy initialEnemy, int numBounces, float bounceRange, DamageContainer sourceDc, float bounceProcCoefficient, Color? tint)
		{
			var visited = new HashSet<GameObject> { initialEnemy.gameObject };
			Vector3 position = initialEnemy.GetCenterPosition();
			var positions = new List<Vector3> { position };
			float damage = sourceDc.damage * 0.5f;
			for (int bounce = 0; bounce < numBounces; bounce++)
			{
				int count = EnemyTargeting.GetEnemiesInRadiusSafe(null, position, bounceRange, out Collider[] candidates);
				var indexes = new List<int>();
				for (int i = 0; i < count; i++) if (candidates[i] != null && !visited.Contains(candidates[i].gameObject)) indexes.Add(i);
				if (indexes.Count == 0) break;
				int selected = indexes[UnityEngine.Random.Range(0, indexes.Count)];
				if (!Assets.Scripts.Managers.EnemyManager.Instance.GetEnemy(candidates[selected], out Enemy target)) break;
				position = target.GetCenterPosition();
				positions.Add(position);
				var bounceDc = new DamageContainer(bounceProcCoefficient, sourceDc.damageSource) { damage = damage, enemy = target, element = EElement.Lightning };
				target.DamageFromPlayerOther(bounceDc);
				visited.Add(target.gameObject);
			}
			if (positions.Count > 1)
			{
				GameObject effect = PoolManager.Instance.chainLightningPool.Get();
				if (effect != null) { if (tint.HasValue) OpenBonkLightningAppearance.Apply(effect, tint, null); effect.GetComponent<ChainLightning>().Set(positions); }
			}
		}
	}
}
