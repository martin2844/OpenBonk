using System;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;

namespace Assets.Scripts.Inventory__Items__Pickups
{
	public static class RunUnlockables
	{
		public static HashSet<ItemData> banishedItems;

		public static HashSet<UnlockableBase> banishedUpgradables;

		public static Dictionary<EItemRarity, List<ItemData>> availableItems;

		private static Dictionary<EItem, int> numItemsPickedupThisRun;

		public static Action A_Inited;

		private static int maxOverpoweredLamps;

		private static int maxAnvils;

		public static void Init()
		{
			GameManager.A_RunStarted += OnNewRunStarted;
			MyAchievements.A_Unlocked += OnAchievementUnlocked;
			ItemInventory.A_ItemAdded += OnItemAdded;
			ItemInventory.A_ItemRemoved += OnItemRemoved;
		}

		public static void Cleanup()
		{
			GameManager.A_RunStarted -= OnNewRunStarted;
			MyAchievements.A_Unlocked -= OnAchievementUnlocked;
			ItemInventory.A_ItemAdded -= OnItemAdded;
			ItemInventory.A_ItemRemoved -= OnItemRemoved;
		}

		private static void OnNewRunStarted()
		{
			banishedItems = new HashSet<ItemData>();
			banishedUpgradables = new HashSet<UnlockableBase>();
			availableItems = new Dictionary<EItemRarity, List<ItemData>>();
			numItemsPickedupThisRun = new Dictionary<EItem, int>();
			foreach (EItemRarity rarity in Enum.GetValues(typeof(EItemRarity))) availableItems.Add(rarity, new List<ItemData>());
			foreach (ItemData item in DataManager.Instance.unsortedItems)
				if (MyAchievements.IsAvailable(item) && item.inItemPool) AddItemToPool(item);
			A_Inited?.Invoke();
		}

		public static void Testing()
		{
		}

		private static void AddItemToPool(ItemData item)
		{
			availableItems[item.rarity].Add(item);
		}

		private static void OnItemAdded(EItem eItem)
		{
			numItemsPickedupThisRun.TryGetValue(eItem, out int picked);
			numItemsPickedupThisRun[eItem] = ++picked;
			ItemData item = DataManager.Instance.GetItem(eItem);
			int amount = MyPlayer.Instance.inventory.itemInventory.GetAmount(eItem);
			if ((item.maxAmount > 0 && amount >= item.maxAmount) || (item.maxAmountPerRun > 0 && picked >= item.maxAmountPerRun))
				availableItems[item.rarity].Remove(item);
		}

		private static void OnItemRemoved(EItem eItem, bool whatever)
		{
			ItemData item = DataManager.Instance.GetItem(eItem);
			if (item.maxAmount > 0 && MyPlayer.Instance.inventory.itemInventory.GetAmount(eItem) == item.maxAmount - 1 && !availableItems[item.rarity].Contains(item))
				AddItemToPool(item);
		}

		private static void OnAchievementUnlocked(MyAchievement ach)
		{
		}

		public static void BanishItem(ItemData unlockable)
		{
			banishedItems.Add(unlockable);
			availableItems[unlockable.rarity].Remove(unlockable);
		}

		public static void BanishUpgradable(UnlockableBase upgradable)
		{
			banishedUpgradables.Add(upgradable);
		}
	}
}
