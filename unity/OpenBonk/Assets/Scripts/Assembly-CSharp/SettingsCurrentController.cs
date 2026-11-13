#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using Rewired;
#endif
using TMPro;
using UnityEngine;
using Assets.Scripts.UI.Localization;

public class SettingsCurrentController : MonoBehaviour
{
	public TextMeshProUGUI t_controller;

#if UNITY_ANDROID && !UNITY_EDITOR
	private float nextControllerCheck;
	private string displayedController;

	private void Update()
	{
		if (Time.unscaledTime < nextControllerCheck) return;
		nextControllerCheck = Time.unscaledTime + 0.25f;
		Refresh();
	}
#endif

	private void Awake()
	{
	}

	private void OnDestroy()
	{
	}

#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
	private void OnControllerChange(Controller c)
	{
	}
#endif

	private void OnEnable()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		displayedController = null;
		Refresh();
#endif
	}

	private void Refresh()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (t_controller == null) return;
		string controllerName = null;
		foreach (string name in Input.GetJoystickNames())
		{
			if (string.IsNullOrWhiteSpace(name)) continue;
			controllerName = name;
			break;
		}
		string display = controllerName ?? LocalizationUtility.GetLocalizedString("MainMenuOther", "NONE", "None");
		if (display == displayedController) return;
		t_controller.text = display;
		displayedController = display;
#endif
	}
}
