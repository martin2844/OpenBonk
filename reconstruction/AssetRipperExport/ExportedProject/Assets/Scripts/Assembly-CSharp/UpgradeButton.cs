using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts._Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Utility;

public class UpgradeButton : MyButton
{
	public RawImage icon;

	public TextMeshProUGUI t_name;

	public TextMeshProUGUI t_description;

	public TextMeshProUGUI t_level;

	public TextMeshProUGUI t_rarity;

	public RawImage iconBorder;

	public RawImage iconBackground;

	public Image background;

	public Image backgroundOverlay;

	public GameObject banishOverlay;

	private IUpgradable upgradable;

	private List<StatModifier> upgradeOffer;

	public UpgradePicker upgradePicker;

	public TomeSynergiesUi tomeSynergiesUi;

	private bool isItem;

	private ItemData itemData;

	private ERarity rarity;

	private int price;

	private bool canAfford;

	public GameObject overlayCantAfford;

	public void SetUpgrade(IUpgradable upgradable)
	{
		isItem = false;
		this.upgradable = upgradable;
		int level = upgradable.GetLevel();
		rarity = level > 0 ? Rarity.GetUpgradeOfferRarity(PlayerStats.GetStat(EStat.Luck)) : ERarity.New;
		upgradeOffer = upgradable.GetUpgradeOffer(level > 0 ? rarity : ERarity.Common);
		Color rarityColor = MyColorUtility.RarityToColor(rarity);
		if (t_rarity != null) t_rarity.color = rarityColor;
		if (iconBorder != null) iconBorder.color = rarityColor;
		if (iconBackground != null) iconBackground.color = rarityColor;
		if (background != null) background.color = MyColorUtility.GetRarityColorBackground(rarity);
		if (icon != null) icon.texture = upgradable.GetIcon();
		string localizedName = upgradable.GetName();
		if (t_name != null) t_name.text = string.IsNullOrEmpty(localizedName) ? ((UnlockableBase)upgradable).GetInternalName() : localizedName;
		if (t_level != null) t_level.text = level > 0 ? $"LVL {level}" : "NEW";
		if (t_rarity != null) t_rarity.text = rarity.ToString();
		if (t_description != null)
		{
			string description = upgradable.GetUpgradeDescription(level, upgradeOffer, rarity);
			if (!string.IsNullOrEmpty(description)) { t_description.text = description; }
			else
			{
			var lines = new List<string>();
			foreach (StatModifier modifier in upgradeOffer)
				lines.Add(modifier.modifyType == EStatModifyType.Multiplication
					? $"{modifier.stat}: ×{modifier.modification:0.##}"
					: $"{modifier.stat}: {modifier.modification:+0.##;-0.##;0}");
			t_description.text = string.Join("\n", lines);
			}
		}
		#if UNITY_ANDROID
		if (upgradePicker != null) upgradePicker.OnControllerOfferTextChanged(this);
		#endif
		GetButton().interactable = false;
		Invoke(nameof(EnableButton), 0.5f);
	}

	public void SetItem(ItemData itemData)
	{
		isItem = true;
		price = 0;
		canAfford = true;
		if (overlayCantAfford != null) overlayCantAfford.SetActive(false);
		this.itemData = itemData;
		Color rarityColor = itemData.GetColor();
		if (t_rarity != null) t_rarity.color = rarityColor;
		if (iconBorder != null) iconBorder.color = rarityColor;
		if (iconBackground != null) iconBackground.color = rarityColor;
		if (background != null) background.color = MyColorUtility.GetRarityColorBackground(itemData.rarity);
		if (icon != null) icon.texture = itemData.icon;
		if (t_name != null) t_name.text = itemData.GetName();
		if (t_description != null) t_description.text = itemData.GetDummyItem().GetDescription(itemData.localizedDescription);
		if (t_level != null) t_level.text = "";
		if (t_rarity != null) t_rarity.text = itemData.rarity.ToString();
		#if UNITY_ANDROID
		if (upgradePicker != null) upgradePicker.OnControllerOfferTextChanged(this);
		#endif
		GetButton().interactable = false;
		Invoke(nameof(EnableButton), 0.5f);
	}

	public void SetItemPriced(ItemData itemData, int price)
	{
		SetItem(itemData);
		this.price = price;
		canAfford = Assets.Scripts.Actors.Player.MyPlayer.Instance.inventory.gold >= price;
		if (overlayCantAfford != null) overlayCantAfford.SetActive(!canAfford);
		if (t_level != null) t_level.text = string.Format("<color=white><sprite name=gold> {0:N0}", price);
	}

	private void EnableButton()
	{
		GetButton().interactable = true;
	}

	public void SelectUpgrade()
	{
		if (Time.time < upgradePicker.banishCooldownOverAtTime) return;
		if (isItem)
		{
			if (!canAfford || Assets.Scripts.Actors.Player.MyPlayer.Instance.inventory.goldInt < price)
			{
				if (AlwaysUi.Instance != null && AlwaysUi.Instance.UiTextPopup != null)
					AlwaysUi.Instance.UiTextPopup.SetText(
						Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("PopupText", "CANT_AFFORD"),
						transform.position, Color.red);
				return;
			}
			upgradePicker.SelectItem(itemData);
			if (price > 0) Assets.Scripts.Actors.Player.MyPlayer.Instance.inventory.ChangeGold(-price);
		}
		else upgradePicker.SelectUpgrade(upgradable, upgradeOffer, this, rarity);
	}

	public override void StartHover()
	{
		if (banishOverlay != null) banishOverlay.SetActive(upgradePicker != null && upgradePicker.banishMode);
		isHovering = true;
		if (backgroundOverlay != null) backgroundOverlay.enabled = true;
	}

	public override void StopHover()
	{
		isHovering = false;
		if (backgroundOverlay != null) backgroundOverlay.enabled = false;
		if (banishOverlay != null) banishOverlay.SetActive(false);
	}

	protected override void OnClick()
	{
	}
}
