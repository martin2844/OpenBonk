using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CAnimateNewButtons_003Ed__15 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public MainMenu _003C_003E4__this;

		public List<GameObject> objects;

		private List<GameObject>.Enumerator _003C_003E7__wrap1;

		private GameObject _003Co_003E5__3;

		private float _003CscaleTime_003E5__4;

		private float _003Ctimer_003E5__5;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CAnimateNewButtons_003Ed__15(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	public static Action A_MenuOpened;

	public MenuCamera menuCamera;

	public GameObject btnUnlocks;

	public GameObject btnQuests;

	public GameObject btnShop;

	public GameObject leaderboards;

	public GameObject quickQuests;

	public MyButton btnPlay;

	private bool isAnimating;

	public GameObject blockRaycastOverlay;

	public GameObject newButtonParticles;

	public MapSelectionUi mapSelectionUi;

	public GameObject tabMenu;

	public GameObject tabCharacters;

	public GameObject tabMaps;

	public GameObject tabShop;

	public GameObject tabUnlocks;

	public GameObject tabLogs;

	public GameObject settings;

	public GameObject credits;

	public GameObject quests;

	public GameObject leaderboardsFull;

	private GameObject currentTab;

	private void Awake()
	{
		currentTab = tabMenu;
		Assets.Scripts.Utility.MyTime.Unpause();
		SaveManager.A_SavesLoaded += RefreshButtons;
	}

	private void OnDestroy()
	{
		SaveManager.A_SavesLoaded -= RefreshButtons;
		if (isAnimating)
		{
			Assets.Scripts.Managers.ButtonManager.enabled = true;
			if (blockRaycastOverlay != null) blockRaycastOverlay.SetActive(false);
		}
	}

	private void Start()
	{
		if (SaveManager.loaded && SaveManager.Instance != null)
		{
			SaveManager.Instance.SaveProgression();
			SaveManager.Instance.SaveStats();
		}
		RefreshButtons();
		if (mapSelectionUi != null)
		{
			mapSelectionUi.Init();
#if UNITY_ANDROID && !UNITY_EDITOR
			using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
			using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
			{
				if (intent.Call<bool>("getBooleanExtra", "megabonk_desert_probe", false))
				{
					MapData desert = DataManager.Instance.maps.Find(m => m.eMap == Assets.Scripts._Data.MapsAndStages.EMap.Desert);
					if (desert != null) { mapSelectionUi.runConfig.mapData = desert; mapSelectionUi.runConfig.stageData = desert.stages[0]; mapSelectionUi.runConfig.mapTierIndex = 0; }
				}
			}

			using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
			using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
				if (intent.Call<bool>("getBooleanExtra", "megabonk_selection_probe", false)) StartCoroutine(ProbeSelection());
				else if (intent.Call<bool>("getBooleanExtra", "megabonk_controller_probe", false)) StartCoroutine(ProbeController());
				else if (intent.Call<bool>("getBooleanExtra", "megabonk_focus_recovery_probe", false)) StartCoroutine(ProbeFocusRecovery());
				else if (intent.Call<bool>("getBooleanExtra", "megabonk_chest_navigation_probe", false)) StartCoroutine(ProbeChestNavigation());
				else if (intent.Call<bool>("getBooleanExtra", "megabonk_rebind_probe", false)) AndroidRebindProbe.Begin();
				else if (intent.Call<bool>("getBooleanExtra", "megabonk_challenge_menu_probe", false)) StartCoroutine(ProbeChallengeMenu());
				else if (intent.Call<bool>("getBooleanExtra", "megabonk_challenge_reload_probe", false))
				{
					var challenge = DataManager.Instance.GetAchievement("a_forest1_turbo");
					float completed = Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.GetStat(Assets.Scripts.Saves___Serialization.Progression.Stats.EMyStat.challengesCompleted.ToString());
					UnityEngine.Debug.Log($"Challenge completion reload: challengeFound={challenge != null}, unlocked={(challenge != null && challenge.IsUnlocked())}, completedCount={completed}");
				}
				else if (intent.Call<bool>("getBooleanExtra", "megabonk_rebind_reload_probe", false))
					UnityEngine.Debug.Log($"Rebind probe reload: jump={AndroidControllerBindings.DisplayForAction("Jump")}, interact={AndroidControllerBindings.DisplayForAction("Interact")}, aim={AndroidControllerBindings.DisplayForAction("Aim")}");
#endif
		}
		MyInputManager.RefreshHorizontalNavigationForChests(false);
		A_MenuOpened?.Invoke();
	}

#if UNITY_ANDROID && !UNITY_EDITOR
	private IEnumerator ProbeChallengeMenu()
	{
		// Isolated test only: unlock this tier in memory so the normal menu gate can be exercised.
		yield return null;
		if (SaveManager.Instance == null || SaveManager.Instance.progression == null || mapSelectionUi.runConfig.mapData == null)
		{
			UnityEngine.Debug.Log("Challenge menu probe: missing progression or map");
			yield break;
		}
		var config = mapSelectionUi.runConfig;
		SaveManager.Instance.progression.menuMeta.SetTierCompletion(config.mapData.eMap, config.mapTierIndex);
		MapSelectionUi.A_RunConfigChanged?.Invoke(config);
		UnityEngine.Debug.Log("Challenge menu probe: tier unlocked in memory");
		float deadline = Time.realtimeSinceStartup + 120f;
		while (Time.realtimeSinceStartup < deadline && !mapSelectionUi.challengesUi.gameObject.activeInHierarchy) yield return null;
		if (!mapSelectionUi.challengesUi.gameObject.activeInHierarchy) { UnityEngine.Debug.Log("Challenge menu probe: window not opened"); yield break; }
		yield return null;
		var rows = mapSelectionUi.challengesUi.challengesSelectionGroup.GetComponentsInChildren<SelectionGroupToggleSingleButtonChallenge>(false);
		UnityEngine.Debug.Log($"Challenge menu probe: open rows={rows.Length}, header={mapSelectionUi.challengesUi.t_header.text}, name={mapSelectionUi.challengesUi.t_name.text}");
		ChallengeData last = null;
		deadline = Time.realtimeSinceStartup + 60f;
		while (Time.realtimeSinceStartup < deadline && mapSelectionUi.challengesUi.gameObject.activeInHierarchy)
		{
			if (config.challenge != last)
			{
				last = config.challenge;
				UnityEngine.Debug.Log("Challenge menu probe: selected=" + (last != null ? last.internalName : "<none>"));
				var selectedRow = mapSelectionUi.challengesUi.startBtn as SelectionGroupToggleSingleButtonChallenge;
				var confirm = mapSelectionUi.challengesUi.btn_confirm != null ? mapSelectionUi.challengesUi.btn_confirm.GetButton() : null;
				UnityEngine.Debug.Log($"Challenge menu probe: selectedRow={(selectedRow != null && selectedRow.challengeData != null ? selectedRow.challengeData.internalName : "<none>")}, startMatches={(last != null && selectedRow != null && selectedRow.challengeData == last)}, confirmLeftMatches={(selectedRow != null && confirm != null && confirm.navigation.selectOnLeft == selectedRow.GetButton())}");
			}
			yield return null;
		}
	}

	private IEnumerator ProbeChestNavigation()
	{
		yield return null;
		var events = UnityEngine.EventSystems.EventSystem.current;
		bool navigation = events != null && events.sendNavigationEvents;
		if (events != null) events.sendNavigationEvents = false;
		AndroidNativeInput.RefreshChestHorizontalNavigation(true, false);
		UnityEngine.Debug.Log("Chest navigation probe: ready");
		float deadline = Time.realtimeSinceStartup + 45f;
		bool loggedA = false, loggedPad = false;
		while (Time.realtimeSinceStartup < deadline && (!loggedA || !loggedPad))
		{
			float axis = MyInputManager.GetAxis(MyInputManager.UIHorizontal);
			if (!loggedA && Input.GetKey(KeyCode.A)) { loggedA = true; UnityEngine.Debug.Log("Chest navigation probe: A axis=" + axis); }
			if (!loggedPad && Mathf.Abs(axis) > 0.3f && !Input.GetKey(KeyCode.A)) { loggedPad = true; UnityEngine.Debug.Log("Chest navigation probe: direction axis=" + axis); }
			yield return null;
		}
		AndroidNativeInput.RefreshChestHorizontalNavigation(false, false);
		if (events != null) events.sendNavigationEvents = navigation;
		UnityEngine.Debug.Log($"Chest navigation probe: done A={loggedA} direction={loggedPad}");
	}

	private IEnumerator ProbeFocusRecovery()
	{
		float deadline = Time.realtimeSinceStartup + 60f;
		while (Time.realtimeSinceStartup < deadline && (WindowManager.activeWindow == null || WindowManager.activeWindow.startBtn == null || !WindowManager.activeWindow.startBtn.gameObject.activeInHierarchy)) yield return null;
		var window = WindowManager.activeWindow;
		var events = UnityEngine.EventSystems.EventSystem.current;
		if (window == null || window.startBtn == null || events == null) { UnityEngine.Debug.Log("Focus recovery probe: missing window or event system"); yield break; }
		bool navigation = events.sendNavigationEvents;
		events.sendNavigationEvents = false;
		yield return new WaitForSecondsRealtime(1f);
		Assets.Scripts.Managers.ButtonManager.SetNull();
		UnityEngine.Debug.Log("Focus recovery probe: cleared selection in " + window.name);
		yield return new WaitForSecondsRealtime(1f);
		UnityEngine.Debug.Log("Focus recovery probe: waiting for navigation, selected=" + (Assets.Scripts.Managers.ButtonManager.GetCurrentButton() != null));
		deadline = Time.realtimeSinceStartup + 30f;
		while (Time.realtimeSinceStartup < deadline && Assets.Scripts.Managers.ButtonManager.GetCurrentButton() == null) yield return null;
		var selected = Assets.Scripts.Managers.ButtonManager.GetCurrentButton();
		UnityEngine.Debug.Log("Focus recovery probe: result=" + (selected != null ? selected.name : "<none>"));
		events.sendNavigationEvents = navigation;
	}

	private IEnumerator ProbeController()
	{
		string last = "<unset>";
		float end = Time.realtimeSinceStartup + 60f;
		float nextAxisLog = 0f;
		var eventSystem = UnityEngine.EventSystems.EventSystem.current;
		bool oldNavigation = eventSystem != null && eventSystem.sendNavigationEvents;
		if (eventSystem != null) eventSystem.sendNavigationEvents = false;
		UnityEngine.Debug.Log("Controller probe devices: " + string.Join(", ", Input.GetJoystickNames()));
		while (Time.realtimeSinceStartup < end)
		{
			var events = UnityEngine.EventSystems.EventSystem.current;
			string selected = events != null && events.currentSelectedGameObject != null ? events.currentSelectedGameObject.name : "<none>";
			if (selected != last) { UnityEngine.Debug.Log($"Controller probe focus: {selected}, windows={WindowManager.GetNumOpenWindows()}"); last = selected; }
			if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.DownArrow))
				UnityEngine.Debug.Log($"Controller probe key: a={Input.GetKeyDown(KeyCode.JoystickButton0)}, b={Input.GetKeyDown(KeyCode.JoystickButton1)}, enter={Input.GetKeyDown(KeyCode.Return)}, down={Input.GetKeyDown(KeyCode.DownArrow)}, submit={MyInputManager.GetButtonDown(MyInputManager.UISubmit)}, cancel={MyInputManager.GetButtonDown(MyInputManager.UICancel)}");
			for (int i = 0; i < 20; i++)
				if (Input.GetKeyDown(KeyCode.JoystickButton0 + i)) UnityEngine.Debug.Log("Controller probe button: " + i);
			if (MyInputManager.GetButtonDown(MyInputManager.UIAbort)) UnityEngine.Debug.Log("Controller probe action: UIAbort");
			if (MyInputManager.GetButtonDown(MyInputManager.Aim)) UnityEngine.Debug.Log("Controller probe action: Aim");
			if (MyInputManager.GetButtonUp(MyInputManager.Aim)) UnityEngine.Debug.Log("Controller probe action: Aim released");
			if (MyInputManager.GetButtonDown(MyInputManager.MapOverlay)) UnityEngine.Debug.Log("Controller probe action: MapOverlay");
			if (MyInputManager.GetButtonUp(MyInputManager.MapOverlay)) UnityEngine.Debug.Log("Controller probe action: MapOverlay released");
			if (Time.realtimeSinceStartup >= nextAxisLog)
			{
				nextAxisLog = Time.realtimeSinceStartup + .25f;
				float lookX = MyInputManager.GetAxis(MyInputManager.LookHorizontal);
				float lookY = MyInputManager.GetAxis(MyInputManager.LookVertical);
				float moveX = MyInputManager.GetAxis(MyInputManager.MoveHorizontal), moveY = MyInputManager.GetAxis(MyInputManager.MoveVertical);
				float uiX = MyInputManager.GetAxis(MyInputManager.UIHorizontal), uiY = MyInputManager.GetAxis(MyInputManager.UIVertical);
				if (Mathf.Abs(moveX) > .3f || Mathf.Abs(moveY) > .3f || Mathf.Abs(uiX) > .3f || Mathf.Abs(uiY) > .3f)
					UnityEngine.Debug.Log($"Controller probe directions: move={moveX:F2},{moveY:F2} ui={uiX:F2},{uiY:F2}");
				if (Mathf.Abs(lookX) > .3f || Mathf.Abs(lookY) > .3f) UnityEngine.Debug.Log($"Controller probe look: x={lookX:F2}, y={lookY:F2}");
				for (int joy = 1; joy <= 2; joy++)
					for (int axis = 1; axis <= 12; axis++)
					{
						float value = Input.GetAxisRaw($"Joy{joy}Axis{axis}");
						if (Mathf.Abs(value) > .35f) UnityEngine.Debug.Log($"Controller probe axis: joystick={joy}, axis={axis}, value={value:F2}");
					}
			}
			yield return null;
		}
		if (eventSystem != null) eventSystem.sendNavigationEvents = oldNavigation;
	}

	private IEnumerator ProbeSelection()
	{
		yield return new WaitForSecondsRealtime(2f);
		var ui = mapSelectionUi;
		var save = SaveManager.Instance.progression;
		var oldMeta = save.menuMeta;
		var temporary = new Assets.Scripts.Saves___Serialization.Progression.MenuMeta();
		try
		{
			save.menuMeta = temporary;
			ui.gameObject.SetActive(true);
			ui.Init();
			var tier0 = ui.tierSelectionGroup.GetButton(0);
			var tier1 = ui.tierSelectionGroup.GetButton(1);
			var tier2 = ui.tierSelectionGroup.GetButton(2);
			if (tier0 == null || tier1 == null || tier2 == null) { UnityEngine.Debug.Log("Selection probe: missing tier buttons"); yield break; }
			bool lockedBefore = !tier1.canSelect;
			tier1.GetButton().onClick.Invoke();
			int afterLocked = ui.runConfig.mapTierIndex;
			temporary.SetTierCompletion(ui.runConfig.mapData.eMap, 0);
			ui.mapStatsWindow.RefreshTiers();
			bool unlockedAfter = tier1.canSelect;
			tier1.GetButton().onClick.Invoke();
			int selected = ui.runConfig.mapTierIndex;
			bool correctStage = ui.runConfig.stageData == ui.runConfig.mapData.stages[1];
			int mapCount = 0;
			while (ui.mapSelectionGroup.GetButton(mapCount) != null) mapCount++;
			UnityEngine.Debug.Log($"Selection probe: maps={mapCount}, map={ui.runConfig.mapData.name}, lockedBefore={lockedBefore}, lockedClickTier={afterLocked}, unlockedAfter={unlockedAfter}, selectedTier={selected}, stage={ui.runConfig.stageData.name}, stageMatches={correctStage}, tier2Locked={!tier2.canSelect}, savedTier={temporary.GetLastSelectedTier(ui.runConfig.mapData.eMap)}");
		}
		finally
		{
			save.menuMeta = oldMeta;
			ui.Init();
		}
	}
#endif

	private void RefreshButtons()
	{
		var save = SaveManager.Instance;
		if (!SaveManager.loaded || save?.progression?.menuMeta == null || save.config?.preferences == null) return;
		var meta = save.progression.menuMeta;
		var prefs = save.config.preferences;
		var newlyUnlocked = new List<GameObject>();
		UpdateButton(btnUnlocks, meta.HasMenuUnlocks(), ref prefs.hasShownUnlocks, newlyUnlocked);
		UpdateButton(btnQuests, meta.HasMenuQuests(), ref prefs.hasShownQuests, newlyUnlocked);
		UpdateButton(btnShop, meta.HasMenuShop(), ref prefs.hasShownShop, newlyUnlocked);
		UpdateButton(leaderboards, meta.HasLeaderboards(), ref prefs.hasShownLeaderboards, newlyUnlocked);
		UpdateButton(quickQuests, meta.HasQuickQuests(), ref prefs.hasShownQuickQuests, newlyUnlocked);
		if (newlyUnlocked.Count > 0 && !isAnimating) StartCoroutine(AnimateNewButtons(newlyUnlocked));
	}

	private static void UpdateButton(GameObject button, bool unlocked, ref bool shown, List<GameObject> newlyUnlocked)
	{
		if (button == null) return;
		button.SetActive(unlocked);
		if (unlocked && !shown) { shown = true; newlyUnlocked.Add(button); }
	}

	private IEnumerator AnimateNewButtons(List<GameObject> objects)
	{
		Assets.Scripts.Managers.ButtonManager.enabled = false;
		if (blockRaycastOverlay != null) blockRaycastOverlay.SetActive(true);
		isAnimating = true;
		foreach (var button in objects)
		{
			if (button == null) continue;
			button.SetActive(true);
			button.transform.localScale = Vector3.zero;
		}
		Assets.Scripts.UI.UiUtility.RebuildUi(transform);
		yield return new WaitForSecondsRealtime(1f);
		foreach (var button in objects)
		{
			if (button == null) continue;
			button.SetActive(true);
			if (newButtonParticles != null)
			{
				var particles = Instantiate(newButtonParticles, newButtonParticles.transform.parent);
				particles.transform.position = button.transform.position;
				particles.SetActive(true);
				particles.AddComponent<DestroyObject>().time = .8f;
			}
			if (AudioManager.Instance != null && AudioManager.Instance.newMenuButton != null)
				AudioManager.Instance.PlayNewMenuButton();
			float timer = 0f;
			while (timer < .25f)
			{
				timer += Time.deltaTime;
				float scale = UnityEngine.UIElements.Experimental.Easing.InOutQuad(Mathf.Clamp01(timer / .25f));
				button.transform.localScale = Vector3.one * scale;
				yield return null;
			}
			yield return new WaitForSecondsRealtime(.6f);
		}
		Assets.Scripts.Managers.ButtonManager.enabled = true;
		if (blockRaycastOverlay != null) blockRaycastOverlay.SetActive(false);
		isAnimating = false;
		Assets.Scripts.Managers.ButtonManager.ForceHoverButton(btnPlay);
	}

	public void GoToMenu()
	{
		SetWindow(tabMenu);
		if (menuCamera != null) menuCamera.GoToMain();
		RefreshButtons();
	}

	public void GoToCharacterSelection()
	{
		if (tabCharacters == null)
		{
			return;
		}

		tabCharacters.SetActive(true);
		if (currentTab != null)
		{
			currentTab.SetActive(false);
		}
		currentTab = tabCharacters;
		if (menuCamera != null)
		{
			menuCamera.GoToCharacters();
		}
		RefreshButtons();
	}

	public void GoToMapSelection()
	{
		if (tabMaps == null)
		{
			return;
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		// Persist the confirmed character before Android can reclaim the process.
		if (SaveManager.Instance != null) SaveManager.Instance.SaveConfig();
#endif
		tabMaps.SetActive(true);
		if (currentTab != null)
		{
			currentTab.SetActive(false);
		}
		currentTab = tabMaps;
	}

	public void GoToShop()
	{
		SetWindow(tabShop);
	}

	public void GoToCredits()
	{
		SetWindow(credits);
	}

	public void GoToUnlocks()
	{
		SetWindow(tabUnlocks);
	}

	public void GoToQuests()
	{
		SetWindow(quests);
	}

	public void GoToLogs()
	{
		SetWindow(tabLogs);
	}

	public void GoToSettings()
	{
		SetWindow(settings);
	}

	public void GoToLeaderboards()
	{
		SetWindow(leaderboardsFull);
	}

	public void SetWindow(GameObject tabWindow)
	{
		if (tabWindow == null) return;
		tabWindow.SetActive(true);
		if (currentTab != null) currentTab.SetActive(false);
		currentTab = tabWindow;
	}

	public void ExitGame()
	{
		if (AlwaysUi.Instance == null || AlwaysUi.Instance.dynamicWindows == null) return;
		AlwaysUi.Instance.dynamicWindows.NewWindowPrompt(
			Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Main Menu", "MENU_BUTTON_EXIT_GAME"),
			Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("DynamicWindows", "EXIT_GAME"),
			() => Application.Quit());
	}
}
