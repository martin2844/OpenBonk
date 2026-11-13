using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RequirementPrefab : MonoBehaviour
{
	public GameObject checkMark;

	public GameObject progress;

	public TextMeshProUGUI t_requirement;

	public RawImage progressBar;

	public TextMeshProUGUI t_progress;

	public void Set(MyAchievement ach)
	{
		if (checkMark != null) checkMark.SetActive(ach != null && MyAchievements.IsUnlocked(ach));
		if (t_requirement != null) t_requirement.text = ach == null ? "Available in Full Game" : ach.GetUnlockRequirement();
		bool tracking = ach != null && ach.IsTrackingStat();
		if (progress != null) progress.SetActive(tracking);
		if (!tracking) return;
		float value = MyStats.GetStat(ach.statName);
		if (t_progress != null) t_progress.text = string.Format("{0:N0} / {1:N0}", Mathf.Min(value, ach.targetValue), ach.targetValue);
		float ratio = Mathf.Clamp01(value / ach.targetValue);
		if (progressBar != null)
		{
			progressBar.transform.localScale = new Vector3(ratio, 1f, 1f);
			progressBar.color = MyColorUtility.GetRedToGreenGradient(ratio);
		}
	}

	public void Set(UnlockableBase unlockable)
	{
		if (unlockable == null) return;
		if (checkMark != null) checkMark.SetActive(MyAchievements.IsPurchased(unlockable));
		if (t_requirement != null) t_requirement.text = LocalizationUtility.GetLocalizedString("Main Menu", "BUTTON_BUY_PURCHASE") + ": " + unlockable.GetUnlockableTypeDisplayString() + " - " + unlockable.GetName();
		if (progress != null) progress.SetActive(false);
	}

	public void HideBar()
	{
		if (progress != null) progress.SetActive(false);
		if (t_progress != null) t_progress.text = string.Empty;
	}
}
