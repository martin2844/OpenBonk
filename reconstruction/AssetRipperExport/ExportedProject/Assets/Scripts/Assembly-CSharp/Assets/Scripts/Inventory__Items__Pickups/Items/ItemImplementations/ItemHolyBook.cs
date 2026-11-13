using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemHolyBook : ItemBase
	{
		public static Action<float> A_OnUse;

		private float maxHpPerAmount = 100f;

		private float hpRegenPerAmount = 50f;

		private float overhealPerAmount = 0.25f;

		private float radius;

		private float radiusPerAmount = 1f;

		private float healsThisTick;

		private float nextDamageTime;

		private float cooldown = 1.5f;

		private string damageSource;

		private DamageContainer dc;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.MaxHealth, modifyType = EStatModifyType.Flat, modification = maxHpPerAmount * amount });
			SetStat(new StatModifier { stat = EStat.HealthRegen, modifyType = EStatModifyType.Flat, modification = hpRegenPerAmount * amount });
			SetStat(new StatModifier { stat = EStat.Overheal, modifyType = EStatModifyType.Flat, modification = overhealPerAmount * amount });
			radius = 5f + radiusPerAmount * amount;
		}

		public ItemHolyBook(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			damageSource = EItem.HolyBook.ToString();
			dc = new DamageContainer(.5f, damageSource);
		}

		public override void Init()
		{
			PlayerHealth.A_Heal += OnHeal;
		}

		public override void Cleanup()
		{
			PlayerHealth.A_Heal -= OnHeal;
		}

		private void OnHeal(PlayerHealth ph, float hpHealed, bool isShield)
		{
			if (!isShield) healsThisTick += hpHealed;
		}

		public override void Tick()
		{
			if (healsThisTick <= 0f || nextDamageTime > MyTime.time) return;
			if (MyPlayer.Instance == null || EnemyManager.Instance == null) return;
			nextDamageTime = MyTime.time + cooldown;
			float damage = MyPlayer.Instance.GetBaseDamage() * .5f * healsThisTick;
			float actualRadius = radius * PlayerStats.GetStat(EStat.SizeMultiplier);
			healsThisTick = 0f;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, MyPlayer.Instance.transform.position, actualRadius, out Collider[] targets);
			for (int i = 0; i < count; i++)
			{
				if (!EnemyManager.Instance.GetEnemy(targets[i], out var enemy)) continue;
				dc = WeaponUtility.GetDamageContainer(dc, damage, .5f, damageSource, Vector3.zero, enemy);
				enemy.DamageFromPlayerOther(dc);
				if (i < 10 && enemy.collider != null && EffectManager.Instance != null && PoolManager.Instance != null)
					EffectManager.Instance.EnemyHitEffect(enemy.collider.ClosestPoint(MyPlayer.Instance.transform.position), Vector3.zero, true, damageSource, null, true);
			}
			A_OnUse?.Invoke(actualRadius);
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.MaxHealth) ?? string.Empty,
				["stat2"] = EnumUtility.EnumToReadable(EStat.HealthRegen) ?? string.Empty,
				["value1"] = string.Format("{0}", maxHpPerAmount),
				["value2"] = string.Format("{0}", hpRegenPerAmount)
			};
		}
	}
}
