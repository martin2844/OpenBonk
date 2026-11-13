using System;
#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using Rewired;
using Rewired.Integration.UnityUI;
#endif
using UnityEngine;

public class MyInputManager : MonoBehaviour
{
#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
	public UserDataStore_FileCustom userDataStore;

	public RewiredStandaloneInputModule rewiredEventSystem;

	private static Player player;
#endif

	private static bool isSpaceNavigationActive;

	private static bool isHorizontalNavigationDuringChestActive;

#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
	public static Action<Controller> A_SetCurrentController;
#endif

	public static string Jump = "Jump";

	public static string JumpBhop = "Jump Bhop";

	public static string Slide = "Slide";

	public static string Aim = "Aim";

	public static string Interact = "Interact";

	public static string QuickReset = "QuickReset";

	public static string MapOverlay = "MapOverlay";

	public static string Wallrun = "Wallrun";

	public static string MoveHorizontal = "Move Horizontal";

	public static string MoveVertical = "Move Vertical";

	public static string LookHorizontal = "Look Horizontal";

	public static string LookVertical = "Look Vertical";

	public static string UIHorizontal = "UIHorizontal";

	public static string UIVertical = "UIVertical";

	public static string UISubmit = "UISubmit";

	public static string UICancel = "UICancel";

	public static string UIAbort = "UIAbort";

	public static string UIShoulderLeft = "UIShoulderLeft";

	public static string UIShoulderRight = "UIShoulderRight";

	private static double mouseLastActiveTime;

	private bool isVerticalEnabled;

	private float vertUiInputDeadzoneEnter;

	private float vertUiInputDeadzoneExit;

	private float stoppedInputAtTime;

	private float resumeInputDelay;

#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
	public static Controller currentController { get; private set; }
#endif

	private void Awake()
	{
	}

	private void OnDestroy()
	{
	}

	private void OnSavesLoaded()
	{
	}

	private void Update()
	{
	}

	public static bool IsUsingController()
	{
		return false;
	}

	private void CheckCurrentController()
	{
	}

#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
	public static Player GetPlayer()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		// The shipped Rewired integration has desktop backends only. Rewired's
		// core assembly can still load on Android, but it cannot provide the
		// game's player/controller there. Avoid touching its global input state.
		return null;
#else
		if (player == null && ReInput.isReady)
		{
			player = ReInput.players.GetPlayer(0);
		}
		return player;
#endif
	}

	public static Controller GetLastActiveController()
	{
		return null;
	}
#endif

	public static bool GetButton(string action)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		return AndroidNativeInput.GetButton(action);
#else
		Player rewiredPlayer = GetPlayer();
		return (rewiredPlayer != null && rewiredPlayer.GetButton(action)) || AndroidNativeInput.GetButton(action);
#endif
	}

	public static bool GetButtonDown(string action)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		return AndroidNativeInput.GetButtonDown(action);
#else
		Player rewiredPlayer = GetPlayer();
		return (rewiredPlayer != null && rewiredPlayer.GetButtonDown(action)) || AndroidNativeInput.GetButtonDown(action);
#endif
	}

	public static bool GetButtonUp(string action)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		return AndroidNativeInput.GetButtonUp(action);
#else
		Player rewiredPlayer = GetPlayer();
		return (rewiredPlayer != null && rewiredPlayer.GetButtonUp(action)) || AndroidNativeInput.GetButtonUp(action);
#endif
	}

	public static float GetAxis(string action)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		return AndroidNativeInput.GetAxis(action);
#else
		Player rewiredPlayer = GetPlayer();
		if (rewiredPlayer != null)
		{
			float rewiredAxis = rewiredPlayer.GetAxis(action);
			if (Mathf.Abs(rewiredAxis) > 0.001f)
			{
				return rewiredAxis;
			}
		}
		return AndroidNativeInput.GetAxis(action);
#endif
	}

#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
	private void SetController(Controller c)
	{
	}
#endif

	public static void RefreshSpaceNavigation()
	{
	}

	public static void RefreshHorizontalNavigationForChests(bool isChestWindowOpen)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		var controls = SaveManager.Instance != null && SaveManager.Instance.config != null ? SaveManager.Instance.config.cfControlSettings : null;
		if (controls != null) AndroidNativeInput.RefreshChestHorizontalNavigation(isChestWindowOpen, controls.horizontal_navigation == 1);
#endif
	}

	public static bool IsActionBound(string action)
	{
		if (string.Equals(action, Wallrun, System.StringComparison.OrdinalIgnoreCase))
			return AndroidControllerBindings.TryGet(action, out int button) && button >= 0;
		return false;
	}

	private void OnSettingUpdated(string settingName, object o1, object o2)
	{
	}

	private void OnWindowOpened(Window window)
	{
	}

	private void WindowUpdate()
	{
	}

	private void ChangeVerticalUI(bool active)
	{
	}
}
