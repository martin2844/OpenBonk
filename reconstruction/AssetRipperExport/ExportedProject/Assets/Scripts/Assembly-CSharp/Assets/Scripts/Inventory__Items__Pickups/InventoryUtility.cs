using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts._Data;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Progression;
using Utility;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups
{
	public class InventoryUtility
	{
		public const int MAX_WEAPON_LEVEL_BASE = 40;

		public const int MAX_TOME_LEVEL_BASE = 99;

		public static List<IUpgradable> GetRandomUpgrades()
		{
			var all = new List<IUpgradable>();
			all.AddRange(UnlockUtility.GetAvailableWeapons());
			all.AddRange(UnlockUtility.GetAvailableTomes());
			var owned = new List<IUpgradable>();
			var available = new List<IUpgradable>();
			foreach (IUpgradable entry in all)
			{
				int level = entry.GetLevel();
				if (level >= entry.GetMaxLevel()) continue;
				if (level > 0) owned.Add(entry);
				else if (CanUnlockItem(entry)) available.Add(entry);
			}
			int count = System.Math.Min(GetNumUpgrades(), owned.Count + available.Count);
			var result = new List<IUpgradable>();
			for (int i = 0; i < count; i++)
			{
				bool chooseNew = available.Count > 0 && (owned.Count == 0 || MyRandom.random.NextDouble() >= 1f / count);
				List<IUpgradable> source = chooseNew ? available : owned;
				int index = MyRandom.random.Next(0, source.Count);
				result.Add(source[index]);
				source.RemoveAt(index);
			}
			return result;
		}

		public static List<ItemData> GetRandomItemsMoai(int moaiLuckMode)
		{
			var pools = new Dictionary<EItemRarity, List<ItemData>>();
			foreach (var pool in RunUnlockables.availableItems)
				pools.Add(pool.Key, new List<ItemData>(pool.Value));
			var result = new List<ItemData>();
			int count = moaiLuckMode == 0 ? 2 : 3;
			for (int i = 0; i < count; i++)
			{
				float luck = PlayerStats.GetStat(EStat.Luck);
				if (moaiLuckMode == 0) luck *= 0.5f;
				else if (moaiLuckMode == 2) luck *= 1.5f;
				EItemRarity rarity = Rarity.GetItemRarity(luck);
				if (moaiLuckMode == 3) rarity = EItemRarity.Legendary;
				var pool = pools[rarity];
				if (pool.Count == 0) continue;
				int index = MyRandom.random.Next(pool.Count);
				result.Add(pool[index]);
				pool.RemoveAt(index);
			}
			return result;
		}

		public static List<ItemData> GetRandomItemsShadyGuy(EItemRarity itemRarity)
		{
			var pools = new Dictionary<EItemRarity, List<ItemData>>();
			foreach (var pool in RunUnlockables.availableItems)
			{
				var items = new List<ItemData>();
				foreach (ItemData item in pool.Value)
					if (item.eItem != EItem.GoldenRing || UnityEngine.Random.Range(0f, 1f) <= 0.0025f)
						items.Add(item);
				pools.Add(pool.Key, items);
			}
			var result = new List<ItemData>();
			for (int i = 0; i < 3; i++)
			{
				var rarity = Rarity.GetItemRarity(PlayerStats.GetStat(EStat.Luck) * 0.5f);
				if (rarity < itemRarity) rarity = itemRarity;
				var pool = pools[rarity];
				if (pool.Count == 0) continue;
				int index = MyRandom.random.Next(pool.Count);
				result.Add(pool[index]);
				pool.RemoveAt(index);
			}
			return result;
		}

		private static int GetNumUpgrades()
		{
			return 3;
		}

		private static bool CanUnlockItem(IUpgradable upgradable)
		{
			if (upgradable.GetType() == typeof(TomeData)) return CanUnlockTomes();
			if (upgradable is OpenBonkLuaWeaponData || upgradable.GetType() == typeof(WeaponData)) return CanUnlockWeapons();
			return true;
		}

		private static bool CanUnlockWeapons()
		{
			return MyPlayer.Instance.inventory.weaponInventory.GetNumWeapons() < GetNumAvailableWeaponSlots();
		}

		private static bool CanUnlockTomes()
		{
			return MyPlayer.Instance.inventory.tomeInventory.GetNumTomes() < GetNumAvailableTomeSlots();
		}

		public static int GetNumAvailableWeaponSlots()
		{
			return ChallengesTracker.HasChallengeModifier("minimalist") ? 1 : 2 + DataManager.Instance.GetShopItemData(EShopItem.Weapons).GetLevel();
		}

		public static int GetNumAvailableTomeSlots()
		{
			return ChallengesTracker.HasChallengeModifier("minimalist") ? 1 : 2 + DataManager.Instance.GetShopItemData(EShopItem.Tomes).GetLevel();
		}

		public static int GetNumMaxWeaponSlots()
		{
			return 4;
		}

		public static int GetNumMaxTomeSlots()
		{
			return 4;
		}

		public static int GetWeaponMaxLevel()
		{
			return MAX_WEAPON_LEVEL_BASE + GetNumExtraWeaponLevels();
		}

		public static int GetTomeMaxLevel()
		{
			return MAX_TOME_LEVEL_BASE + GetNumExtraTomeLevels();
		}

		public static int GetNumExtraWeaponLevels()
		{
			if (MyPlayer.Instance == null || MyPlayer.Instance.inventory == null) return 0;
			return (MyPlayer.Instance.inventory.itemInventory.GetItem(EItem.Pot) as ItemPotSteel)?.weaponLevels ?? 0;
		}

		public static int GetNumExtraTomeLevels()
		{
			if (MyPlayer.Instance == null || MyPlayer.Instance.inventory == null) return 0;
			return (MyPlayer.Instance.inventory.itemInventory.GetItem(EItem.WizardsHat) as ItemWizardsHat)?.tomeLevels ?? 0;
		}
	}
}
