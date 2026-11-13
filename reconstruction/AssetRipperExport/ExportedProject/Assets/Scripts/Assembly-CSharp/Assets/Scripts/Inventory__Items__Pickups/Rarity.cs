using Assets.Scripts.Inventory__Items__Pickups.Items;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups
{
	public static class Rarity
	{
		public static ERarity GetUpgradeOfferRarity(float luck)
		{
			ERarity[] rarities = { ERarity.Common, ERarity.Uncommon, ERarity.Rare, ERarity.Epic, ERarity.Legendary };
			float[] weights = { 1.5f, 0.3f, 0.15f, 0.04f, 0.0085f };
			CalculateRarityWeights(weights, luck);
			float roll = Random.Range(0f, 1f);
			float cumulative = 0f;
			for (int i = 0; i < weights.Length; i++)
			{
				cumulative += weights[i];
				if (cumulative >= roll) return rarities[i];
			}
			return ERarity.Common;
		}

		public static ERarity GetEncounterOfferRarity(float luck)
		{
			return GetUpgradeOfferRarity(luck);
		}

		public static EItemRarity GetItemRarity(float luck)
		{
			var rarities = new List<EItemRarity>();
			var weights = new List<float>();
			float[] baseWeights = { 70f, 15f, 6f, 1.5f };
			for (int i = 0; i < baseWeights.Length; i++)
			{
				EItemRarity rarity = (EItemRarity)i;
				if (RunUnlockables.availableItems[rarity].Count > 0) { rarities.Add(rarity); weights.Add(baseWeights[i]); }
			}
			if (weights.Count == 0) return EItemRarity.Common;
			float[] adjusted = weights.ToArray();
			CalculateRarityWeights(adjusted, luck);
			float roll = Random.Range(0f, 1f), cumulative = 0f;
			for (int i = 0; i < adjusted.Length; i++) { cumulative += adjusted[i]; if (cumulative >= roll) return rarities[i]; }
			return EItemRarity.Common;
		}

		public static EItemRarity GetShadyGuyRarity(float luck, float[] customWeights = null)
		{
			EItemRarity[] rarities = { EItemRarity.Common, EItemRarity.Rare, EItemRarity.Epic, EItemRarity.Legendary };
			float[] weights = customWeights ?? new[] { 1f, 0.5f, 0.25f, 0.125f };
			CalculateRarityWeights(weights, luck);
			float roll = Random.Range(0f, 1f);
			float cumulative = 0f;
			for (int i = 0; i < weights.Length; i++)
			{
				cumulative += weights[i];
				if (cumulative >= roll) return rarities[i];
			}
			return EItemRarity.Common;
		}

		public static void CalculateRarityWeights(float[] rarityWeights, float luck)
		{
			float scaledLuck = Mathf.Log(luck + 1f) * -1.5f;
			float total = 0f;
			for (int i = 0; i < rarityWeights.Length; i++)
			{
				rarityWeights[i] *= Mathf.Pow(1.5f, (rarityWeights.Length - i - 1) * scaledLuck);
				total += rarityWeights[i];
			}
			for (int i = 0; i < rarityWeights.Length; i++) rarityWeights[i] /= total;
		}

		public static float GetMultiplier(ERarity rarity)
		{
			switch (rarity)
			{
				case ERarity.Uncommon: return 1.2f;
				case ERarity.Rare: return 1.4f;
				case ERarity.Epic: return 1.6f;
				case ERarity.Legendary: return 2f;
				default: return 1f;
			}
		}
	}
}
