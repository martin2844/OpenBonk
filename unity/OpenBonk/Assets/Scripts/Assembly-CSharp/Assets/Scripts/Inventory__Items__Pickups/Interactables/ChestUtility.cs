using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Interactables
{
	public class ChestUtility
	{
		public static EEncounter ChestTypeToEncounter(EChest chestType)
		{
			switch (chestType)
			{
				case EChest.Normal: return EEncounter.ChestNormal;
				case EChest.Corrupt: return EEncounter.ChestEvil;
				case EChest.Free: return EEncounter.ChestFree;
				case EChest.FreeCrypt: return EEncounter.ChestFreeCrypt;
				case EChest.Ghost: return EEncounter.ChestGhost;
				default: throw new System.Exception("Unknown chest type: " + chestType);
			}
		}

		public static EChest EncounterToChestType(EEncounter encounter)
		{
			switch (encounter)
			{
				case EEncounter.ChestNormal: return EChest.Normal;
				case EEncounter.ChestEvil: return EChest.Corrupt;
				case EEncounter.ChestFree: return EChest.Free;
				case EEncounter.ChestFreeCrypt: return EChest.FreeCrypt;
				case EEncounter.ChestGhost: return EChest.Ghost;
				default: throw new System.Exception("Unknown chest encounter: " + encounter);
			}
		}

		public static void OpenChestNoAnimation(EChest chestType)
		{
			ItemData item;
			try { item = ItemUtility.GetRandomChestItem(chestType, PlayerStats.GetStat(EStat.Luck)); }
			catch (System.Exception e)
			{
				Debug.LogError(e.Message);
				item = DataManager.Instance.unsortedItems[0];
			}
			MyPlayer.Instance.inventory.itemInventory.AddItem(item.eItem);
		}
	}
}
