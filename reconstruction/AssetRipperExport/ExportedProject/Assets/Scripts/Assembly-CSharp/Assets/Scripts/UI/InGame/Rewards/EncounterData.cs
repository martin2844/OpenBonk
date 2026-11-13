using UnityEngine;
using UnityEngine.Localization;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Actors.Player;

namespace Assets.Scripts.UI.InGame.Rewards
{
	[CreateAssetMenu(menuName = "Me/EncounterData")]
	public class EncounterData : ScriptableObject
	{
		public EEncounter encounterType;

		public EncounterOffer[] offers;

		public LocalizedString localizedName;

		public LocalizedString localizedDescription;

		public string GetName()
		{
			return localizedName.GetLocalizedString();
		}

		public string GetDescription()
		{
			return localizedDescription.GetLocalizedString();
		}

		public EncounterOffer[] GetOffers()
		{
			if (encounterType == EEncounter.RandomStats)
			{
				var result = EncounterUtility.GetRandomStatOffers(3, ChargeShrine.lastRewardShrine.isGolden).ToArray();
				if (MyPlayer.Instance.inventory.itemInventory.GetItem(EItem.Wrench) is ItemWrench wrench)
					foreach (var offer in result)
						foreach (var effect in offer.effects) effect.statModifier.modification *= wrench.GetChargeRewardMultiplier();
				return result;
			}
			if (encounterType == EEncounter.BalanceShrine) return EncounterUtility.GetBalanceShrineOffers(2).ToArray();
			return offers;
		}

		public bool HasRarity()
		{
			return encounterType == EEncounter.RandomStats;
		}
	}
}
