using System;
using System.Collections.Generic;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.UI.Menu.Windows;
using Assets.Scripts.UI.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using System.Reflection;

public class Settings : MonoBehaviour
{
	public GameObject enumPrefab;

	public GameObject sliderPrefab;

	public GameObject resolutionPrefab;

	public GameObject controlPrefab;

	public GameObject controlPrefabNew;

	public GameObject controllerDisplayPrefab;

	public GameObject headerPrefab;

	public GameObject languagePrefab;

	public Transform videoContent;

	public Transform gameContent;

	public Transform audioContent;

	public Transform controlContent;

	public Transform visualsContent;

	public Transform otherContent;

	public List<BetterSetting> settings;

	public GameObject resolutionWindow;

	public Window settingsWindow;

	public GameObject btn_resetSettings;

	public GameObject b_resetControls;

	public TabsExplicitNavigation gameContentNav;

	public TabsExplicitNavigation settingsNav;

	public static Action A_ResetRewiredControls;

	public static Settings Instance;

	public LocalizedString localizedSyncAchievementsHeader;

	public LocalizedString localizedSyncAchievementsPrompt;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		MyButtonSettingRes.A_Clicked += OpenResolutionWindow;
		if (settings == null) settings = new List<BetterSetting>();
	}

	private void Start()
	{
		if (Instance != this) return;
		UpdateSettings();
		if (settingsWindow != null) settingsWindow.FindAllButtonsInWindow();
	}

	private void OnEnable()
	{
		Invoke(nameof(Rebuild), 0.01f);
	}

	private void Rebuild()
	{
		Assets.Scripts.UI.UiUtility.RebuildUi(transform);
	}

	private void OnDisable()
	{
		if (SaveManager.Instance != null) SaveManager.Instance.SaveConfig();
	}

	private void OnDestroy()
	{
		MyButtonSettingRes.A_Clicked -= OpenResolutionWindow;
		if (Instance == this) Instance = null;
	}

	private void OpenResolutionWindow()
	{
		if (resolutionWindow != null) resolutionWindow.SetActive(true);
	}

	private void UpdateSettings()
	{
		var config = SaveManager.Instance.config;
		Action<string, object, CFSettings> save = CurrentSettings.Instance.BetterUpdateCfSettings;
		CreateGenericSettings(save, gameContent, config.cfGameSettings);
		CreateGenericSettings(save, videoContent, config.cfVideoSettings);
		CreateGenericSettings(save, controlContent, config.cfControlSettings);
		CreateGenericSettings(save, audioContent, config.cfAudioSettings);
		CreateGenericSettings(save, visualsContent, config.cfVisualsSettings);
		CreateGenericSettings(save, otherContent, config.cfOtherSettings);
		if (btn_resetSettings != null)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			btn_resetSettings.SetActive(true);
#endif
			btn_resetSettings.transform.SetAsLastSibling();
		}
		if (b_resetControls != null) b_resetControls.transform.SetAsLastSibling();
		if (gameContentNav != null) gameContentNav.Refresh();
		if (settingsNav != null) settingsNav.Refresh();
	}

	private void CreateGenericSettings(Action<string, object, CFSettings> saveAction, Transform contentParent, CFSettings cfSettings)
	{
		var headers = cfSettings.GetHeaders();
		FieldInfo[] fields = cfSettings.GetType().GetFields();
		for (int index = 0; index < fields.Length; index++)
		{
			SettingHeader header = headers.Find(h => h.index == index);
			if (header != null)
			{
				GameObject heading = Instantiate(headerPrefab, contentParent);
				var headerComponent = heading.GetComponent<SettingsHeaderMonoObject>();
				if (headerComponent != null) headerComponent.Set(header);
				var localized = heading.GetComponentInChildren<LocalizeStringEvent>();
				if (localized != null) { localized.StringReference = new LocalizedString("SettingsHeaders", header.header); localized.RefreshString(); }
			}
			FieldInfo field = fields[index];
			GameObject settingObject = Instantiate(GetSettingPrefab(ConfigSettingsUtility.GetSettingType(field)), contentParent);
			var setting = settingObject.GetComponent<BetterSetting>();
			if (setting == null) continue;
			settings.Add(setting);
			setting.SetSetting(saveAction, field.Name, field.GetValue(cfSettings), this, cfSettings);
		}
		var navigation = contentParent.GetComponent<TabsExplicitNavigation>();
		if (navigation != null) navigation.Refresh();
	}

	private GameObject GetSettingPrefab(SettingType settingType)
	{
		switch (settingType)
		{
			case SettingType.Enum: return enumPrefab;
			case SettingType.Slider: return sliderPrefab;
			case SettingType.Resolution: return resolutionPrefab;
			case SettingType.Control: return controlPrefab;
			case SettingType.Language: return languagePrefab;
			case SettingType.ControlNew: return controlPrefabNew;
			case SettingType.ControllerDisplay: return controllerDisplayPrefab;
			default: throw new ArgumentOutOfRangeException(nameof(settingType));
		}
	}

	public void TryResetSaveFile()
	{
		var windows = AlwaysUi.Instance != null ? AlwaysUi.Instance.dynamicWindows : null;
		if (windows == null) return;
		string header = LocalizedResetText("MainMenuOther", "WARNING", "Warning");
		string content = LocalizedResetText("DynamicWindows", "RESET_SAVE_FILE", "Are you sure you want to reset all of your settings?");
		windows.NewWindowPrompt(header, content, ResetSaveFile);
	}

	private static string LocalizedResetText(string table, string key, string fallback)
	{
		try {
			string text = LocalizationUtility.GetLocalizedString(table, key);
			return string.IsNullOrEmpty(text) || text.StartsWith("Missing localization") ? fallback : text;
		} catch (Exception) { return fallback; }
	}

	public void ResetControls()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		AndroidControllerBindings.Reset();
		if (SaveManager.Instance != null && SaveManager.Instance.config != null && SaveManager.Instance.config.cfControlSettings != null && CurrentSettings.Instance != null)
		{
			CurrentSettings.Instance.BetterUpdateCfSettings("swap_menu_ab", 0, SaveManager.Instance.config.cfControlSettings);
			CurrentSettings.Instance.BetterUpdateCfSettings("touch_controls", 0, SaveManager.Instance.config.cfControlSettings);
		}
		RefreshSettings();
		A_ResetRewiredControls?.Invoke();
#endif
	}

	private void ResetSaveFile()
	{
		var manager = SaveManager.Instance;
		if (manager == null || CurrentSettings.Instance == null) return;
		manager.NewSaveConfig();
		var config = manager.config;
		CFSettings[] categories = { config.cfGameSettings, config.cfVideoSettings, config.cfAudioSettings,
			config.cfControlSettings, config.cfVisualsSettings, config.cfOtherSettings };
		foreach (var setting in settings) if (setting != null) setting.RebindConfig(categories);
		CurrentSettings.Instance.UpdateSave();
		RefreshSettings();
		if (AudioManager.Instance != null && AudioManager.Instance.uiAbort != null)
			AudioManager.Instance.uiAbort.Play();
	}

	public void RefreshSettings()
	{
		foreach (var setting in settings) if (setting != null) setting.UpdateValue();
	}

	public void SyncSteamAchievements()
	{
	}

	private void SyncAchievements()
	{
	}

	private void OnResButtonClicked()
	{
	}

	private void OnResChanged(int resIndex)
	{
	}
}
