using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using UnityEngine;

[CreateAssetMenu(menuName = "Me/Item", order = 1)]
public class ItemData : UnlockableBase
{
	public bool inItemPool;

	public EItem eItem;

	public Texture icon;

	public EItemRarity rarity;

	public MyAchievement unlockRequirement;

	public int maxAmount;

	public int maxAmountPerRun;

	public int itemTickPriority;

	private ItemBase dummyItem;

	public override string GetName()
	{
		return localizedName.GetLocalizedString();
	}

	public override string GetDescription()
	{
		return GetDummyItem().GetDescription(localizedDescription);
	}

	public string GetShortDescription()
	{
		return GetDescription();
	}

	public ItemBase GetDummyItem()
	{
		return dummyItem ??= ItemFactory.CreateItem(eItem, null);
	}

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
		return Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Unlockables", "ITEM", "Item");
	}

	public override string GetInternalName()
	{
		return eItem.ToString();
	}

	public Color GetColor()
	{
		return Assets.Scripts.Utility.MyColorUtility.GetItemRarityColor(rarity);
	}

	public override int CompareTo(UnlockableBase other)
	{
		var item = other as ItemData;
		int rarityOrder = rarity.CompareTo(item.rarity);
		return rarityOrder != 0 ? rarityOrder : GetPrice().CompareTo(item.GetPrice());
	}
}
