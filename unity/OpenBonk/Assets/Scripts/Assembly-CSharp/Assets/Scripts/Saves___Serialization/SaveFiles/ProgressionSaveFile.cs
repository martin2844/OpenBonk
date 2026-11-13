using System;
using System.Collections.Generic;
using Assets.Scripts.Saves___Serialization.Progression;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts._Data.ShopItems;

namespace Assets.Scripts.Saves___Serialization.SaveFiles
{
	[Serializable]
	public class ProgressionSaveFile
	{
		public int gold;

		public int silver;

		public Dictionary<EShopItem, int> shopItems = new Dictionary<EShopItem, int>();

		public Dictionary<ECharacter, CharacterProgression> characterProgression = new Dictionary<ECharacter, CharacterProgression>();

		public HashSet<string> achievements = new HashSet<string>();

		public HashSet<string> claimedAchievements = new HashSet<string>();

		public HashSet<string> purchases = new HashSet<string>();

		public HashSet<string> inactivated = new HashSet<string>();

		public bool hasNewQuestDone;

		public MenuMeta menuMeta = new MenuMeta();

		public HashSet<string> newUnlockables = new HashSet<string>();

		public HashSet<string> newShopItems = new HashSet<string>();

		public HashSet<string> newMaps = new HashSet<string>();

		public static Action<int> A_SilverChanged;

		public static Action<MyAchievement> A_AchievementClaimed;

		public static Action<UnlockableBase> A_UnlockablePurchased;

		public void Init()
		{
			foreach (EShopItem item in Enum.GetValues(typeof(EShopItem)))
				if (!shopItems.ContainsKey(item)) shopItems.Add(item, 0);
			menuMeta ??= new MenuMeta();
			purchases ??= new HashSet<string>();
			inactivated ??= new HashSet<string>();
			claimedAchievements ??= new HashSet<string>();
			achievements ??= new HashSet<string>();
			characterProgression ??= new Dictionary<ECharacter, CharacterProgression>();
			if (gold < 0) gold = 0;
			GameManager.A_GameOver += OnGameOver;
		}

		public void OnDestroy()
		{
			GameManager.A_GameOver -= OnGameOver;
		}

		public void CompleteAchievement(MyAchievement achievement)
		{
			UnlockableBase unlockable = achievement.unlockable;
			if (unlockable is ShopItemData) newShopItems.Add(unlockable.GetInternalName());
			else if (unlockable is MapData) newMaps.Add(unlockable.GetInternalName());
			else if (unlockable is CharacterData || unlockable is WeaponData || unlockable is ItemData || unlockable is TomeData || unlockable is HatData)
				newUnlockables.Add(unlockable.GetInternalName());
			hasNewQuestDone = true;
			achievements.Add(achievement.internalName);
		}

		public bool PurchaseUnlockable(UnlockableBase unlockable)
		{
			if (!unlockable.CanBuy() || purchases.Contains(unlockable.GetInternalName())) return false;
			RemoveSilver(unlockable.GetPrice());
			purchases.Add(unlockable.GetInternalName());
			A_UnlockablePurchased?.Invoke(unlockable);
			return true;
		}

		public bool HasUnclaimedQuests()
		{
			return claimedAchievements.Count != achievements.Count;
		}

		public bool PurchaseShopItem(ShopItemData shopItemData)
		{
			if (!shopItemData.CanBuy()) return false;
			RemoveSilver(shopItemData.GetPrice());
			shopItems[shopItemData.eShopItem]++;
			return true;
		}

		public bool RefundShopItem(ShopItemData shopItemData)
		{
			if (!shopItemData.CanRefund()) return false;
			AddSilver(shopItemData.GetRefundPrice());
			shopItems[shopItemData.eShopItem]--;
			return true;
		}

		public void AddSilver(int change)
		{
			silver = change > 0 && silver > int.MaxValue - change ? int.MaxValue : silver + change;
			A_SilverChanged?.Invoke(change);
			Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.SetValueForce("silver", silver);
		}

		public void RemoveSilver(int change)
		{
			silver -= change;
			A_SilverChanged?.Invoke(-change);
		}

		public void ClaimAchievement(MyAchievement achievement)
		{
			claimedAchievements.Add(achievement.internalName);
			AddSilver(achievement.GetSilverReward());
			A_AchievementClaimed?.Invoke(achievement);
		}

		public bool HasShopItem(EShopItem eShopItem)
		{
			return shopItems[eShopItem] > 0;
		}

		public int GetShopItemLevel(EShopItem eShopItem)
		{
			return shopItems[eShopItem];
		}

		private void OnGameOver()
		{
			var player = Assets.Scripts.Actors.Player.MyPlayer.Instance;
			if (player == null || player.inventory == null || player.inventory.playerXp == null) return;
			CharacterProgression character = GetCharacterProgression(player.character);
			character.OnRunFinished(player.inventory.playerXp.xp);
		}

		public CharacterProgression GetCharacterProgression(ECharacter character)
		{
			characterProgression ??= new Dictionary<ECharacter, CharacterProgression>();
			if (!characterProgression.TryGetValue(character, out CharacterProgression result))
				characterProgression[character] = result = new CharacterProgression();
			return result;
		}
	}
}
