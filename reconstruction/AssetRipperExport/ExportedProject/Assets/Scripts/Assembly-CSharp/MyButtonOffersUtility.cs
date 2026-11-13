using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;

public class MyButtonOffersUtility : MyButton
{
	public TextMeshProUGUI t_amount;

	public TextMeshProUGUI t_price;

	public GameObject disbaledOverlay;

	public MaskableGraphic background;

	public Color defaultColor;

	public Color hoverColor;

	private bool colorInited;

	private bool cantAfford;

	private float refreshedAtTime;

	private void SetColor(Color c)
	{
		if (background == null) return;
		if (!colorInited)
		{
			defaultColor = background.color;
			hoverColor = Color.Lerp(defaultColor, Color.black, .2f);
			colorInited = true;
		}
		background.color = c;
	}

	public void Enable()
	{
		if (disbaledOverlay != null) disbaledOverlay.SetActive(false);
		if (GetButton() != null) GetButton().interactable = true;
	}

	public void Disable()
	{
		if (disbaledOverlay != null) disbaledOverlay.SetActive(true);
		if (GetButton() != null) GetButton().interactable = true;
	}

	public override void StartHover()
	{
		SetColor(hoverColor);
		SetColor(hoverColor);
		isHovering = true;
	}

	public override void StopHover()
	{
		SetColor(defaultColor);
		SetColor(defaultColor);
		isHovering = false;
	}

	protected override void OnClick()
	{
		if (!cantAfford || Time.time <= refreshedAtTime) return;
		var popup = AlwaysUi.Instance != null ? AlwaysUi.Instance.UiTextPopup : null;
		if (popup != null)
			popup.SetText(LocalizationUtility.GetLocalizedString("PopupText", "CANT_AFFORD"), transform.position, MyColorUtility.interactDisabledOutlineColor, .75f);
	}

	public void SetAmount(int n, int price)
	{
		if (t_amount != null) t_amount.text = n.ToString();
		if (t_price != null) t_price.text = price.ToString("N0");
		var inventory = MyPlayer.Instance != null ? MyPlayer.Instance.inventory : null;
		cantAfford = n > 0 && inventory != null && price > inventory.gold;
		if (disbaledOverlay != null) disbaledOverlay.SetActive(n <= 0 || cantAfford);
		if (GetButton() != null) GetButton().interactable = true;
		if (t_price != null) t_price.enabled = n > 0;
		refreshedAtTime = Time.time;
	}
}
