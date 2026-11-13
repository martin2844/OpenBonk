using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Utility;

public class QuestsFooter : MonoBehaviour
{
	public UnlockContainer achievementContainer;

	public TextMeshProUGUI t_description;

	public TextMeshProUGUI t_unlock;

	public TextMeshProUGUI t_reward;

	public GameObject checkMark;

	public RawImage i_progressBar;

	public TextMeshProUGUI t_progress;

	private void Awake()
	{
		SetEmpty();
		MyButtonQuest.A_Hover += OnAchievementHover;
	}

	private void OnDestroy()
	{
		MyButtonQuest.A_Hover -= OnAchievementHover;
	}

	private void SetEmpty()
	{
		if (achievementContainer != null) achievementContainer.SetEmpty();
		if (t_description != null) t_description.text = string.Empty;
		if (t_unlock != null) t_unlock.text = string.Empty;
		if (t_reward != null) t_reward.text = string.Empty;
	}

	private void OnAchievementHover(MyButtonQuest questButton)
	{
		if (questButton == null || questButton.achievement == null) return;
		var achievement = questButton.achievement;
		if (achievementContainer != null) achievementContainer.SetAchievement(achievement);
		if (t_description != null) t_description.text = achievement.GetUnlockRequirement();
		if (t_unlock != null)
		{
			t_unlock.text = achievement.GetUnlockedString();
			t_unlock.gameObject.SetActive(!string.IsNullOrEmpty(t_unlock.text));
		}
		if (t_reward != null) t_reward.text = achievement.GetRewardString();
		bool completed = achievement.IsCompleted();
		if (checkMark != null) checkMark.SetActive(completed);
		float ratio;
		if (achievement.IsTrackingStat())
		{
			float current = Mathf.Clamp(MyStats.GetStat(achievement.statName), 0f, achievement.targetValue);
			ratio = Mathf.Clamp01(current / achievement.targetValue);
			if (t_progress != null) t_progress.text = string.Format("{0:N0}/{1:N0}", current, achievement.targetValue);
		}
		else
		{
			ratio = completed || achievement.IsClaimed() ? 1f : 0f;
			if (t_progress != null) t_progress.text = ratio > 0f ? "1 / 1" : "0 / 1";
		}
		if (i_progressBar != null)
		{
			i_progressBar.transform.localScale = new Vector3(ratio, 1f, 1f);
			i_progressBar.color = MyColorUtility.GetRedToGreenGradient(ratio);
		}
	}
}
