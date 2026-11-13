using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items
{
	public static class ItemUtility
	{
		public static ItemData GetRandomChestItem(EChest chestType, float luck)
		{
			string statName = EMyStat.chestsOpened.ToString();
			if (SaveManager.Instance.stats.stats.TryGetValue(statName, out MyStat stat) && (int)stat.value <= 6)
			{
				EItemRarity[] firstChestRarities = { EItemRarity.Rare, EItemRarity.Common, EItemRarity.Epic, EItemRarity.Common, EItemRarity.Common, EItemRarity.Legendary };
				int index = (int)stat.value - 1;
				return GetRandomItemFromRarity(index >= 0 && index < firstChestRarities.Length ? firstChestRarities[index] : EItemRarity.Common);
			}
			switch (chestType)
			{
				case EChest.Corrupt: return GetRandomItemFromRarity(EItemRarity.Corrupted);
				case EChest.Normal:
				case EChest.Free:
				case EChest.FreeCrypt:
				case EChest.Ghost: return GetRandomItem(luck);
				default: throw new System.Exception("Unknown chest type: " + chestType);
			}
		}

		public static ItemData GetRandomItem(float luck)
		{
			return GetRandomItemFromRarity(Rarity.GetItemRarity(luck));
		}

		public static ItemData GetRandomItemFromRarity(EItemRarity rarity)
		{
			if (rarity == EItemRarity.Legendary && Random.Range(0f, 1f) < 0.0025f && MyStats.GetStat(EMyStat.runs.ToString()) >= 6f)
				return DataManager.Instance.GetItem(EItem.GoldenRing);
			var items = RunUnlockables.availableItems[rarity];
			return items[global::Utility.MyRandom.random.Next(0, items.Count)];
		}

		public static bool TryProc(float procCoefficient, float baseProcChance)
		{
			return global::Utility.MyRandom.random.NextDouble() < procCoefficient * baseProcChance;
		}
	}
}
