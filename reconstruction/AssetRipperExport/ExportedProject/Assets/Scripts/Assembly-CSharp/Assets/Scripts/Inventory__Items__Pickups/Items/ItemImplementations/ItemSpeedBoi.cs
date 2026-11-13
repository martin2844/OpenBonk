using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemSpeedBoi : ItemBase
	{
		private float damageMultiplierDuringFreeze = 2f;

		private float duration;

		private float durationPerAmount = 2f;

		private float normalCooldown = 10f;

		private float slowdownReadyAtTime;

		public static Action A_Slowdown;

		private float slowdownHpRatio = .5f;

		public ItemSpeedBoi(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Init()
		{
			PlayerHealth.A_TakeDamage += OnTakeDamage;
			MyTime.A_TimeScaleChange += RefreshTimeScale;
			GameManager.A_StageStarted += ResetStats;
		}

		public override void Cleanup()
		{
			PlayerHealth.A_TakeDamage -= OnTakeDamage;
			MyTime.A_TimeScaleChange -= RefreshTimeScale;
			GameManager.A_StageStarted -= ResetStats;
		}

		private void RefreshTimeScale()
		{
			if (Mathf.Abs(MyTime.timeScale - 1f) < .01f) ResetStats();
		}

		protected override void OnInitOrAmountChanged()
		{
			duration = Mathf.Clamp(8f + amount * durationPerAmount, 1f, 15f);
		}

		private void OnTakeDamage(PlayerHealth ph, DamageContainer dc, bool shieldDamage)
		{
			if ((float)ph.GetCombinedHp() / ph.GetCombinedMaxHp() < slowdownHpRatio) Slowdown();
		}

		private void Slowdown()
		{
			if (MyTime.time < slowdownReadyAtTime) return;
			slowdownReadyAtTime = MyTime.time + duration + normalCooldown;
			MyTime.SetTimeScale(.3f, duration);
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Multiplication, modification = damageMultiplierDuringFreeze });
			A_Slowdown?.Invoke();
		}

		private void ResetStats()
		{
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Multiplication, modification = 1f });
		}

		public override void Tick()
		{
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
				["stat1"] = EnumUtility.EnumToReadable(EStat.MaxHealth) ?? "",
				["value1"] = string.Format("{0}%", slowdownHpRatio * 100f),
				["value2"] = string.Format("{0}x", damageMultiplierDuringFreeze)
			};
		}
	}
}
