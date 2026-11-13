using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts._Data;
using Assets.Scripts._Data.Tomes;
using UnityEngine;

[CreateAssetMenu(menuName = "Me/Tome", order = 1)]
public class TomeData : UnlockableBase, IUpgradable
{
	public ETome eTome;

	[TextArea]
	public string description;

	public StatModifier statModifier;

	public Texture icon;

	public UpgradeData upgradeData;

	[Header("Unlocks")]
	public MyAchievement AchievementRequirement;

	public string GetUpgradeDescription(int level, List<StatModifier> upgradeOffer, ERarity rarity)
	{
		if (eTome == ETome.Balance) return TomeUtility.GetUpgradeDescription(this, rarity);
		if (eTome == ETome.Chaos || eTome == ETome.Gambler || eTome == ETome.Hoarder) return GetDescription();
		return level > 0 ? StatUtility.GetUpgradeDescriptionTome(upgradeOffer, this) : StatUtility.GetUpgradeDescriptionTomeModifier(statModifier, this);
	}

	public override string GetDescription()
	{
		if (eTome == ETome.Balance) return TomeUtility.GetUpgradeDescription(this, ERarity.New);
		if (eTome == ETome.Chaos || eTome == ETome.Gambler || eTome == ETome.Hoarder) return localizedDescription.GetLocalizedString();
		return "+" + Assets.Scripts.UI.Localization.LocalizationUtility.GetStatName(statModifier.stat) + " - " + Assets.Scripts.UI.Localization.LocalizationUtility.GetStatDesc(statModifier.stat);
	}

	public override Texture GetIcon()
	{
		return icon;
	}

	public override MyAchievement GetUnlockRequirement()
	{
		return AchievementRequirement;
	}

	public override UnlockableBase GetUnlockableRequirement()
	{
		return null;
	}

	public override string GetUnlockableTypeDisplayString()
	{
		return Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Unlockables", "TOME_BOOK", "Tome");
	}

	public override string GetInternalName()
	{
		return eTome.ToString();
	}

	public int GetLevel()
	{
		return GameManager.Instance.GetPlayerInventory().tomeInventory.GetTomeLevel(eTome);
	}

	public int GetMaxLevel()
	{
		return InventoryUtility.GetTomeMaxLevel();
	}

	public List<StatModifier> GetUpgradeOffer(ERarity rarity)
	{
		if (eTome == ETome.Balance) return new List<StatModifier>();
		if (rarity == ERarity.New) return new List<StatModifier> { statModifier };
		return new List<StatModifier> { new StatModifier { stat = statModifier.stat, modifyType = statModifier.modifyType, modification = StatUtility.GetRarityValue(statModifier.modification, rarity) } };
	}
}
