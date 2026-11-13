using TMPro;
using Assets.Scripts.Settings___Saves.SaveFiles;

public class BetterDropdownSetting : BetterSetting
{
	public TextMeshProUGUI title;

	public MyButton resButton;

	private void OnEnable() { CurrentSettings.A_ResolutionChanged += OnResolutionChanged; }
	private void OnDisable() { CurrentSettings.A_ResolutionChanged -= OnResolutionChanged; }
	private void OnResolutionChanged(int index)
	{
		if (_settingName == "resolution") UpdateValue();
	}

	public override void ControllerInputDir(int dir, float multiplier)
	{
		if (!IsDisabled() && resButton != null && resButton.GetButton() != null)
			resButton.GetButton().onClick.Invoke();
	}

	protected override void OnSetting()
	{
	}

	public void ValueChanged()
	{
		SaveValue();
	}

	protected override void ShowValue()
	{
		if (title == null) return;
		int current = GetValue();
#if UNITY_ANDROID && !UNITY_EDITOR
		if (_settingName == "resolution" && (options == null || current < 0 || current >= options.Length))
		{
			title.text = $"{UnityEngine.Screen.width}x{UnityEngine.Screen.height}";
			return;
		}
#endif
		title.text = options != null && current >= 0 && current < options.Length ? ConfigSettingsUtility.GetSettingEnumLocalized(options[current]) : "-";
	}

	private int GetValue()
	{
		return _settingValue is int current ? current : -1;
	}
}
