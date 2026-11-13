using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.UI.Localization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MicrowaveItemButton : MyButton
{
	public RawImage icon;

	private MicrowaveUi microwaveUi;

	private ItemData itemData;

	public void Set(MicrowaveUi microwaveUi, EItem eItem)
	{
		this.microwaveUi = microwaveUi;
		itemData = DataManager.Instance.GetItem(eItem);
		if (icon != null && itemData != null) icon.texture = itemData.icon;
	}

	public void SelectUpgrade()
	{
		if (itemData == null || microwaveUi == null || MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || MyPlayer.Instance.inventory.itemInventory == null) return;
		if (itemData.maxAmount <= 0 || MyPlayer.Instance.inventory.itemInventory.GetAmount(itemData.eItem) < itemData.maxAmount)
		{
			microwaveUi.SelectUpgrade(itemData.eItem);
			return;
		}
		var popup = AlwaysUi.Instance != null ? AlwaysUi.Instance.UiTextPopup : null;
		if (popup != null)
		{
			var fields = new Dictionary<string, string> { ["maxAmount"] = itemData.maxAmount.ToString(), ["itemName"] = itemData.GetName() };
			popup.SetText(LocalizationUtility.GetLocalizedString("PopupText", "MICROWAVE_MAX_ITEMS", fields), transform.position, Color.red);
		}
	}

	public override void StartHover()
	{
		isHovering = true;
	}

	public override void StopHover()
	{
		isHovering = false;
	}

	protected override void OnClick()
	{
	}
}
