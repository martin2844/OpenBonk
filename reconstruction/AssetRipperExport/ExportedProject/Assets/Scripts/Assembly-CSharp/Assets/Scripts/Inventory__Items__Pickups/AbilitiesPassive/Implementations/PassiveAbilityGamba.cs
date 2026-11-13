using UnityEngine.Localization;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityGamba : PassiveAbility
	{
		private float upgradeMultiplier = .75f;

		private float minMultiplier = .06f;

		private float maxMultiplier = 1f;

		private int currentLevel;

		public override void Init()
		{
			PlayerXp.A_LevelUp += OnLevelup;
		}

		public override void Tick()
		{
		}

		private void OnLevelup(int level)
		{
			while (currentLevel < level)
			{
				float multiplier = Mathf.Clamp(upgradeMultiplier / (1f + Mathf.Pow(currentLevel / 50f, 1.5f)), minMultiplier, maxMultiplier);
				var modifier = EncounterUtility.GetRandomStatOffers(1, false, false)[0].effects[0].statModifier;
				modifier.modification = StatUtility.GetRarityValue(modifier.modification, ERarity.Common, 3) * multiplier;
				MyPlayer.Instance.inventory.statInventory.ChangeStat(modifier, true, 0f, false);
				if (UiManager.Instance.scoreUi.GetQueueCount() <= 9)
					UiManager.Instance.scoreUi.AddScore(modifier, true, true, 1f);
				currentLevel++;
			}
		}

		public override void Cleanup()
		{
			PlayerXp.A_LevelUp -= OnLevelup;
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Gamba;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString();
		}
	}
}
