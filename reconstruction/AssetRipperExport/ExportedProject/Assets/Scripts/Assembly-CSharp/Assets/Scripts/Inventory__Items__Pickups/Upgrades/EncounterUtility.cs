using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.UI.InGame.Rewards.Effects;

namespace Assets.Scripts.Inventory__Items__Pickups.Upgrades
{
	public class EncounterUtility
	{
		public static List<EStat> upgradableStatsShrines = Stats(0,1,2,3,4,5,9,10,11,12,15,16,17,18,19,23,24,25,26,29,30,31,32,39,40,41,38,46);

		public static List<EStat> upgradableStatsChaosAndGamble = Stats(0,1,2,3,4,5,9,10,11,12,15,16,17,18,19,23,24,25,29,30,31,32,39,40,41,38,46);

		private static List<EStat> upgradableStatsBalanceShrine = Stats(0,1,2,3,4,5,9,10,11,12,15,17,18,19,23,24,25,26,27,29,30,31,32,40,41);
		private static readonly float[] randomStatValues = { 15f,20f,5f,5f,.05f,.05f,0f,0f,0f,.08f,.08f,.1f,.12f,0f,0f,.06f,1f,.06f,.05f,.1f,0f,0f,0f,.1f,.1f,.08f,.1f,.1f,0f,.2f,.05f,.075f,.075f,0f,0f,0f,0f,0f,.08f,.15f,.1f,.05f,0f,0f,0f,1f,1f,0f,0f,.075f };

		private static List<EStat> Stats(params int[] values)
		{
			var result = new List<EStat>();
			foreach (int value in values) result.Add((EStat)value);
			return result;
		}

		private static List<EStat> PickStats(List<EStat> source, int amount)
		{
			var available = new List<EStat>(source);
			var result = new List<EStat>();
			for (int i = 0; i < amount; i++)
			{
				int index = global::Utility.MyRandom.random.Next(0, available.Count);
				result.Add(available[index]);
				available.RemoveAt(index);
			}
			return result;
		}

		public static List<EStat> GetRandomStats(int amount)
		{
			return PickStats(upgradableStatsShrines, amount);
		}

		public static List<EStat> GetRandomStatsChaosAndGamble(int amount)
		{
			return PickStats(upgradableStatsChaosAndGamble, amount);
		}

		public static List<EStat> GetRandomStatsBalanceShrine(int amount)
		{
			return PickStats(upgradableStatsBalanceShrine, amount);
		}

		public static List<EncounterOffer> GetRandomStatOffers(int amount, bool forceLegendary = false, bool useShrineStats = true)
		{
			var result = new List<EncounterOffer>();
			var stats = useShrineStats ? GetRandomStats(amount) : GetRandomStatsChaosAndGamble(amount);
			foreach (EStat stat in stats)
			{
				ERarity rarity = Rarity.GetEncounterOfferRarity(PlayerStats.GetStat(EStat.Luck));
				if (forceLegendary) rarity = ERarity.Legendary;
				float value = GetRandomStatValue(stat, out EStatModifyType type);
				var modifier = new StatModifier { stat = stat, modifyType = type, modification = StatUtility.GetRarityValue(value, rarity, 3) };
				result.Add(new EncounterOffer { rarity = rarity, effects = new[] { new EffectStat { effectType = EEncounterEffect.StatChange, statModifier = modifier } } });
			}
			return result;
		}

		public static List<EncounterOffer> GetBalanceShrineOffers(int amount)
		{
			var stats = GetRandomStatsBalanceShrine(amount * 2);
			var result = new List<EncounterOffer>();
			for (int i = 0; i < amount; i++)
			{
				result.Add(new EncounterOffer { rarity = ERarity.Rare, effects = new[] {
					new EffectStat { effectType = EEncounterEffect.StatChangeBalanceShrine, statModifier = new StatModifier { stat = stats[i * 2], modifyType = EStatModifyType.Multiplication, modification = 2f } },
					new EffectStat { effectType = EEncounterEffect.StatChangeBalanceShrine, statModifier = new StatModifier { stat = stats[i * 2 + 1], modifyType = EStatModifyType.Multiplication, modification = .5f } }
				} });
			}
			return result;
		}

		private static float GetRandomStatValue(EStat stat, out EStatModifyType type)
		{
			int index = (int)stat;
			type = EStatModifyType.Flat;
			if ((uint)index >= 50) return 0f;
			switch (index)
			{
				case 9: case 10: case 11: case 12: case 15: case 19: case 23: case 24: case 25: case 26: case 29: case 31: case 32: case 39: case 40: case 41: case 49:
					type = EStatModifyType.Addition; break;
			}
			return randomStatValues[index];
		}
	}
}
