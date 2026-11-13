using System.Collections.Generic;
using Assets.Scripts.Game.Other;
using TMPro;
using UnityEngine;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Localization;

public class MapStatsWindow : MonoBehaviour
{
	public TextMeshProUGUI t_mapSpecs;

	public SelectionGroupToggleSingle tierSelection;

	public SelectionGroupToggleSingle challengeSelection;

	public SelectionGroupToggleSingleButtonTier[] tierButtons;

	public MapSelectionUi mapSelectionUi;

	public MyButton btnChallenges;

	public GameObject newChallengesIndicator;

	private Dictionary<int, float> tierSilverMultipliers;

	private void Start()
	{
		Refresh();
	}

	private void Awake()
	{
		if (tierSelection != null) tierSelection.A_ButtonSelected += OnInfoChanged;
		if (challengeSelection != null) challengeSelection.A_ButtonSelected += OnInfoChanged;
		MapSelectionUi.A_RunConfigChanged += OnRunConfigChanged;
		MapSelectionUi.A_MapSelected += OnMapSelected;
	}

	private void OnDestroy()
	{
		if (tierSelection != null) tierSelection.A_ButtonSelected -= OnInfoChanged;
		if (challengeSelection != null) challengeSelection.A_ButtonSelected -= OnInfoChanged;
		MapSelectionUi.A_RunConfigChanged -= OnRunConfigChanged;
		MapSelectionUi.A_MapSelected -= OnMapSelected;
	}

	private void OnInfoChanged(SelectionGroupToggleSingleButton btn)
	{
		Refresh();
	}

	private void OnRunConfigChanged(RunConfig runConfig)
	{
		Refresh();
	}

	private void OnMapSelected(SelectionGroupToggleSingleButton mapButton, MapData mapData)
	{
		Refresh();
	}

	public void RefreshTiers()
	{
		if (mapSelectionUi == null || mapSelectionUi.runConfig == null || mapSelectionUi.runConfig.mapData == null || tierButtons == null) return;
		var config = mapSelectionUi.runConfig;
		int highest = SaveManager.Instance != null && SaveManager.Instance.progression != null
			? SaveManager.Instance.progression.menuMeta.GetHighestCompletedTier(config.mapData.eMap) : -1;
		int visibleCount = config.mapData.eMap == Assets.Scripts._Data.MapsAndStages.EMap.Graveyard ? 1 : tierButtons.Length;
		for (int i = 0; i < tierButtons.Length; i++)
		{
			if (tierButtons[i] == null) continue;
			tierButtons[i].gameObject.SetActive(i < visibleCount);
			tierButtons[i].CanSelect(i <= highest + 1);
			tierButtons[i].SetCompleted(i <= highest, config);
		}
		if (tierSelection != null) tierSelection.FindButtons();
	}

	private void Refresh()
	{
		if (mapSelectionUi == null || mapSelectionUi.runConfig == null || mapSelectionUi.runConfig.mapData == null) return;
		RefreshTiers();
		var config = mapSelectionUi.runConfig;
		int stages = tierSelection != null ? tierSelection.GetSelectedIndex() + 1 : config.mapTierIndex + 1;
		if (SaveManager.Instance != null && SaveManager.Instance.progression != null && SaveManager.Instance.progression.menuMeta != null)
		{
			var meta = SaveManager.Instance.progression.menuMeta;
			bool hasChallenges = !(config.mapData is OpenBonkExternalMapData) || HasEnabledChallenges(config.stageData);
			if (btnChallenges != null) btnChallenges.SetInteractableButKeepSelectionOn(hasChallenges && meta.IsTierCompleted(config.mapData.eMap, config.mapTierIndex));
			if (newChallengesIndicator != null) newChallengesIndicator.SetActive(hasChallenges && meta.GetMapProgress(config.mapData.eMap).tierChallengeNotifications.Contains(config.mapTierIndex));
		}
		if (t_mapSpecs != null)
		{
			string stageText = LocalizationUtility.GetLocalizedString("ChallengesUi", "STAGES", new Dictionary<string, string> { ["numStages"] = stages.ToString() });
			t_mapSpecs.text = stageText + "\n<sprite name=silver> " + config.GetFormattedSilverMultiplier() + "x " + LocalizationUtility.GetStatName(EStat.SilverIncreaseMultiplier) + "\n";
			if (config.challenge != null) t_mapSpecs.text += "<color=orange>" + config.challenge.GetDisplayName() + "</color>";
		}
	}

	public static bool HasEnabledChallenges(StageData stage)
	{
		if (stage == null || stage.challenges == null) return false;
		foreach (var challenge in stage.challenges) if (challenge != null && challenge.isEnabled) return true;
		return false;
	}
}
