using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityStonks : PassiveAbility
	{
		private float goldIncreasePerLevel = .01f;

		private float damageIncreasePer1000Gold = .1f;

		private float damagePer1000Gold_First10k = .2f;

		private float damagePer1000Gold_First200k = .085f;

		private float damagePer1000Gold_First1m = .06f;

		private float damagePer1000Gold_After1m = .035f;

		private float nextUpdateTime;

		private float updateCooldown = 1f;

		private float lastValue = -1f;

		public override void Init()
		{
			PlayerXp.A_LevelUp += OnLevelup;
		}

		private float GetDamage()
		{
			float gold = MyPlayer.Instance.inventory.gold;
			float value = 0f;
			if (gold > 1000000f) { value += (gold - 1000000f) / 1000f * damagePer1000Gold_After1m; gold = 1000000f; }
			if (gold > 200000f) { value += (gold - 200000f) / 1000f * damagePer1000Gold_First1m; gold = 200000f; }
			if (gold > 10000f) { value += (gold - 10000f) / 1000f * damagePer1000Gold_First200k; gold = 10000f; }
			return value + gold / 1000f * damagePer1000Gold_First10k;
		}

		public override void Tick()
		{
			if (nextUpdateTime > MyTime.time) return;
			nextUpdateTime = MyTime.time + updateCooldown;
			float value = GetDamage();
			if (Mathf.Abs(lastValue - value) < .02f) return;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = value });
			lastValue = value;
		}

		private void OnLevelup(int level)
		{
			SetStat(new StatModifier { stat = EStat.GoldIncreaseMultiplier, modifyType = EStatModifyType.Flat, modification = level * goldIncreasePerLevel });
		}

		public override void Cleanup()
		{
			PlayerXp.A_LevelUp -= OnLevelup;
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Stonks;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.GoldIncreaseMultiplier) ?? "",
				["value1"] = string.Format("{0}%", goldIncreasePerLevel * 100f),
				["damage"] = LocalizationUtility.GetStatName(EStat.DamageMultiplier),
				["gold"] = LocalizationUtility.GetLocalizedString("Other", "GOLD")
			});
		}
	}
}
