using System;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyButtonQuest : MyButton
{
	public TextMeshProUGUI t_description;

	public TextMeshProUGUI t_reward;

	public TextMeshProUGUI t_progress;

	public TextMeshProUGUI t_claimAmount;

	public RawImage i_icon;

	public RawImage i_progressBar;

	public RawImage i_outline;

	public RawImage i_background;

	public MaskableGraphic i_colorBg;

	public MaskableGraphic i_colorOutline;

	public MaskableGraphic i_completeCheck;

	public GameObject claimOverlay;

	public GameObject incompleteOverlay;

	public GameObject rewardContainer;

	public GameObject hoveringOverlay;

	public Color bgColorDefault;

	public Color bgColorCompleted;

	public static Action<MyButtonQuest> A_Select;

	public static Action<MyButtonQuest> A_Hover;

	public MyAchievement achievement { get; private set; }

	public void Set(MyAchievement achievement)
	{
		this.achievement = achievement;
		bool completed = achievement.IsCompleted();
		bool claimed = achievement.IsClaimed();
		t_description.text = achievement.GetUnlockRequirement();
		t_reward.text = string.Format("<size=110%><sprite name=silver></size> {0}", achievement.GetSilverReward());
		i_colorOutline.color = MyColorUtility.DifficultyToColor(achievement.difficulty);
		i_completeCheck.color = MyColorUtility.DifficultyToColor(achievement.difficulty);
		i_colorBg.color = completed ? bgColorCompleted : bgColorDefault;
		float progress;
		if (achievement.IsTrackingStat())
		{
			float current = Mathf.Clamp(Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.GetStat(achievement.statName), 0f, achievement.targetValue);
			t_progress.text = string.Format("{0:N0}/{1:N0}", current, achievement.targetValue);
			progress = current / achievement.targetValue;
		}
		else
		{
			progress = completed ? 1f : 0f;
			t_progress.text = completed ? "1 / 1" : "0 / 1";
		}
		i_progressBar.transform.localScale = new Vector3(progress, 1f, 1f);
		i_progressBar.color = MyColorUtility.GetRedToGreenGradient(progress);
		i_icon.texture = achievement.GetIcon();
		i_icon.color = completed ? Color.white : Color.black;
		claimOverlay.SetActive(completed && !claimed);
		rewardContainer.SetActive(!completed && !claimed);
		i_completeCheck.gameObject.SetActive(completed || claimed);
		t_claimAmount.text = "<size=115%><sprite name=silver></size> " + achievement.GetSilverReward();
	}

	public override void StartHover()
	{
		hoveringOverlay.SetActive(true);
		isHovering = true;
		A_Hover?.Invoke(this);
	}

	public override void StopHover()
	{
		hoveringOverlay.SetActive(false);
		isHovering = false;
	}

	protected override void OnClick()
	{
		if (selectedAtTime > Time.time - 0.1f) return;
		TryClaimButton();
		A_Select?.Invoke(this);
	}

	public void TryClaimButton()
	{
		if (achievement.IsCompleted() && !achievement.IsClaimed()) Claim();
	}

	private void Claim()
	{
		SaveManager.Instance.progression.ClaimAchievement(achievement);
		claimOverlay.SetActive(false);
		if (MenuParticles.Instance != null) MenuParticles.Instance.CoinEffect(t_claimAmount.transform.position);
	}
}
