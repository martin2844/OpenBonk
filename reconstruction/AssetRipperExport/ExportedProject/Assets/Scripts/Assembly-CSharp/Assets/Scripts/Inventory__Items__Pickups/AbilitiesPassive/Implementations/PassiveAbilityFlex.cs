using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using UnityEngine.Localization;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityFlex : PassiveAbility
	{
		public static Action<bool> A_FlexReady;

		private static float cooldown = 12f;

		private static float minCooldown = 4f;

		private static float maxCooldown = 12f;

		private float cooldownReductionPerLevel = 0.2f;

		private float radius = 10f;

		private static float flexReadyAtTime;

		public float damagePerFlex = 0.025f;

		private int stacks;

		private bool canFlex;

		private DamageContainer reuseDc = new DamageContainer(1f, "Flex");

		private string damageSource = "Flex";

		public override void Init()
		{
			PlayerHealth.A_CheckStopDamage += OnCheckStopDamage;
			PlayerXp.A_LevelUp += OnLevelup;
			cooldown = maxCooldown;
			stacks = 0;
			flexReadyAtTime = MyTime.time + 4f;
		}

		public override void Cleanup()
		{
			PlayerHealth.A_CheckStopDamage -= OnCheckStopDamage;
			PlayerXp.A_LevelUp -= OnLevelup;
		}

		private void OnLevelup(int level)
		{
			cooldown = Mathf.Clamp(maxCooldown - level * cooldownReductionPerLevel, minCooldown, maxCooldown);
		}

		public override void Tick()
		{
			if (GameManager.Instance.isCrypt && (MyTime.cryptTimer <= 0f || MyTime.cryptTimer >= 600f)) return;
			if (!canFlex && HasFlex()) { canFlex = true; A_FlexReady?.Invoke(true); }
		}

		private void OnCheckStopDamage(DamageContainer dc, bool shieldDamage)
		{
			if (!HasFlex()) return;
			bool selfDamage = dc.damageSource == "Kevin" || dc.damageSource == PlayerHealth.healthRegenDamageSource;
			if (!selfDamage && (dc.flags & DcFlags.BypassAll) == DcFlags.BypassAll) return;
			dc.damage = (dc.flags & DcFlags.BossDamage) != 0 ? dc.damage * 0.25f : 0f;
			UseFlex();
		}

		private float GetDamage()
		{
			return MyPlayer.Instance.GetBaseDamage();
		}

		private float GetKnockback()
		{
			return 2f * PlayerStats.GetStat(EStat.KnockbackMultiplier);
		}

		private void UseFlex()
		{
			flexReadyAtTime = MyTime.time + cooldown;
			stacks++;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Flat, modification = stacks * damagePerFlex });
			Vector3 origin = MyPlayer.Instance.transform.position;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, origin, radius, out Collider[] enemies);
			for (int i = 0; i < count; i++)
			{
				if (!EnemyManager.Instance.GetEnemy(enemies[i], out var enemy)) continue;
				Vector3 direction = enemy.transform.position - origin;
				direction.y = 0f;
				direction.Normalize();
				reuseDc = WeaponUtility.GetDamageContainer(reuseDc, GetDamage(), 1f, damageSource, direction, enemy);
				reuseDc.knockback = GetKnockback();
				enemy.DamageFromPlayerOther(reuseDc);
				enemy.AddDebuff(EDebuff.Stun, reuseDc, 3f);
			}
			MyPlayer.Instance.playerSfxs?.Flex();
			canFlex = false;
			A_FlexReady?.Invoke(false);
		}

		private bool HasFlex()
		{
			return MyTime.time > flexReadyAtTime;
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Flex;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["value1"] = string.Format("{0}", cooldownReductionPerLevel),
				["statDamage"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier) ?? "",
				["valueDamage"] = string.Format("{0}%", damagePerFlex * 100f)
			});
		}
	}
}
