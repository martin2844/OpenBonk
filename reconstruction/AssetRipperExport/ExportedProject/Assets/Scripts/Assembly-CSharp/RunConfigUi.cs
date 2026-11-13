using TMPro;
using System.Collections.Generic;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

public class RunConfigUi : MonoBehaviour
{
	public TextMeshProUGUI t_mapName;

	public TextMeshProUGUI t_mapTier;

	public TextMeshProUGUI t_silverMultiplier;

	public TextMeshProUGUI t_challengeName;

	public TextMeshProUGUI t_challengeDescription;

	public Image mapOutline;

	public GameObject challenge;

	public GameObject challengeFailedOverlay;

	private void OnEnable()
	{
	}

	private void Start()
	{
		var config = MapController.runConfig;
		if (config == null || config.mapData == null) return;
		Color tierColor = MyColorUtility.GetTierColor(config.mapTierIndex);
		if (mapOutline != null && mapOutline.material != null) mapOutline.material.SetColor("_Color", tierColor);
		if (t_mapName != null) { t_mapName.color = tierColor; t_mapName.text = config.mapData.GetName(); }
		if (t_mapTier != null)
		{
			t_mapTier.color = tierColor;
			t_mapTier.text = LocalizationUtility.GetLocalizedString("Other", "TIER_SMART",
				new Dictionary<string, string> { { "tier", (config.mapTierIndex + 1).ToString() } });
		}
		if (t_silverMultiplier != null) t_silverMultiplier.text = string.Format("<sprite name=silver> <size=90%>{0}x", config.GetSilverMultiplier());
		if (config.challenge != null)
		{
			if (challenge != null) challenge.SetActive(true);
			if (t_challengeName != null) t_challengeName.text = config.challenge.GetDisplayName() ?? "";
			if (t_challengeDescription != null) t_challengeDescription.text = config.challenge.GetUnlockDescription();
		}
		if (transform.parent != null) UiUtility.RebuildUi(transform.parent);
	}

	private void Refresh()
	{
	}
}
