using System.Collections.Generic;
using Assets.Scripts.Game.Other;
using Assets.Scripts._Data.MapsAndStages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;

public class MapStatsInfoUi : MonoBehaviour
{
	public TextMeshProUGUI t_mapName;

	public TextMeshProUGUI t_mapRuns;

	public TextMeshProUGUI t_tier;

	public TextMeshProUGUI t_highscore;

	public TextMeshProUGUI t_fastestTime;

	public RawImage characterIconPrefab;

	public RawImage mapIcon;

	private List<RawImage> characterIcons;

	private EMap currentMap;

	private int currentTier;

	private void Awake()
	{
		MapSelectionUi.A_RunConfigChanged += OnRunConfigChanged;
	}

	private void OnEnable()
	{
		// The Android menu can initialize its run config while this panel is inactive.
		var window = GetComponent<MapStatsWindow>();
		if (window != null && window.mapSelectionUi != null) SetConfig(window.mapSelectionUi.runConfig);
	}

	private void OnDestroy()
	{
		MapSelectionUi.A_RunConfigChanged -= OnRunConfigChanged;
	}

	private void OnRunConfigChanged(RunConfig runConfig)
	{
		SetConfig(runConfig);
	}

	public void SetConfig(RunConfig runConfig)
	{
		if (runConfig == null || runConfig.mapData == null || SaveManager.Instance == null || SaveManager.Instance.progression == null) return;
		MapData map = runConfig.mapData;
		int tier = runConfig.mapTierIndex;
		mapIcon.texture = map.GetIcon();
		t_mapName.text = map.GetName() ?? string.Empty;
		t_tier.text = string.Format("{0} {1}", LocalizationUtility.GetLocalizedString("Other", "TIER", "Tier", false), tier + 1);
		t_tier.color = Color.Lerp(MyColorUtility.GetTierColor(tier), Color.white, 0.25f);
		var progress = SaveManager.Instance.progression.menuMeta.GetMapProgress(map.eMap);
		t_mapRuns.text = string.Format("{0} {1}", progress.GetNumTierRuns(tier), LocalizationUtility.GetLocalizedString("Other", "RUNS", "Runs", false));
		t_highscore.text = progress.GetTierHighscoreString(tier);
		t_fastestTime.text = progress.GetTierFastestTimeString(tier);
		if (characterIcons == null) characterIcons = new List<RawImage> { characterIconPrefab };
		foreach (RawImage icon in characterIcons) icon.gameObject.SetActive(false);
		var characters = progress.GetTierCompletionCharacters(tier);
		int visible = 0;
		for (int i = 0; i < characters.Count; i++)
		{
			// A disabled or removed character mod may still occur in saved history.
			if (DataManager.Instance == null || !DataManager.Instance.TryGetCharacterData(characters[i], out CharacterData character) || character == null) continue;
			if (visible >= characterIcons.Count) characterIcons.Add(Instantiate(characterIconPrefab, characterIconPrefab.transform.parent));
			RawImage icon = characterIcons[visible++];
			icon.gameObject.SetActive(true);
			icon.texture = character.GetIcon();
		}
		currentMap = map.eMap;
		currentTier = tier;
	}
}
