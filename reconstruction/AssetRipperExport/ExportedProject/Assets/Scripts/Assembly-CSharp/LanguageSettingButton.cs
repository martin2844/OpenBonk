using UnityEngine;

public class LanguageSettingButton : MonoBehaviour
{
	public BetterSetting betterSetting;

	private void Awake()
	{
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
	}

	private void OnDestroy()
	{
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
	}

	private void OnSettingUpdated(string settingName, object oldValue, object newValue)
	{
		if (settingName == "language" && betterSetting != null) betterSetting.UpdateValue();
	}
}
