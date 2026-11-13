#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using Rewired;
#endif
using Steamworks;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections.Generic;
#endif

public class PauseHandler : MonoBehaviour
{
	protected Callback<GameOverlayActivated_t> m_GameOverlayActivated;

	public PauseUi pauseUi;

#if UNITY_ANDROID && !UNITY_EDITOR
	private Dictionary<string, int> connectedJoysticks;
	private float nextJoystickPoll;
	private static Dictionary<string, int> ReadJoysticks()
	{
		var devices = new Dictionary<string, int>();
		foreach (string name in Input.GetJoystickNames())
		{
			if (string.IsNullOrEmpty(name)) continue;
			devices.TryGetValue(name, out int count);
			devices[name] = count + 1;
		}
		return devices;
	}

	private void Update()
	{
		if (Time.unscaledTime < nextJoystickPoll) return;
		nextJoystickPoll = Time.unscaledTime + .25f;
		var current = ReadJoysticks();
		bool disconnected = false;
		if (connectedJoysticks != null)
			foreach (var pair in connectedJoysticks)
				if (!current.TryGetValue(pair.Key, out int count) || count < pair.Value) { disconnected = true; break; }
		connectedJoysticks = current;
		if (disconnected && SaveManager.Instance?.config?.cfControlSettings != null &&
			SaveManager.Instance.config.cfControlSettings.pause_on_controller_disconnect != 0 && pauseUi != null)
			pauseUi.Pause();
	}

	private void OnApplicationPause(bool paused)
	{
		// Keep the run safe when Android backgrounds the activity. Pause()
		// already rejects cutscenes, encounters and an existing pause menu.
		if (paused && pauseUi != null) pauseUi.Pause();
	}
#endif

	private void Start()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		connectedJoysticks = ReadJoysticks();
#endif
	}

	private void OnDestroy()
	{
	}

#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
	private void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
	{
	}
#endif

	private void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
	{
	}
}
