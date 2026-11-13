using System;
using System.Collections.Generic;
using Assets.Scripts.Settings___Saves.SaveFiles;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public abstract class BetterSetting : MonoBehaviour
{
	protected int value;

	protected int settingType;

	protected string[] options;

	public TextMeshProUGUI settingName;

	private string description;

	private Settings settings;

	public GameObject disabledOverlay;

	public TextMeshProUGUI t_disabledText;

	private bool hasSubscribed;

	private RectTransform rectTransform;

	private bool subscribed;

	private Action<string, object, CFSettings> saveAction;

	protected string _settingName;

	protected object _settingValue;

	private CFSettings cfSettings;

	private HashSet<string> hiddenSettings = new HashSet<string> { "pege_mode", "unlock_all", "debug_enemy_scaling", "quick_reset_time" };

	private HashSet<string> advancedSettings = new HashSet<string> { "keyboard_jump_ignore_extra_jumps", "space_navigation", "show_hud", "skip_chest_animation", "skip_portal_animation", "enable_silver_pots", "show_item_feed", "super_quick_resets", "keyboard_hold_to_wallrun", "controller_hold_to_wallrun", "xp_gold_hud" };

	private bool mouseOver;

	protected void Awake()
	{
	}

	private void TrySubscribe()
	{
	}

	private void Start()
	{
	}

	protected void OnDestroy()
	{
		if (subscribed) CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
		if (hasSubscribed) LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
	}

	private void OnSettingUpdated(string settingName, object oldValue, object newValue)
	{
		if (settingName == "show_advanced_settings") CheckVisibility();
	}

	private void Update()
	{
	}

	public virtual void SetSetting(Action<string, object, CFSettings> saveAction, string settingName, object currentValue, Settings settings, CFSettings cfSettings)
	{
		this.saveAction = saveAction;
		_settingName = settingName;
		_settingValue = currentValue;
		this.settings = settings;
		this.cfSettings = cfSettings;
		options = ConfigSettingsUtility.GetSettingValues(settingName);
		description = ConfigSettingsUtility.GetSettingDescription(settingName);
		this.settingName.text = ConfigSettingsUtility.SettingNameToReadable(settingName, cfSettings);
		OnSetting();
		ShowValue();
		CheckExtraScripts();
		if (!subscribed) { CurrentSettings.A_SettingUpdated += OnSettingUpdated; subscribed = true; }
		if (!hasSubscribed) { LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged; hasSubscribed = true; }
		CheckVisibility();
	}

	private void CheckVisibility()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (_settingName == "vsync" || _settingName == "keyboard_hold_to_wallrun") { gameObject.SetActive(false); return; }
#endif
		if (hiddenSettings.Contains(_settingName)) { gameObject.SetActive(false); return; }
		if (advancedSettings.Contains(_settingName))
			gameObject.SetActive(SaveManager.Instance != null && SaveManager.Instance.config != null && SaveManager.Instance.config.cfGameSettings.show_advanced_settings == 1);
	}

	private void OnLocaleChanged(Locale locale)
	{
		RefreshLanguage();
	}

	private void RefreshLanguage()
	{
		if (settingName == null || cfSettings == null || _settingName == null) return;
		settingName.text = ConfigSettingsUtility.SettingNameToReadable(_settingName, cfSettings);
		ShowValue();
	}

	public void UpdateValue()
	{
		var field = cfSettings.GetType().GetField(_settingName);
		if (field == null) return;
		_settingValue = field.GetValue(cfSettings);
		options = ConfigSettingsUtility.GetSettingValues(_settingName);
		OnSetting();
		ShowValue();
	}

	// Resetting settings replaces the category objects. Keep existing widgets
	// bound to the new save rather than writing into the discarded objects.
	public void RebindConfig(CFSettings[] categories)
	{
		if (cfSettings == null || categories == null) return;
		foreach (var category in categories)
			if (category != null && category.GetType() == cfSettings.GetType())
			{
				cfSettings = category;
				return;
			}
	}

	public abstract void ControllerInputDir(int dir, float multiplier);

	protected void SaveValue()
	{
		saveAction(_settingName, _settingValue, cfSettings);
	}

	protected virtual void OnSetting()
	{
	}

	protected abstract void ShowValue();

	private void OnMouseEnter()
	{
	}

	private void CustomPointerHandler()
	{
	}

	private void CheckExtraScripts()
	{
		if (_settingName == "fps_limit" && GetComponent<FpsLimitSetting>() == null)
			gameObject.AddComponent<FpsLimitSetting>();
		if ((_settingName == "inverted_horizontal_axis" || _settingName == "inverted_vertical_axis") && GetComponent<InvertedSettingTroll>() == null)
			gameObject.AddComponent<InvertedSettingTroll>();
	}

	public void Disable(string disableText)
	{
		if (disabledOverlay != null) disabledOverlay.SetActive(true);
		if (t_disabledText != null) t_disabledText.text = disableText;
	}

	public void Enable()
	{
		if (disabledOverlay != null) disabledOverlay.SetActive(false);
	}

	public bool IsDisabled()
	{
		return disabledOverlay != null && disabledOverlay.activeSelf;
	}
}
