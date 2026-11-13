using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.UI.Localization;
using UnityEngine;

[CreateAssetMenu(menuName = "Me/Shop Item", order = 1)]
public class ShopItemData : UnlockableBase
{
	public EShopItem eShopItem;

	public Texture icon;

	public int maxLevel;

	public MyAchievement unlockRequirement;

	public bool canRefund;

	public float value;

	public int sortingOrder;

	[Header("Price Settings")]
	public int linearIncrease;

	public float exponentialMultiplier;

	public override Texture GetIcon()
	{
		return icon;
	}

	public override MyAchievement GetUnlockRequirement()
	{
		return unlockRequirement;
	}

	public override UnlockableBase GetUnlockableRequirement()
	{
		return null;
	}

	public override string GetUnlockableTypeDisplayString()
	{
		return LocalizationUtility.GetLocalizedString("Unlockables", "SHOP_ITEM");
	}

	public override string GetInternalName()
	{
		return eShopItem.ToString();
	}

	private int GetLevelPrice(int level)
	{
		return level == 0 ? price : Mathf.RoundToInt(price * Mathf.Pow(exponentialMultiplier, level)) + linearIncrease * level;
	}

	public override int GetPrice()
	{
		return GetLevelPrice(GetLevel());
	}

	public int GetRefundPrice()
	{
		return GetLevelPrice(GetLevel() - 1);
	}

	public int GetLevel()
	{
		return SaveManager.Instance.progression.shopItems[eShopItem];
	}

	public bool IsMaxLevel()
	{
		return GetLevel() >= GetMaxLevel();
	}

	public new bool CanBuy()
	{
		return !IsMaxLevel() && SaveManager.Instance.progression.silver >= GetPrice();
	}

	public bool CanRefund()
	{
		return canRefund && GetLevel() > 0;
	}

	public int GetMaxLevel()
	{
		if (eShopItem == EShopItem.Weapons)
			return (MyAchievements.IsUnlockedInternalNameAch("a_weaponSlots") ? 1 : 0) + (MyAchievements.IsUnlockedInternalNameAch("a_weaponSlots2") ? 1 : 0);
		if (eShopItem == EShopItem.Tomes)
			return (MyAchievements.IsUnlockedInternalNameAch("a_tomeSlots") ? 1 : 0) + (MyAchievements.IsUnlockedInternalNameAch("a_tomeSlots2") ? 1 : 0);
		return maxLevel;
	}

	public override int CompareTo(UnlockableBase other)
	{
		if (other is ShopItemData shop)
		{
			int order = shop.sortingOrder.CompareTo(sortingOrder);
			if (order != 0) return order;
		}
		return GetPrice().CompareTo(other.GetPrice());
	}
}
