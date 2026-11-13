using Assets.Scripts.Game.Other;
using UnityEngine;

public class SelectionGroupToggleSingleButtonTier : SelectionGroupToggleSingleButton
{
	public GameObject completedIcon;

	public GameObject alert;

	private RunConfig runConfig;

	private void Start()
	{
	}

	public void SetCompleted(bool completed, RunConfig runConfig)
	{
		this.runConfig = runConfig;
		if (completedIcon != null) completedIcon.SetActive(completed);
		if (alert != null && runConfig != null && runConfig.mapData != null && SaveManager.Instance != null && SaveManager.Instance.progression != null)
			alert.SetActive(SaveManager.Instance.progression.menuMeta.GetMapProgress(runConfig.mapData.eMap).tierNotifications.Contains(transform.GetSiblingIndex()));
	}

	protected override void OnClick()
	{
		if (alert == null || !alert.activeSelf || runConfig == null || runConfig.mapData == null || SaveManager.Instance == null) return;
		SaveManager.Instance.progression.menuMeta.GetMapProgress(runConfig.mapData.eMap).tierNotifications.Remove(transform.GetSiblingIndex());
		alert.SetActive(false);
	}
}
