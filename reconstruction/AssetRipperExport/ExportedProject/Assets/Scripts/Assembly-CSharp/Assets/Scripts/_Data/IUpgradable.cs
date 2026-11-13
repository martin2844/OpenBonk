using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;

namespace Assets.Scripts._Data
{
	public interface IUpgradable
	{
		string GetName();

		string GetUpgradeDescription(int level, List<StatModifier> upgradeOffer, ERarity rarity);

		Texture GetIcon();

		int GetLevel();

		int GetMaxLevel();

		List<StatModifier> GetUpgradeOffer(ERarity rarity);
	}
}
