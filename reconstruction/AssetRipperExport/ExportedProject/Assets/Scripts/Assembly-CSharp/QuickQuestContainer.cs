using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class QuickQuestContainer : MonoBehaviour
{
	public RawImage icon;

	public RawImage progress;

	public TextMeshProUGUI t_name;

	public TextMeshProUGUI t_desc;

	public TextMeshProUGUI t_progress;

	private MyAchievement currentAchievement;

	private void Awake()
	{
		UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
	}

	private void OnDestroy()
	{
		UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
	}

	private void OnLocaleChanged(Locale obj)
	{
		SetQuest(currentAchievement);
	}

	public void SetQuest(MyAchievement ach)
	{
		if (ach == null) return;
		currentAchievement = ach;
		icon.texture = ach.GetIcon();
		t_name.text = ach.GetDisplayName();
		t_desc.text = ach.GetUnlockDescription();
		float value = ach.GetProgress();
		progress.transform.localScale = new Vector3(value, 1f, 1f);
		Color color = MyColorUtility.GetRedToGreenGradient(value);
		progress.color = color;
		t_progress.color = Color.Lerp(color, Color.white, .5f);
		t_progress.text = string.Format("{0} / {1}", ach.GetCurrentValue(), ach.targetValue);
	}
}
