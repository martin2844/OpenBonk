using Assets.Scripts.Settings___Saves.SaveFiles;
using UnityEngine;

public class SettingsHeaderMonoObject : MonoBehaviour
{
	private SettingHeader settingHeader;

	public void Set(SettingHeader settingHeader)
	{
		this.settingHeader = settingHeader;
	}

	private void Start()
	{
		CheckVisibility();
	}

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
		if (settingName == "show_advanced_settings") CheckVisibility();
	}

	private void CheckVisibility()
	{
		if (settingHeader == null || settingHeader.header != "ADVANCED") return;
		var config = SaveManager.Instance != null ? SaveManager.Instance.config : null;
		gameObject.SetActive(config != null && config.cfGameSettings != null && config.cfGameSettings.show_advanced_settings == 1);
	}
}
