using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using Assets.Scripts._Data.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyButtonTabsQuest : MyButtonTabs
{
	public TextMeshProUGUI t_progress;

	public TextMeshProUGUI t_unlcaimed;

	public RawImage i_progressBar;

	public GameObject unclaimedUi;

	private EAchievementType achievementType;

	private int unclaimed;

	private new void Awake()
	{
		base.Awake();
		ProgressionSaveFile.A_AchievementClaimed += OnClaimed;
	}

	private void OnDestroy()
	{
		ProgressionSaveFile.A_AchievementClaimed -= OnClaimed;
	}

	public void Set(EAchievementType achievementType)
	{
		MyAchievements.GetAchievementTypeProgress(achievementType, out int completed, out int total, out unclaimed);
		this.achievementType = achievementType;
		t_progress.text = string.Format("{0} / {1}", completed, total);
		if (total > 0)
		{
			float progress = (float)completed / total;
			i_progressBar.transform.localScale = new Vector3(progress, 1f, 1f);
			i_progressBar.color = MyColorUtility.GetRedToGreenGradient(progress);
		}
		unclaimedUi.SetActive(unclaimed > 0);
		t_unlcaimed.text = unclaimed.ToString();
	}

	private void OnClaimed(MyAchievement achievement)
	{
		if (achievement == null || t_unlcaimed == null || unclaimedUi == null || achievement.achievementType != achievementType) return;
		unclaimed--;
		t_unlcaimed.text = unclaimed.ToString();
		if (unclaimed <= 0) unclaimedUi.SetActive(false);
	}
}
