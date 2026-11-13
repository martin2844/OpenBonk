using UnityEngine;
using Assets.Scripts.UI;
using Assets.Scripts.Utility;
using Assets.Scripts.Managers;
using UnityEngine.Localization;

public class PauseUi : MonoBehaviour
{
	public GameObject main;

	public GameObject options;

	public GameObject map;

	private GameObject current;

	public Window mainWindow;

	public Window mapWindow;

	public UpgradeInventoryUI inventory;

	private bool wasGamePaused;
	private int closedAtFrame = -1;
	public bool ClosedThisFrame => closedAtFrame == Time.frameCount;

	private void Awake()
	{
		GoToMain();
	}

	private void Update()
	{
		if (!gameObject.activeInHierarchy || (!main.activeSelf && !map.activeSelf)) return;
		if (AlwaysUi.Instance != null && AlwaysUi.Instance.dynamicWindows != null && AlwaysUi.Instance.dynamicWindows.HasWindows()) return;
		if (PlayerInput.IsConsoleOpen()) return;
		if (MyInputManager.GetButtonDown(MyInputManager.MapOverlay)) GoToMap();
		if (MyInputManager.GetButtonUp(MyInputManager.MapOverlay)) GoToMain();
	}

	public void Pause()
	{
		if (!CanPause() || gameObject.activeInHierarchy) return;
		gameObject.SetActive(true);
		inventory.Refresh();
		UiUtility.RebuildUi(inventory.transform.parent);
	}

	public void Resume()
	{
		gameObject.SetActive(false);
	}

	public bool CanPause()
	{
		return GameManager.Instance != null && !GameManager.Instance.cutscene && !gameObject.activeInHierarchy && GameManager.Instance.IsPlaying()
			&& UiManager.Instance != null && UiManager.Instance.encounterWindows != null && !UiManager.Instance.encounterWindows.HasEncounter();
	}

	public void Exit()
	{
		AlwaysUi.Instance.dynamicWindows.NewWindowPrompt(
			Localized("Main Menu", "MENU_BUTTON_EXIT_GAME", "Exit"),
			Localized("DynamicWindows", "EXIT_RUN", "Are you sure you want to end your run and go to the menu?"),
			() => TransitionUI.Instance.LoadMenu());
	}

	public void GoToMain()
	{
		GoToWindow(main);
	}

	public void GoToOptions()
	{
		GoToWindow(options);
	}

	public void GoToMap()
	{
		GoToWindow(map);
	}

	private void GoToWindow(GameObject window)
	{
		if (current != null) current.SetActive(false);
		current = window;
		current.SetActive(true);
	}

	public void Restart()
	{
		AlwaysUi.Instance.dynamicWindows.NewWindowPrompt(
			Localized("Game_PauseUi", "RESTART", "Restart"),
			Localized("DynamicWindows", "RESTART", "Do you want to restart?"),
			MapController.RestartRun);
	}

	public void Toggle()
	{
		if (main.activeInHierarchy) Resume();
		else Pause();
	}

	private void OnDisable()
	{
		closedAtFrame = Time.frameCount;
#if UNITY_ANDROID && !UNITY_EDITOR
		if (AndroidGameplayInputProbe.Active) Debug.Log($"Gameplay input probe pause disable: frame={Time.frameCount}, start={Input.GetKeyDown(KeyCode.JoystickButton10)}, escape={Input.GetKeyDown(KeyCode.Escape)}, cancel={MyInputManager.GetButtonDown(MyInputManager.UICancel)}");
#endif
		if (!wasGamePaused) MyTime.Unpause();
	}

	private void OnEnable()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (AndroidGameplayInputProbe.Active) Debug.Log($"Gameplay input probe pause enable: frame={Time.frameCount}, start={Input.GetKeyDown(KeyCode.JoystickButton10)}, escape={Input.GetKeyDown(KeyCode.Escape)}, cancel={MyInputManager.GetButtonDown(MyInputManager.UICancel)}");
#endif
		wasGamePaused = MyTime.paused;
		MyTime.Pause();
	}

	public bool IsPaused()
	{
		return gameObject.activeInHierarchy;
	}

	private static string Localized(string table, string key, string fallback)
	{
		try
		{
			string value = new LocalizedString(table, key).GetLocalizedString();
			return string.IsNullOrEmpty(value) ? fallback : value;
		}
		catch (System.Exception)
		{
			return fallback;
		}
	}
}
