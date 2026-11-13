using Assets.Scripts.UI.HUD;
using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Debug;
using Assets.Scripts.Managers;
using UnityEngine;

public class UiManager : MonoBehaviour
{
	public DeathScreen deathScreen;

	public ScoreUi scoreUi;

	public EncounterWindows encounterWindows;

	public MapTitle mapTile;

	public PauseUi pause;

	public ShrineLogs shrineLogs;

	public ObjectiveArrow objectiveArrow;

	public AlertUi alertUi;

	public ServerFeed feed;

	public ObjectiveUi objective;

	public ColorFilterUI colorFilterUi;

	public GameObject hud;

	public CinematicBars cinematicBars;

	private bool inited;

	public static UiManager Instance;

	public CanvasGroup hudGroup;

	private void Awake()
	{
		TryInit();
	}

	private void Start()
	{
		RefreshHud();
		UiUtility.RebuildUi(transform);
	}

	public void TryInit()
	{
		if (inited) return;
		inited = true;
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
		hud.SetActive(false);
		if (MapController.IsFirstStage()) ShrineLogs.Reset();
		SpawnPlayerPortal.A_PortalOpen += OnPortalOpen;
		SpawnPlayerPortal.A_PortalClosed += OnPortalClose;
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
	}

	private void Update()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (AndroidGameplayInputProbe.Active && Input.GetKeyDown(KeyCode.JoystickButton10))
			Debug.Log($"Gameplay input probe UI handler: console={(DebugConsole.Instance != null && DebugConsole.Instance.IsActive())}, abort={MyInputManager.GetButtonDown(MyInputManager.UIAbort)}, before={pause.gameObject.activeInHierarchy}, main={pause.main.activeInHierarchy}");
#endif
		if (DebugConsole.Instance != null && DebugConsole.Instance.IsActive()) return;
#if UNITY_ANDROID && !UNITY_EDITOR
		// Android may deliver Start as both joystick button 10 and Return.
		// The UI module can submit Resume before this Update sees UIAbort.
		if (pause.ClosedThisFrame && MyInputManager.GetButtonDown(MyInputManager.UIAbort)) return;
#endif
		if ((pause.gameObject.activeInHierarchy && MyInputManager.GetButtonDown(MyInputManager.UICancel)) ||
			MyInputManager.GetButtonDown(MyInputManager.UIAbort))
		{
			pause.Toggle();
#if UNITY_ANDROID && !UNITY_EDITOR
			if (AndroidGameplayInputProbe.Active) Debug.Log($"Gameplay input probe UI toggled: after={pause.gameObject.activeInHierarchy}, paused={Assets.Scripts.Utility.MyTime.paused}");
#endif
		}
	}

	private void RefreshHud()
	{
		if (SaveManager.Instance == null || SaveManager.Instance.config == null || SaveManager.Instance.config.cfGameSettings == null) return;
		hudGroup.alpha = SaveManager.Instance.config.cfGameSettings.show_hud == 1 ? 1f : 0f;
#if UNITY_ANDROID && !UNITY_EDITOR
		if (MapGenerationController.androidHudProbe) hudGroup.alpha = 1f;
#endif
		Debug.Log($"HUD visibility: enabled={SaveManager.Instance.config.cfGameSettings.show_hud}, alpha={hudGroup.alpha}");
	}

	private void OnDestroy()
	{
		SpawnPlayerPortal.A_PortalOpen -= OnPortalOpen;
		SpawnPlayerPortal.A_PortalClosed -= OnPortalClose;
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
	}

	private void OnSettingUpdated(string settingName, object oldValue, object newValue)
	{
		if (settingName == "show_hud") RefreshHud();
	}

	private void OnPortalOpen()
	{
		hud.SetActive(false);
	}

	private void OnPortalClose()
	{
		hud.SetActive(true);
	}
}
