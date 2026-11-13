using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityWallClimb : PassiveAbility
	{
		private float hpPerLevel = 2f;

		public override void Init()
		{
			PlayerXp.A_LevelUp += OnLevelup;
		}

		private void OnLevelup(int level)
		{
			SetStat(new StatModifier { stat = EStat.MaxHealth, modifyType = EStatModifyType.Flat, modification = level * hpPerLevel });
		}

		public override void Cleanup()
		{
			PlayerXp.A_LevelUp -= OnLevelup;
		}

		public override void Tick()
		{
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.WallClimb;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.MaxHealth) ?? "",
				["value1"] = string.Format("+{0}", hpPerLevel)
			});
		}
	}
}
