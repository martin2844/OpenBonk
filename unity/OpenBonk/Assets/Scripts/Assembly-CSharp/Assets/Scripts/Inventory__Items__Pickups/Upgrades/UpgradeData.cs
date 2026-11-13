using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Upgrades
{
	[CreateAssetMenu(menuName = "Me/UpgradeData", order = 1)]
	public class UpgradeData : ScriptableObject
	{
		public List<StatModifier> upgradeModifiers;

		public List<StatModifier> GetUpgradeOffer(ERarity rarity, EWeapon eWeapon)
		{
			var result = new List<StatModifier>();
			float multiplier = Rarity.GetMultiplier(rarity);
			var remaining = new List<StatModifier>(upgradeModifiers);
			int count = rarity == ERarity.Common && Random.Range(0, 2) != 1 ? 1 : (remaining.Count < 2 ? 1 : 2);
			int extra = 0;
			int anvils = MyPlayer.Instance.inventory.itemInventory.GetAmount(EItem.Anvil);
			while (extra < anvils && count + extra < remaining.Count) extra++;
			for (int i = 0; i < count + extra; i++)
			{
				int index = MyRandom.random.Next(0, remaining.Count);
				result.Add(GetRandomModifier(remaining[index], multiplier));
				remaining.RemoveAt(index);
			}
			return result;
		}

		private StatModifier GetRandomModifier(StatModifier randomModifier, float multiplier)
		{
			return new StatModifier
			{
				stat = randomModifier.stat,
				modifyType = randomModifier.modifyType,
				modification = (float)System.Math.Round(randomModifier.modification * multiplier, 2)
			};
		}
	}
}
