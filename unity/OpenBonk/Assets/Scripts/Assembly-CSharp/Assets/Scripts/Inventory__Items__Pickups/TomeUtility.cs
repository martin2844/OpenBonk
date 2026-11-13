using Assets.Scripts.Actors.Player;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups
{
	public class TomeUtility
	{
		private static float balanceTomeValue = 0.015f;

		private static float chaosTomeMultiplier = 1.4f;

		public static void CheckSpecialTomes(TomeData tomeData, ERarity rarity)
		{
			if (tomeData.eTome == ETome.Chaos || tomeData.eTome == ETome.Gambler)
			{
				bool chaos = tomeData.eTome == ETome.Chaos;
				var modifier = EncounterUtility.GetRandomStatOffers(1, false, !chaos)[0].effects[0].statModifier;
				modifier.modification = Stats.StatUtility.GetRarityValue(modifier.modification * (chaos ? chaosTomeMultiplier : 1.5f), rarity, 3);
				bool positive = chaos || global::Utility.MyRandom.random.NextDouble() > .25;
				if (!positive) modifier.modification = -modifier.modification;
				MyPlayer.Instance.inventory.statInventory.ChangeStat(modifier, true, 0f, false);
				UiManager.Instance.scoreUi.AddScore(modifier, positive, true, 1f);
			}
			else if (tomeData.eTome == ETome.Hoarder)
			{
				Vector3 position = SpawnPositions.GetRandomSpawnPositionOnMap();
				if (position == SpawnPositions.INVALID_POS) MyLogger.LogError("FAILED TO FIND RANDOM POSITION ON MAP...");
				else EffectManager.Instance.SpawnChest(EffectManager.Instance.openChestNormal, position);
			}
		}

		public static string GetUpgradeDescription(TomeData tomeData, ERarity rarity)
		{
			if (tomeData.eTome != Assets.Scripts._Data.Tomes.ETome.Balance) return "No description for: " + tomeData.eTome;
			float value = Stats.StatUtility.GetRarityValue(balanceTomeValue, rarity, 3) * 100f;
			return Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Tomes", "TOME_BALANCE_DESC", new System.Collections.Generic.Dictionary<string, string> { ["value"] = string.Format("+{0}%", value) });
		}
	}
}
