using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityRngBlessing : PassiveAbility
	{
		private float luckPerLevel = 0.015f;

		public override void Init()
		{
			PlayerXp.A_LevelUp += OnLevelup;
		}

		public override void Tick()
		{
		}

		private void OnLevelup(int level)
		{
			SetStat(new StatModifier { stat = EStat.Luck, modifyType = EStatModifyType.Flat, modification = level * luckPerLevel });
		}

		public override void Cleanup()
		{
			PlayerXp.A_LevelUp -= OnLevelup;
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.RngBlessing;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.Luck) ?? "",
				["value1"] = string.Format("{0}%", luckPerLevel * 100f)
			});
		}
	}
}
