using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityQuantumXp : PassiveAbility
	{
		private float xpPerLevel = 0.01f;

		public override void Init()
		{
			PlayerXp.A_LevelUp += OnLevelup;
		}

		public override void Cleanup()
		{
			PlayerXp.A_LevelUp -= OnLevelup;
		}

		private void OnLevelup(int level)
		{
			SetStat(new StatModifier { stat = EStat.XpIncreaseMultiplier, modifyType = EStatModifyType.Flat, modification = level * xpPerLevel });
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.XpIncreaseMultiplier) ?? "",
				["value1"] = string.Format("{0}%", xpPerLevel * 100f)
			});
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Quantum;
		}

		public override void Tick()
		{
		}
	}
}
