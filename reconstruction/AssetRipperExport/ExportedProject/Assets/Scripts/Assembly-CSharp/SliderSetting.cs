using System;
using Assets.Scripts.Settings___Saves.SaveFiles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderSetting : BetterSetting
{
	public TMP_InputField valueText;

	public Slider slider;

	private bool useWholeNumbers;

	public override void SetSetting(Action<string, object, CFSettings> saveAction, string settingName, object currentValue, Settings settings, CFSettings cfSettings)
	{
		base.SetSetting(saveAction, settingName, currentValue, settings, cfSettings);
		useWholeNumbers = ConfigSettingsUtility.GetSliderWholeNumbers(settingName);
		slider.wholeNumbers = useWholeNumbers;
		valueText.contentType = useWholeNumbers ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;
		ConfigSettingsUtility.GetSliderRange(settingName, out float min, out float max);
		slider.minValue = min;
		slider.maxValue = max;
		// Base.SetSetting displays the value before this slider has its own range.
		// Reapply it after configuring the range and number format.
		ShowValue();
	}

	public void UpdateValueSlider()
	{
		_settingValue = slider.value;
		valueText.text = _settingValue.ToString();
		ShowValue();
		SaveValue();
	}

	public void UpdateValueInputField()
	{
		_settingValue = Mathf.Clamp(float.Parse(valueText.text), slider.minValue, slider.maxValue);
		slider.value = (float)_settingValue;
		ShowValue();
		SaveValue();
	}

	public override void ControllerInputDir(int dir, float multiplier)
	{
		if (IsDisabled()) return;
		float increment = useWholeNumbers ? 1f : ConfigSettingsUtility.GetSliderIncrement(_settingName);
		if (!useWholeNumbers)
		{
			if (increment == -1f) increment = (slider.maxValue - slider.minValue) / 100f;
			increment *= multiplier;
		}
		slider.value = Mathf.Clamp(slider.value + increment * dir, slider.minValue, slider.maxValue);
		UpdateValueSlider();
	}

	private float GetValue()
	{
		return (float)_settingValue;
	}

	protected override void ShowValue()
	{
		float current = GetValue();
		valueText.SetTextWithoutNotify(string.Format(useWholeNumbers ? "{0:N0}" : "{0:F2}", current));
		slider.SetValueWithoutNotify(current);
	}
}
