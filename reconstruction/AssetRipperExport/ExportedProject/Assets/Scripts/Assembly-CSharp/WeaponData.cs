using System.Collections.Generic;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts._Data;
using UnityEngine;

[CreateAssetMenu(menuName = "Me/Weapon", order = 1)]
public class WeaponData : UnlockableBase, IUpgradable
{
	public EWeapon eWeapon;

	public Texture icon;

	public bool onlySpawnWhenCloseEnemies;

	public Dictionary<EStat, float> baseStats;

	public float damage;

	public float knockback;

	public float critChance;

	public EElement element;

	public int projectiles;

	public int projectileBounces;

	public float attackDuration;

	public float maxDuration;

	public float maxSizeMultiplier;

	public float effectDuration;

	public float projectileSpeed;

	[Header("New Quantity and cooldown")]
	public float endCooldown;

	public float burstTime;

	public float minBurstInterval;

	[Header("Behaviour")]
	public bool canBounce;

	public EAmplificationMode amplificationMode;

	public float procCoefficient;

	public bool useVision;

	public bool canMultiHit;

	public bool hasCrosshair;

	public float spawnProjectileRange;

	public bool isAura;

	public Vector3 spawnOffset;

	public GameObject attack;

	public UpgradeData upgradeData;

	[Header("Unlocks")]
	public MyAchievement AchievementRequirement;

	public string damageSourceName { get; private set; }

	public void Init()
	{
		damageSourceName = eWeapon.ToString();
		baseStats = new Dictionary<EStat, float>
		{
			{ EStat.AttackSpeed, 1f },
			{ EStat.DamageMultiplier, damage },
			{ EStat.KnockbackMultiplier, knockback },
			{ EStat.Projectiles, projectiles },
			{ EStat.ProjectileBounces, projectileBounces },
			{ EStat.DurationMultiplier, attackDuration },
			{ EStat.ProjectileSpeedMultiplier, projectileSpeed },
			{ EStat.SizeMultiplier, 1f },
			{ EStat.CritChance, critChance },
			{ EStat.CritDamage, 0f }
		};
	}

	public string GetUpgradeDescription(int level, List<StatModifier> upgradeOffer, ERarity rarity)
	{
		return level == 0 ? GetDescription() : StatUtility.GetUpgradeDescriptionWeapon(upgradeOffer, this);
	}

	public override Texture GetIcon()
	{
		return icon;
	}

	public override MyAchievement GetUnlockRequirement()
	{
		return AchievementRequirement;
	}

	public override UnlockableBase GetUnlockableRequirement()
	{
		return null;
	}

	public override string GetUnlockableTypeDisplayString()
	{
		return Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Unlockables", "WEAPON", "Weapon");
	}

	public override string GetInternalName()
	{
		return eWeapon.ToString();
	}

	public int GetLevel()
	{
		return GameManager.Instance.GetPlayerInventory().weaponInventory.GetWeaponLevel(eWeapon);
	}

	public int GetMaxLevel()
	{
		return InventoryUtility.GetWeaponMaxLevel();
	}

	public List<StatModifier> GetUpgradeOffer(ERarity rarity)
	{
		return upgradeData.GetUpgradeOffer(rarity, eWeapon);
	}

	public float GetBaseStat(EStat eStat)
	{
		return baseStats[eStat];
	}

	public float CalculateTotalDistance(float initialSpeed, float reduction)
	{
		float total = 0f;
		while (initialSpeed > 0f)
		{
			total += initialSpeed;
			initialSpeed = Mathf.Clamp(initialSpeed - reduction, 0f, 99f);
		}
		return total;
	}

	public float GetSpawnProjectileRange()
	{
		return spawnProjectileRange;
	}

	public override string ToString()
	{
		return null;
	}
}
