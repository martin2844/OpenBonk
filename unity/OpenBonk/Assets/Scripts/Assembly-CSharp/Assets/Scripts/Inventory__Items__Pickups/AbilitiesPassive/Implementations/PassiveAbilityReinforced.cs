using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityReinforced : PassiveAbility
	{
		private float armorPerLevel = 0.01f;

		public override void Init()
		{
			PlayerXp.A_LevelUp += OnLevelup;
		}

		public override void Tick()
		{
		}

		private void OnLevelup(int level)
		{
			SetStat(new StatModifier { stat = EStat.Armor, modifyType = EStatModifyType.Flat, modification = level * armorPerLevel });
		}

		public override void Cleanup()
		{
			PlayerXp.A_LevelUp -= OnLevelup;
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Reinforced;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.Armor) ?? "",
				["value1"] = string.Format("{0}%", armorPerLevel * 100f)
			});
		}
	}
}
