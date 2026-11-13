using TMPro;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

public class ResetRunUi : MonoBehaviour
{
	public Transform content;

	public Image progressBar;

	public TextMeshProUGUI text;

	private float startedHoldingTime;

	private bool holding;

	private bool restarting;

	private float GetHoldTime()
	{
		return SaveManager.Instance.config.cfGameSettings.quick_reset_time;
	}

	private void Update()
	{
		var player = MyPlayer.Instance;
		var windows = UiManager.Instance != null ? UiManager.Instance.encounterWindows : null;
		if (player == null || player.inventory == null || PlayerInput.IsConsoleOpen() ||
			windows == null || windows.HasEncounter() || (MyTime.paused && !player.IsDead()) ||
			TransitionUI.Instance == null || TransitionUI.Instance.isTransitioning)
		{
			StopProgress();
			return;
		}
		if (restarting) return;
		if (holding) UpdateBar();
		var settings = SaveManager.Instance != null && SaveManager.Instance.config != null
			? SaveManager.Instance.config.cfGameSettings : null;
		if (settings == null || settings.quick_reset == 0) return;
		if (MyInputManager.GetButtonDown(MyInputManager.QuickReset)) StartProgress();
		if (!MyInputManager.GetButton(MyInputManager.QuickReset)) StopProgress();
	}

	private void StartProgress()
	{
		if (content != null) content.gameObject.SetActive(true);
		startedHoldingTime = Time.time;
		holding = true;
		UpdateBar();
		if (text != null)
		{
			text.gameObject.SetActive(true);
			text.CrossFadeAlpha(0f, 0f, true);
			text.CrossFadeAlpha(1f, .15f, true);
		}
	}

	private void StopProgress()
	{
		if (content != null) content.gameObject.SetActive(false);
		holding = false;
		if (text != null) text.gameObject.SetActive(false);
	}

	private void UpdateBar()
	{
		float progress = Mathf.Clamp01((Time.time - startedHoldingTime) / GetHoldTime());
		if (progressBar != null) progressBar.fillAmount = progress;
		if (progress >= 1f)
		{
			MapController.RestartRun();
			StopProgress();
		}
	}
}
