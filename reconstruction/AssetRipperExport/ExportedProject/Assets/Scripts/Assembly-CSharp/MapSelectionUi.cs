using System;
using Assets.Scripts.Managers;
using Assets.Scripts.Game.Other;
using UnityEngine;
using UnityEngine.UI;

public class MapSelectionUi : Window
{
	public SelectionGroupToggleSingle mapSelectionGroup;

	public SelectionGroupToggleSingle tierSelectionGroup;

	public ChallengesUi challengesUi;

	private bool firstSelection;

	private SelectionGroupToggleSingleButton selectedBtn;

	private SelectionGroupToggleSingleButton mapBtn;

	public MapStatsWindow mapStatsWindow;

	public MapSelectionWindow mapButtonsWindow;

	public RunConfig runConfig;

	public MyButton btnConfirm;

	public GameObject jukebox;

	public static Action<RunConfig> A_RunConfigChanged;

	public static Action<SelectionGroupToggleSingleButton, MapData> A_MapSelected;

	public MapData defaultMapData;

	private bool isSelectingMap;

	private bool newMapSelected;

	private bool isStartingRun;

	public static bool isTestFight;

	public static Action A_MapSelectionEnabled;

	private new void Awake()
	{
		base.Awake();
		BindSelectionGroups();
	}

	private void BindSelectionGroups()
	{
		if (mapSelectionGroup != null) { mapSelectionGroup.A_ButtonSelected -= OnMapSelected; mapSelectionGroup.A_ButtonSelected += OnMapSelected; }
		if (tierSelectionGroup != null) { tierSelectionGroup.A_ButtonSelected -= OnTierSelected; tierSelectionGroup.A_ButtonSelected += OnTierSelected; }
	}

	private new void OnDestroy()
	{
		base.OnDestroy();
		if (mapSelectionGroup != null) mapSelectionGroup.A_ButtonSelected -= OnMapSelected;
		if (tierSelectionGroup != null) tierSelectionGroup.A_ButtonSelected -= OnTierSelected;
	}

	public void Init()
	{
		if (runConfig == null) runConfig = new RunConfig();
		if (runConfig.mapData == null) runConfig.mapData = defaultMapData;
		BindSelectionGroups();
		if (mapButtonsWindow != null) mapButtonsWindow.InitButtons();
		if (mapSelectionGroup != null)
		{
			mapSelectionGroup.FindButtons();
			for (int i = 0; ; i++)
			{
				var candidate = mapSelectionGroup.GetButton(i);
				if (candidate == null) break;
				var entry = candidate.GetComponent<MapEntry>();
				if (entry != null && entry.mapData == runConfig.mapData) { mapSelectionGroup.ForceSelect(i); break; }
			}
		}
		if (tierSelectionGroup != null) tierSelectionGroup.FindButtons();
		if (mapStatsWindow != null) mapStatsWindow.RefreshTiers();
		if (runConfig.mapData != null)
		{
			int selectedTier = SaveManager.Instance != null && SaveManager.Instance.progression != null
				? SaveManager.Instance.progression.menuMeta.GetLastSelectedTier(runConfig.mapData.eMap) : 0;
			int highest = SaveManager.Instance != null && SaveManager.Instance.progression != null
				? SaveManager.Instance.progression.menuMeta.GetHighestCompletedTier(runConfig.mapData.eMap) : -1;
			selectedTier = Mathf.Clamp(selectedTier, 0, Mathf.Min(highest + 1, runConfig.mapData.stages.Length - 1));
			if (tierSelectionGroup != null) tierSelectionGroup.ForceSelect(selectedTier);
			ApplyTier(selectedTier);
		}
		LinkMapAndTierNavigation();
		A_RunConfigChanged?.Invoke(runConfig);
	}

	private void OnMapSelected(SelectionGroupToggleSingleButton btn)
	{
		MapEntry entry = btn == null ? null : btn.GetComponent<MapEntry>();
		if (entry == null || entry.mapData == null || !btn.canSelect) return;
		bool sameMap = mapBtn == btn;
		isSelectingMap = true;
		if (runConfig.mapData != entry.mapData && challengesUi != null) challengesUi.SetNone();
		mapBtn = btn;
		runConfig.mapData = entry.mapData;
		if (mapStatsWindow != null) mapStatsWindow.RefreshTiers();
		int selectedTier = SaveManager.Instance != null && SaveManager.Instance.progression != null
			? SaveManager.Instance.progression.menuMeta.GetLastSelectedTier(entry.mapData.eMap) : 0;
		int highest = SaveManager.Instance != null && SaveManager.Instance.progression != null
			? SaveManager.Instance.progression.menuMeta.GetHighestCompletedTier(entry.mapData.eMap) : -1;
		selectedTier = Mathf.Clamp(selectedTier, 0, Mathf.Min(highest + 1, entry.mapData.stages.Length - 1));
		if (tierSelectionGroup != null)
		{
			if (sameMap) ButtonManager.ForceHoverButton(tierSelectionGroup.GetButton(selectedTier));
			tierSelectionGroup.ForceSelect(selectedTier);
		}
		isSelectingMap = false;
		if (!sameMap) newMapSelected = true;
		ApplyTier(selectedTier);
		LinkMapAndTierNavigation();
		A_MapSelected?.Invoke(btn, entry.mapData);
		A_RunConfigChanged?.Invoke(runConfig);
	}

	private void OnTierSelected(SelectionGroupToggleSingleButton btn)
	{
		if (btn == null || !btn.canSelect || tierSelectionGroup == null || runConfig == null || runConfig.mapData == null) return;
		selectedBtn = btn;
		int previousTier = runConfig.mapTierIndex;
		int tier = tierSelectionGroup.GetSelectedIndex();
		if (runConfig.mapTierIndex != tier && challengesUi != null) challengesUi.SetNone();
		ApplyTier(tier);
		LinkMapAndTierNavigation();
		if (tier == previousTier && !newMapSelected && !isSelectingMap) ButtonManager.ForceHoverButton(btnConfirm);
		newMapSelected = false;
		A_RunConfigChanged?.Invoke(runConfig);
	}

	private void LinkMapAndTierNavigation()
	{
		if (mapBtn == null || mapSelectionGroup == null || tierSelectionGroup == null) return;
		Button selectedMap = mapBtn.GetButton();
		var selectedTier = tierSelectionGroup.GetButton(tierSelectionGroup.GetSelectedIndex());
		Button tierButton = selectedTier != null ? selectedTier.GetButton() : null;
		if (selectedMap == null || tierButton == null) return;
		for (int i = 0; ; i++)
		{
			var entry = mapSelectionGroup.GetButton(i);
			if (entry == null) break;
			Button button = entry.GetButton();
			if (button == null) continue;
			Navigation navigation = button.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			navigation.selectOnRight = tierButton;
			button.navigation = navigation;
		}
		for (int i = 0; ; i++)
		{
			var entry = tierSelectionGroup.GetButton(i);
			if (entry == null) break;
			Button button = entry.GetButton();
			if (button == null) continue;
			Navigation navigation = button.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			navigation.selectOnLeft = selectedMap;
			if (i == 0) navigation.selectOnUp = selectedMap;
			button.navigation = navigation;
		}
	}

	private void ApplyTier(int tier)
	{
		if (runConfig.mapData == null || runConfig.mapData.stages == null || runConfig.mapData.stages.Length == 0) return;
		runConfig.mapTierIndex = Mathf.Clamp(tier, 0, runConfig.mapData.stages.Length - 1);
		runConfig.stageData = runConfig.mapData.stages[runConfig.mapTierIndex];
		if (SaveManager.Instance != null && SaveManager.Instance.progression != null)
			SaveManager.Instance.progression.menuMeta.SetTier(runConfig.mapData.eMap, runConfig.mapTierIndex);
	}

	public void SetChallenge(ChallengeData challengeData)
	{
		runConfig.challenge = challengeData;
		A_RunConfigChanged?.Invoke(runConfig);
	}

	public void StartMap()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
		using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
		using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
		{
			if (intent.Call<bool>("getBooleanExtra", "megabonk_graveyard_probe", false) && runConfig != null)
			{
				var graveyard = DataManager.Instance.maps.Find(m => m != null && m.eMap == Assets.Scripts._Data.MapsAndStages.EMap.Graveyard);
				if (graveyard != null && graveyard.stages != null && graveyard.stages.Length > 0)
				{
					runConfig.mapData = graveyard; runConfig.stageData = graveyard.stages[0]; runConfig.mapTierIndex = 0;
					Debug.Log("Graveyard probe: selected " + graveyard.name);
				}
			}
			if (intent.Call<bool>("getBooleanExtra", "megabonk_crypt_limit_probe", false))
			{
				var challenge = DataManager.Instance.GetAchievement("a_graveyard1_crypt 1") as ChallengeData;
				if (challenge != null && runConfig != null) { runConfig.challenge = challenge; Debug.Log("Crypt limit probe: selected " + challenge.internalName); }
			}
		}
#endif
		if (isStartingRun)
		{
			return;
		}

		isTestFight = false;
		if (runConfig == null)
		{
			Debug.LogError("Cannot start a run because the map selection has no RunConfig.");
			return;
		}

		isStartingRun = true;
		MapController.StartNewMap(runConfig);
	}

	public void StartBossFightTest()
	{
	}

	private new void OnDisable()
	{
		base.OnDisable();
	}

	private new void OnEnable()
	{
		base.OnEnable();
	}
}
