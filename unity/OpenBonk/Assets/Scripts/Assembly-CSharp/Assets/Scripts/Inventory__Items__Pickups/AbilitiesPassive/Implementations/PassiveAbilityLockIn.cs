using UnityEngine.Localization;
using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityLockIn : PassiveAbility
	{
		public int thornsPerLevel = 2;

		private float maxDamage = 1f;

		private float maxDamagePerLevel = .01f;

		private float updateCooldown = 1f;

		private float nextUpdateTime;

		private float lastValue = -1f;

		public override void Init()
		{
			maxDamage = 1f;
			PlayerXp.A_LevelUp += OnLevelup;
			AegisAttack.A_Used += OnAegisChange;
			AegisAttack.A_Regen += OnAegisChange;
		}

		public override void Cleanup()
		{
			PlayerXp.A_LevelUp -= OnLevelup;
			AegisAttack.A_Used -= OnAegisChange;
			AegisAttack.A_Regen -= OnAegisChange;
		}

		private void OnAegisChange(int currentAmount)
		{
			SetStat(new StatModifier { stat = EStat.AttackSpeed, modifyType = EStatModifyType.Addition, modification = currentAmount > 0 ? 0f : 1.5f });
		}

		public override void Tick()
		{
			if (MyTime.time < nextUpdateTime) return;
			nextUpdateTime = MyTime.time + updateCooldown;
			var health = MyPlayer.Instance.inventory.playerHealth;
			float damage = Mathf.Clamp01(1f - (float)health.GetCombinedHp() / health.GetCombinedMaxHp()) * maxDamage;
			if (Mathf.Abs(lastValue - damage) < .02f) return;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = damage });
			lastValue = damage;
		}

		private void OnLevelup(int level)
		{
			maxDamage = 1f + level * maxDamagePerLevel;
			SetStat(new StatModifier { stat = EStat.Thorns, modifyType = EStatModifyType.Flat, modification = level * thornsPerLevel });
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.LockIn;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.Thorns) ?? "",
				["value1"] = string.Format("+{0}", thornsPerLevel)
			});
		}
	}
}
