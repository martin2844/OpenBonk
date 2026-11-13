using System;
using TMPro;
using UnityEngine;

public class SelectionGroupToggleSingleButtonChallenge : SelectionGroupToggleSingleButton
{
	public static Action<SelectionGroupToggleSingleButtonChallenge> A_ChallengeHovered;

	public TextMeshProUGUI t_name;

	public TextMeshProUGUI t_silver;

	public GameObject completedIcon;

	public GameObject completedOverlay;

	public bool isShowing { get; private set; }

	public ChallengeData challengeData { get; private set; }

	public void Set(ChallengeData challengeData)
	{
		this.challengeData = challengeData;
		if (challengeData != null && challengeData.CanShow()) SetVisible();
		else SetHidden();
	}

	private void SetVisible()
	{
		if (t_name != null) t_name.text = challengeData.GetDisplayName();
		if (t_silver != null) t_silver.text = "<sprite name=silver> " + challengeData.GetSilverMultiplier();
		bool completed = challengeData != null && SaveManager.Instance != null && SaveManager.Instance.progression != null && challengeData.IsUnlocked();
		if (completedIcon != null) completedIcon.SetActive(completed);
		if (completedOverlay != null) completedOverlay.SetActive(completed);
		if (unselectableOverlay != null) unselectableOverlay.SetActive(false);
		isShowing = true;
	}

	private void SetHidden()
	{
		if (t_name != null) t_name.text = "??";
		if (t_silver != null) t_silver.text = string.Empty;
		bool completed = challengeData != null && SaveManager.Instance != null && SaveManager.Instance.progression != null && challengeData.IsUnlocked();
		if (completedIcon != null) completedIcon.SetActive(completed);
		if (completedOverlay != null) completedOverlay.SetActive(completed);
		if (unselectableOverlay != null) unselectableOverlay.SetActive(true);
		isShowing = false;
	}

	public override void StartHover()
	{
		isHovering = true;
		A_ChallengeHovered?.Invoke(this);
	}

	public override void StopHover()
	{
		isHovering = false;
	}
}
