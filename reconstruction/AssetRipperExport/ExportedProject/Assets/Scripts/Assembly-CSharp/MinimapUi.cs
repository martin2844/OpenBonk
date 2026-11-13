using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Camera;
using Assets.Scripts.Game.MapGeneration.MapEvents;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Utility;

public class MinimapUi : MonoBehaviour
{
	public Transform border;

	public Transform[] directionLetters;

	public RawImage jammer;

	private Color jammerColor;

	private Color jammerColorClear;

	private void Awake()
	{
		if (SaveManager.Instance != null && SaveManager.Instance.config != null && SaveManager.Instance.config.cfGameSettings != null)
			UpdateScale(SaveManager.Instance.config.cfGameSettings.minimap_size);
		jammerColor = jammer.color;
		jammerColorClear = jammerColor;
		jammerColorClear.a = 0f;
		MinimapCamera.A_RotationUpdated += OnRotationUpdated;
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
	}

	private void OnDestroy()
	{
		MinimapCamera.A_RotationUpdated -= OnRotationUpdated;
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
	}

	private void Update()
	{
		bool jammed = IsJammed();
		if (!jammed && !jammer.enabled) return;
		if (jammed)
		{
			if (!jammer.enabled) jammer.enabled = true;
			jammer.color = Color.Lerp(jammer.color, jammerColor, MyTime.deltaTime * 0.3f);
		}
		else
		{
			jammer.color = Color.Lerp(jammer.color, jammerColorClear, MyTime.deltaTime);
			if (jammer.color.a <= 0.02f) jammer.enabled = false;
		}
	}

	private bool IsJammed()
	{
		return MapEventsDesert.IsActiveStorm() || ChallengesTracker.HasChallengeModifier("blind");
	}

	private void OnRotationUpdated(float y)
	{
		border.eulerAngles = new Vector3(0f, 0f, y);
		foreach (Transform letter in directionLetters) letter.eulerAngles = Vector3.zero;
	}

	private void UpdateScale(float scale)
	{
		transform.localScale = Vector3.one * scale;
	}

	private void OnSettingUpdated(string settingName, object oldValue, object newValue)
	{
		if (settingName == "minimap_size") UpdateScale((float)newValue);
	}
}
