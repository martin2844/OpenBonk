using TMPro;
using UnityEngine;
using Assets.Scripts.Settings___Saves.SaveFiles;

public class EnumSetting : BetterSetting
{
	public TextMeshProUGUI valueText;

	public void UpdateValue(int dir)
	{
		int next = GetValue() + dir;
		if (next < 0 || next >= options.Length) return;
		_settingValue = next;
		ShowValue();
		SaveValue();
	}

	public override void ControllerInputDir(int dir, float multiplier)
	{
		UpdateValue(dir);
	}

	protected override void ShowValue()
	{
		int current = GetValue();
		if (options.Length == 0 || current < 0 || current >= options.Length)
		{
			Debug.LogError($"Setting {_settingName} has invalid value. Value: {current}, options length: {options.Length}");
			return;
		}
		string dots = "";
		for (int i = 0; i < options.Length; i++)
		{
			dots += i == current ? "<sprite name=\"CircleFilled\">" : "<sprite name=\"CircleHollow\">";
			if (i + 1 < options.Length) dots += "  ";
		}
		valueText.text = ConfigSettingsUtility.GetSettingEnumLocalized(options[current]) + "\n<size=45%>" + dots;
	}

	private int GetValue()
	{
		return (int)_settingValue;
	}
}
