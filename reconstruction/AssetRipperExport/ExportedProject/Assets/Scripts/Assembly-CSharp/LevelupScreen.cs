using System;
using Assets.Scripts.UI.InGame.Rewards;
using TMPro;
using UnityEngine;
using Assets.Scripts.Utility;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.UI.Localization;

public class LevelupScreen : BaseEncounterWindow
{
	public static bool isLevelingUp;

	public GameObject window;

	public GameObject effects;

	public AudioSource audioLevel;

	public AudioSource audioShadyGuy;

	public GameObject button;

	public UpgradePicker upgradePicker;

	public UpgradeInventoryUI upgradeInventoryUi;

	public TextMeshProUGUI t_title;

	public TextMeshProUGUI t_description;

	public MyButtonOffersUtility b_skip;

	public MyButtonOffersUtility b_refresh;

	public MyButtonOffersUtility b_banish;

	public MyButtonNormal b_leave;

	public static Action A_LevelupEnabled;

	public static Action A_LevelUpClose;

	private int level;

	private int currentLevel;

	public Window windowScript;

	private static bool hasBanishes;

	private static bool hasRefreshes;

	private static bool hasSkips;

	private bool hasInitedThisStage;

	private EEncounter encounterType;

	public float refreshTime;

	private void TryInit()
	{
		if (hasInitedThisStage || MapController.index != 0) return;
		var inventory = MyPlayer.Instance.inventory;
		hasBanishes = inventory.banishes > 0;
		hasRefreshes = inventory.refreshes > 0;
		hasSkips = inventory.skips > 0;
		hasInitedThisStage = true;
	}

	public void ShowLevelupScreen()
	{
		TryInit();
		window.SetActive(true);
		currentLevel++;
		ConfigureEncounterPresentation();
		RefreshUtilityButtons();
		upgradePicker.ShuffleUpgrades(encounterType);
		isLevelingUp = encounterType == EEncounter.Levelup;
		if (isLevelingUp) A_LevelupEnabled?.Invoke();
		if (upgradePicker.GetNumUpgrades() == 0 && encounterType != EEncounter.ShadyGuy) CloseLevelupScreen();
	}

	private void ConfigureEncounterPresentation()
	{
		if (effects != null) effects.SetActive(encounterType == EEncounter.Levelup || encounterType == EEncounter.Moai);
		AudioSource sound = encounterType == EEncounter.Levelup || encounterType == EEncounter.Moai ? audioLevel : audioShadyGuy;
		if (sound != null) sound.Play();
		if (encounterType == EEncounter.Levelup)
		{
			if (t_title != null) t_title.text = LocalizationUtility.GetLocalizedString("Game_Ui", "HEADER_LEVELUP");
			if (t_description != null) t_description.gameObject.SetActive(false);
		}
		else if (encounterType == EEncounter.Moai)
		{
			int luck = UnityEngine.Random.Range(0, 3);
			if (UnityEngine.Random.Range(0f, 1f) <= 0.01f) luck = 3;
			upgradePicker.SetMoaiLuck(luck);
			if (t_title != null) t_title.text = LocalizationUtility.GetLocalizedString("Game_Ui", "HEADER_MOAI");
			if (t_description != null) { t_description.gameObject.SetActive(true); t_description.text = GetMoaiText(luck); }
		}
		else if (encounterType == EEncounter.ShadyGuy)
		{
			if (t_title != null) t_title.text = LocalizationUtility.GetLocalizedString("Game_Ui", "HEADER_SHADY");
			if (t_description != null) { t_description.gameObject.SetActive(true); t_description.text = GetShadyGuyText(); }
		}
		if (b_leave != null) b_leave.gameObject.SetActive(encounterType == EEncounter.ShadyGuy);
	}

	private void Update()
	{
		if (window != null && window.activeInHierarchy && encounterType == EEncounter.ShadyGuy &&
			MyInputManager.GetButtonDown(MyInputManager.UICancel))
			CloseLevelupScreen();
	}

	public void CloseLevelupScreen()
	{
		UiManager.Instance.encounterWindows.RewardFinished();
		upgradePicker.StopBanishMode();
	}

	public override void Open(EEncounter encounterType)
	{
		this.encounterType = encounterType;
		ShowLevelupScreen();
	}

	public override void OnClose()
	{
		isLevelingUp = false;
		window.SetActive(false);
		if (effects != null) effects.SetActive(false);
		MyTime.Unpause();
		if (encounterType == EEncounter.Levelup) A_LevelUpClose?.Invoke();
	}

	public override void ChooseOffer(int index)
	{
	}

	public void Leave()
	{
		CloseLevelupScreen();
	}

	public void Skip()
	{
		var inventory = MyPlayer.Instance.inventory;
		int price = GetShopToolPrice(inventory.skipsUsed);
		if (inventory.skips <= 0 || inventory.gold < price) return;
		inventory.ChangeGold(-price);
		inventory.skips--;
		inventory.skipsUsed++;
		CloseLevelupScreen();
	}

	public void Refresh()
	{
		var inventory = MyPlayer.Instance.inventory;
		int price = GetShopToolPrice(inventory.refreshesUsed);
		if (inventory.refreshes <= 0 || inventory.gold < price) return;
		inventory.ChangeGold(-price);
		inventory.refreshes--;
		inventory.refreshesUsed++;
		RefreshUtilityButtons();
		upgradePicker.ShuffleUpgrades(encounterType);
	}

	public void StartBanish()
	{
		var inventory = MyPlayer.Instance.inventory;
		if (inventory.banishes > 0 && inventory.gold >= GetShopToolPrice(inventory.banishesUsed))
			upgradePicker.StartBanishMode();
	}

	public void Banish()
	{
		var inventory = MyPlayer.Instance.inventory;
		int price = GetShopToolPrice(inventory.banishesUsed);
		if (inventory.banishes <= 0 || inventory.gold < price) return;
		inventory.ChangeGold(-price);
		DecrementBanishes();
		upgradePicker.StopBanishMode();
		CloseLevelupScreen();
	}

	private int GetSkips()
	{
		return MyPlayer.Instance.inventory.skips;
	}

	private int GetRefreshes()
	{
		return MyPlayer.Instance.inventory.refreshes;
	}

	public int GetBanishes()
	{
		return MyPlayer.Instance.inventory.banishes;
	}

	private int GetSkipsUsed()
	{
		return MyPlayer.Instance.inventory.skipsUsed;
	}

	private int GetRefreshesUsed()
	{
		return MyPlayer.Instance.inventory.refreshesUsed;
	}

	public int GetBanishesUsed()
	{
		return MyPlayer.Instance.inventory.banishesUsed;
	}

	public void DecrementBanishes()
	{
		MyPlayer.Instance.inventory.banishes--;
		MyPlayer.Instance.inventory.banishesUsed++;
	}

	private void RefreshUtilityButtons()
	{
		var inventory = MyPlayer.Instance.inventory;
		if (inventory.skips > 0) hasSkips = true;
		if (inventory.refreshes > 0) hasRefreshes = true;
		if (inventory.banishes > 0) hasBanishes = true;
		bool allowTools = encounterType != EEncounter.ShadyGuy;
		if (b_skip != null) { b_skip.gameObject.SetActive(allowTools && hasSkips); b_skip.SetAmount(GetSkips(), GetShopToolPrice(GetSkipsUsed())); }
		if (b_refresh != null) { b_refresh.gameObject.SetActive(allowTools && hasRefreshes); b_refresh.SetAmount(GetRefreshes(), GetShopToolPrice(GetRefreshesUsed())); }
		if (b_banish != null) { b_banish.gameObject.SetActive(allowTools && hasBanishes); b_banish.SetAmount(GetBanishes(), GetShopToolPrice(GetBanishesUsed())); }
	}

	public int GetShopToolPrice(int numUses)
	{
		if (numUses == 0) return 0;
		int previousUses = numUses - 1;
		return Mathf.RoundToInt(5f * Mathf.Pow(2f, previousUses) + 15f * previousUses);
	}

	private string GetMoaiText(int level)
	{
		return LocalizationUtility.GetLocalizedString("Game_Moai", string.Format("MOAI_{0}_{1}", level, UnityEngine.Random.Range(0, 3)));
	}

	private string GetShadyGuyText()
	{
		return LocalizationUtility.GetLocalizedString("Game_ShadyGuy", string.Format("SHADY_GUY_{0}", UnityEngine.Random.Range(0, 14)));
	}
}
