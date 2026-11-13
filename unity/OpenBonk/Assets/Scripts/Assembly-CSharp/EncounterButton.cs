using Assets.Scripts.UI.InGame.Rewards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;

public class EncounterButton : MyButton
{
	public GameObject rarityContainer;

	public Image background;

	public Image selectedOverlay;

	public RawImage iconBorder;

	public RawImage iconBackground;

	public TextMeshProUGUI t_description;

	public TextMeshProUGUI t_rarity;

	public int index;

	private EncounterUi encounterUi;

	private bool canAccept = true;

	private string cantAcceptReason;

	public void Init(EncounterUi encounterUi, int index)
	{
		this.encounterUi = encounterUi;
		this.index = index;
	}

	public void SetOffer(EncounterOffer offer, bool showRarity)
	{
		StopHover();
		rarityContainer.SetActive(showRarity);
		if (showRarity)
		{
			t_rarity.text = LocalizationUtility.GetRarity(offer.rarity);
			iconBorder.color = MyColorUtility.RarityToColor(offer.rarity);
			iconBackground.color = MyColorUtility.GetRarityColorBackground(offer.rarity);
			background.color = MyColorUtility.GetRarityColorBackground(offer.rarity);
		}
		t_description.text = offer.GetEffectsDescription();
		canAccept = offer.CanAccept(out cantAcceptReason);
		TimeoutButton(.5f);
	}

	private void TimeoutButton(float time)
	{
		GetComponent<Button>().interactable = false;
		Invoke(nameof(EnableButton), time);
	}

	private void EnableButton()
	{
		GetComponent<Button>().interactable = true;
	}

	public void SetDeclineOffer(int numOffers)
	{
		canAccept = true;
		cantAcceptReason = null;
		rarityContainer.SetActive(false);
		background.color = Color.Lerp(MyColorUtility.GetHexToColor(MyColorUtility.negativeColorString), Color.black, .8f);
		t_description.text = LocalizationUtility.GetLocalizedString("Game_Ui", "IGNORE_OFFERS") ?? "";
		TimeoutButton(.5f);
	}

	public override void StartHover()
	{
		isHovering = true;
		selectedOverlay.enabled = true;
	}

	public override void StopHover()
	{
		isHovering = false;
		selectedOverlay.enabled = false;
	}

	protected override void OnClick()
	{
		if (canAccept) encounterUi.ChooseOffer(index);
		else AlwaysUi.Instance.UiTextPopup.SetText(cantAcceptReason, transform.position, new Color(1f, .25f, .25f, 1f));
	}
}
