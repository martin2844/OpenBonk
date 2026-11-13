using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Assets.Scripts.UI.Localization;

public class FpsLimitSetting : MonoBehaviour
{
	private BetterSetting betterSetting;

	private void Start()
	{
		betterSetting = GetComponent<BetterSetting>();
		Refresh();
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
		LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
	}

	private void OnDestroy()
	{
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
		LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
	}

	private void OnSettingUpdated(string settingName, object oldValue, object newValue)
	{
		if (settingName == "vsync") Refresh();
	}

	private void OnLocaleChanged(Locale locale)
	{
		Refresh();
	}

	private void Refresh()
	{
		if (SaveManager.Instance == null || SaveManager.Instance.config == null ||
		    SaveManager.Instance.config.cfVideoSettings == null || betterSetting == null) return;
#if UNITY_ANDROID && !UNITY_EDITOR
		// Android uses targetFrameRate regardless of vSyncCount.
		betterSetting.Enable();
#else
		if (SaveManager.Instance.config.cfVideoSettings.vsync == 0)
		{
			betterSetting.Enable();
			return;
		}
		string requires = LocalizationUtility.GetLocalizedStringReference("Other", "REQUIRES").GetLocalizedString();
		string vsync = LocalizationUtility.GetLocalizedStringReference("SettingsVideo", "vsync").GetLocalizedString();
		string off = LocalizationUtility.GetLocalizedStringReference("SettingEnums", "Off").GetLocalizedString();
		betterSetting.Disable(requires + ": " + vsync + " - " + off);
#endif
	}
}
