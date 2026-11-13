using UnityEngine;
using Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers;
using Assets.Scripts.UI.Localization;

public class InvertedSettingTroll : MonoBehaviour
{
	private BetterSetting betterSetting;

	private static int trollStage;

	private float startedTrollingTime;

	private float trollCooldownSeconds = 20f;

	private void Start()
	{
		betterSetting = GetComponent<BetterSetting>();
		Refresh();
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
	}

	private void OnDestroy()
	{
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
	}

	private void OnSettingUpdated(string settingName, object oldValue, object newValue)
	{
		if (settingName == "vsync") Refresh();
	}

	private void Refresh()
	{
		if (SaveManager.Instance == null || SaveManager.Instance.config == null || betterSetting == null) return;
		if (!ChallengeModifierInvertedControls.disableInvertedControlsOptions)
		{
			betterSetting.Enable();
			return;
		}
		if (trollStage != 0 && Time.time - startedTrollingTime <= trollCooldownSeconds) return;
		string key;
		if (GameManager.Instance != null)
		{
			startedTrollingTime = Time.time;
			trollStage = 1;
			key = "TROLL_INVERTED_CONTROLS";
		}
		else key = "TROLL_INVERTED_CONTROLS_2";
		betterSetting.Disable(LocalizationUtility.GetLocalizedString("Other", key) ?? string.Empty);
	}
}
