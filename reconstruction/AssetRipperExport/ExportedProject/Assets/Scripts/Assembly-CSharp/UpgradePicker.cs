using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Menu.Windows;
using Assets.Scripts._Data;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_ANDROID
using Assets.Scripts.UI.Mouse;
using TMPro;
using UnityEngine.Localization.Settings;
#endif

public class UpgradePicker : MonoBehaviour
{
	public UpgradeButton[] buttons;

	public LevelupScreen levelupScreen;

	private int numUpgrades;

	public TabsExplicitNavigation tabsExplicitNavigation;

	private EEncounter encounterType;

	public GameObject window;

	private float openedAtTime;

	public static Action A_ShadyGuyDone;

	private int moaiLuckMode;

	public GameObject banisModeOverlay;

	public float banishCooldownOverAtTime;

	public bool banishMode { get; private set; }

#if UNITY_ANDROID
	private UpgradeButton tooltipSelectedOffer;
	private ToolTipsText controllerTooltip;
	private int controllerTooltipIndex = -1;
	private TextMeshProUGUI controllerTooltipHint;
#endif

	public void ShuffleUpgrades(EEncounter encounterType)
	{
#if UNITY_ANDROID
		ClearControllerTooltip();
#endif
		banishCooldownOverAtTime = 0f;
		openedAtTime = Time.time;
		this.encounterType = encounterType;
		numUpgrades = 0;
		foreach (UpgradeButton button in buttons) button.gameObject.SetActive(false);
		if (encounterType == EEncounter.Levelup)
		{
			var offers = InventoryUtility.GetRandomUpgrades();
			for (int i = 0; i < offers.Count && i < buttons.Length; i++)
			{
				buttons[i].gameObject.SetActive(true);
				buttons[i].SetUpgrade(offers[i]);
				numUpgrades++;
			}
		}
		else if (encounterType == EEncounter.Moai)
		{
			var offers = InventoryUtility.GetRandomItemsMoai(moaiLuckMode);
			for (int i = 0; i < offers.Count && i < buttons.Length; i++)
			{
				buttons[i].gameObject.SetActive(true);
				buttons[i].SetItem(offers[i]);
				numUpgrades++;
			}
		}
		else if (encounterType == EEncounter.ShadyGuy)
		{
			var merchant = InteractableShadyGuy.currentlyInteracting;
			for (int i = 0; i < merchant.items.Count && i < buttons.Length; i++)
			{
				buttons[i].gameObject.SetActive(true);
				buttons[i].SetItemPriced(merchant.items[i], merchant.prices[i]);
				numUpgrades++;
			}
		}
		else return;
		for (int i = 0; i < numUpgrades; i++)
		{
			Button button = buttons[i].GetButton();
			Navigation navigation = button.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			navigation.selectOnUp = i > 0 ? buttons[i - 1].GetButton() : null;
			navigation.selectOnDown = i + 1 < numUpgrades ? buttons[i + 1].GetButton() : null;
			button.navigation = navigation;
		}
		if (tabsExplicitNavigation != null) tabsExplicitNavigation.Refresh();
		if (numUpgrades > 0 && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
	}

	public void SetMoaiLuck(int luckMode)
	{
		moaiLuckMode = luckMode;
	}

	private void KeyboardInput()
	{
		if (window == null || !window.activeInHierarchy || (double)Time.time < (double)openedAtTime + 0.15 ||
			SaveManager.Instance?.config?.cfControlSettings == null ||
			SaveManager.Instance.config.cfControlSettings.select_upgrades_with_number_keys != 1)
			return;

		int selected = -1;
		if (Input.GetKeyDown(KeyCode.Alpha1)) selected = 0;
		if (Input.GetKeyDown(KeyCode.Alpha2)) selected = 1;
		if (Input.GetKeyDown(KeyCode.Alpha3)) selected = 2;
		if (Input.GetKeyDown(KeyCode.Alpha4)) selected = 3;
		if (Input.GetKeyDown(KeyCode.Alpha5)) selected = 4;
		if (selected >= 0 && buttons != null && selected < buttons.Length &&
			buttons[selected] != null && buttons[selected].gameObject.activeSelf)
			buttons[selected].SelectUpgrade();
	}

	public void SelectUpgrade(IUpgradable upgradable, List<StatModifier> upgradeOffer, UpgradeButton btn, ERarity rarity)
	{
		if (banishMode)
		{
			if (upgradable.GetLevel() > 0)
			{
				StopBanishMode();
				if (AlwaysUi.Instance != null && AlwaysUi.Instance.UiTextPopup != null)
					AlwaysUi.Instance.UiTextPopup.SetText(Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("PopupText", "CANT_BANISH"), btn.transform.position, Color.white);
				return;
			}
			RunUnlockables.BanishUpgradable((UnlockableBase)upgradable);
			btn.gameObject.SetActive(false);
			levelupScreen.Banish();
			return;
		}
		MyPlayer.Instance.inventory.AddUpgrade(upgradable, upgradeOffer, rarity);
		levelupScreen.CloseLevelupScreen();
	}

	public static void AutoSelectUpgrade()
	{
		var offers = InventoryUtility.GetRandomUpgrades();
		if (offers.Count == 0) return;
		IUpgradable selected = offers[0];
		List<StatModifier> selectedOffer = selected.GetUpgradeOffer(ERarity.Common);
		ERarity selectedRarity = ERarity.New;
		foreach (IUpgradable offer in offers)
		{
			ERarity rarity = offer.GetLevel() > 0 ? Rarity.GetUpgradeOfferRarity(PlayerStats.GetStat(EStat.Luck)) : ERarity.New;
			List<StatModifier> modifiers = offer.GetUpgradeOffer(rarity == ERarity.New ? ERarity.Common : rarity);
			if (rarity > selectedRarity)
			{
				selected = offer;
				selectedOffer = modifiers;
				selectedRarity = rarity;
			}
		}
		MyPlayer.Instance.inventory.AddUpgrade(selected, selectedOffer, selectedRarity);
		if (UiManager.Instance != null && UiManager.Instance.feed != null)
		{
			string color = Assets.Scripts.Utility.MyColorUtility.ColorToHex(Assets.Scripts.Utility.MyColorUtility.RarityToColor(selectedRarity));
			UiManager.Instance.feed.SetFeed("<color=#" + color + ">" + selected.GetName() + "+</color>", 3f, selected.GetIcon());
		}
	}

	public void SelectItem(ItemData itemData)
	{
		if (banishMode)
		{
			RunUnlockables.BanishItem(itemData);
			levelupScreen.Banish();
			return;
		}
		MyPlayer.Instance.inventory.itemInventory.AddItem(itemData.eItem);
		if (encounterType == EEncounter.ShadyGuy) A_ShadyGuyDone?.Invoke();
		levelupScreen.CloseLevelupScreen();
	}

	public int GetNumUpgrades()
	{
		return numUpgrades;
	}

	public void StartBanishMode()
	{
		if (banisModeOverlay != null) banisModeOverlay.SetActive(true);
		banishMode = true;
	}

	private void Banish()
	{
		levelupScreen.Banish();
	}

	public void StopBanishMode()
	{
		banishCooldownOverAtTime = Time.time + .5f;
		banishMode = false;
		if (banisModeOverlay != null) banisModeOverlay.SetActive(false);
		if (buttons != null) foreach (var button in buttons) if (button != null && button.banishOverlay != null) button.banishOverlay.SetActive(false);
	}

	private void Update()
	{
		KeyboardInput();
		if (banishMode && MyInputManager.GetButtonDown(MyInputManager.UICancel))
			StopBanishMode();
#if UNITY_ANDROID
		UpdateControllerTooltip();
#endif
	}

#if UNITY_ANDROID
	private void OnDisable() { ClearControllerTooltip(); }

	private void UpdateControllerTooltip()
	{
		GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
		UpgradeButton offer = selected != null ? selected.GetComponent<UpgradeButton>() : null;
		if (window == null || !window.activeInHierarchy || offer == null || offer.upgradePicker != this || !offer.gameObject.activeInHierarchy) offer = null;
		if (offer != tooltipSelectedOffer)
		{
			ClearControllerTooltip();
			tooltipSelectedOffer = offer;
			RefreshControllerTooltipHint();
		}
		if (tooltipSelectedOffer != null && Input.GetKeyDown(KeyCode.JoystickButton9)) CycleControllerTooltip();
	}

	internal void OnControllerOfferTextChanged(UpgradeButton offer)
	{
		if (offer != tooltipSelectedOffer) return;
		if (controllerTooltip != null) controllerTooltip.CloseControllerLink();
		controllerTooltip = null;
		controllerTooltipIndex = -1;
		RefreshControllerTooltipHint();
	}

	private static int LinkCount(TMP_Text text)
	{
		if (text == null || !text.gameObject.activeInHierarchy || text.GetComponent<ToolTipsText>() == null) return 0;
		text.ForceMeshUpdate();
		return text.textInfo.linkCount;
	}

	internal void CycleControllerTooltip()
	{
		if (tooltipSelectedOffer == null) return;
		int nameCount = LinkCount(tooltipSelectedOffer.t_name);
		int descriptionCount = LinkCount(tooltipSelectedOffer.t_description);
		int count = nameCount + descriptionCount;
		if (count == 0) return;
		int next = controllerTooltipIndex + 1;
		if (controllerTooltip != null) controllerTooltip.CloseControllerLink();
		controllerTooltip = null;
		controllerTooltipIndex = -1;
		if (next >= count) return;
		TMP_Text text = next < nameCount ? tooltipSelectedOffer.t_name : tooltipSelectedOffer.t_description;
		int localIndex = next < nameCount ? next : next - nameCount;
		var detector = text.GetComponent<ToolTipsText>();
		if (detector != null && detector.OpenControllerLink(localIndex))
		{
			controllerTooltip = detector;
			controllerTooltipIndex = next;
		}
	}

	private void RefreshControllerTooltipHint()
	{
		if (controllerTooltipHint != null) Destroy(controllerTooltipHint.gameObject);
		controllerTooltipHint = null;
		if (tooltipSelectedOffer == null || LinkCount(tooltipSelectedOffer.t_name) + LinkCount(tooltipSelectedOffer.t_description) == 0) return;
		var description = tooltipSelectedOffer.t_description;
		if (description == null) return;
		var hintObject = new GameObject("Controller tooltip hint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		hintObject.transform.SetParent(tooltipSelectedOffer.transform, false);
		var rect = (RectTransform)hintObject.transform;
		rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
		rect.pivot = new Vector2(1f, 0f);
		rect.anchoredPosition = new Vector2(-8f, 2f);
		rect.sizeDelta = new Vector2(260f, 28f);
		controllerTooltipHint = hintObject.GetComponent<TextMeshProUGUI>();
		controllerTooltipHint.font = description.font;
		controllerTooltipHint.fontSize = Mathf.Min(description.fontSize, 22f);
		controllerTooltipHint.color = Color.white;
		controllerTooltipHint.alignment = TextAlignmentOptions.BottomRight;
		controllerTooltipHint.raycastTarget = false;
		string locale = LocalizationSettings.SelectedLocale != null ? LocalizationSettings.SelectedLocale.Identifier.Code : "en";
		controllerTooltipHint.text = locale.StartsWith("es", StringComparison.OrdinalIgnoreCase) ? "R3: explicación" : "R3: details";
	}

	private void ClearControllerTooltip()
	{
		if (controllerTooltip != null) controllerTooltip.CloseControllerLink();
		controllerTooltip = null;
		controllerTooltipIndex = -1;
		tooltipSelectedOffer = null;
		if (controllerTooltipHint != null) Destroy(controllerTooltipHint.gameObject);
		controllerTooltipHint = null;
	}
#endif
}
