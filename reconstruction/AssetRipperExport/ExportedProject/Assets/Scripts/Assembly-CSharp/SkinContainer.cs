using System;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkinContainer : MyButton, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public RawImage skinIcon;

	public GameObject locked;

	public GameObject notSelectedOverlay;

	public GameObject purchaseOverlay;

	public TextMeshProUGUI t_price;

	public SkinData skin;

	public static Action<SkinContainer> A_Hover;

	public static Action<SkinContainer> A_HoverMouse;

	public static Action<SkinContainer> A_HoverMouseExit;

	public static Action<SkinData> A_Purchased;

	public void SetSkin(SkinData skin)
	{
		this.skin = skin;
		bool unlocked = MyAchievements.IsUnlocked(skin, out _);
		skinIcon.gameObject.SetActive(unlocked);
		locked.SetActive(!unlocked);
		if (unlocked)
		{
			skinIcon.texture = skin.icon;
			purchaseOverlay.SetActive(!MyAchievements.IsPurchased(skin));
			t_price.text = string.Format("<sprite name=silver> {0}", skin.GetPrice());
		}
		else purchaseOverlay.SetActive(false);
	}

	public void SetSelected(bool isSelected)
	{
		notSelectedOverlay.SetActive(!isSelected);
	}

	public override void StartHover()
	{
		TooltipHoverEnter();
		if (!IsLocked()) A_Hover?.Invoke(this);
	}

	private bool IsLocked()
	{
		return locked.activeInHierarchy;
	}

	private bool NeedPurchase()
	{
		return !MyAchievements.IsPurchased(skin);
	}

	public ECharacter GetCharacter()
	{
		return skin.character;
	}

	public override void StopHover()
	{
		TooltipHoverExit();
	}

	private void OnDisable()
	{
		TooltipHoverExit();
	}

	protected override void OnClick()
	{
		if (!NeedPurchase() || !MyAchievements.IsUnlocked(skin, out _)) return;
		int price = skin.GetPrice();
		if (SaveManager.Instance.progression.silver < price)
		{
			AlwaysUi.Instance.UiTextPopup.SetText(Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("PopupText", "CANT_AFFORD"), transform.position, Color.red);
			return;
		}
		AlwaysUi.Instance.dynamicWindows.NewWindowPrompt("Skin", string.Format("Purchase skin for <sprite name=silver> {0}?", price), ConfirmPurchase);
	}

	private void ConfirmPurchase()
	{
		SaveManager.Instance.progression.PurchaseUnlockable(skin);
		purchaseOverlay.SetActive(false);
		A_Purchased?.Invoke(skin);
		A_Hover?.Invoke(this);
	}

	private void TooltipHoverEnter()
	{
		string text = IsLocked() ? skin.GetUnlockRequirement().GetUnlockRequirement() : skin.GetName();
		ToolTip.Instance?.SetTip(text, GetComponent<RectTransform>());
	}

	private void TooltipHoverExit()
	{
		ToolTip.Instance?.HideTip();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		A_HoverMouse?.Invoke(this);
		TooltipHoverEnter();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		A_HoverMouseExit?.Invoke(this);
		TooltipHoverExit();
	}
}
