using System.Collections.Generic;
using Assets.Scripts.Settings___Saves.SaveFiles;
using UnityEngine;

public class ResolutionWindow : Window
{
	public GameObject resolutionButtonPrefab;

	private List<MyButtonResolution> resolutionButtons = new List<MyButtonResolution>();

	private new void OnEnable()
	{
		Refresh();
		savedBtn = resolutionButtons.Count > 0 && resolutionButtons[0].gameObject.activeSelf ? resolutionButtons[0] : null;
		base.OnEnable();
	}

	private void Refresh()
	{
		int current = SaveManager.Instance != null && SaveManager.Instance.config != null && SaveManager.Instance.config.cfVideoSettings != null
			? SaveManager.Instance.config.cfVideoSettings.resolution : -1;
		var modes = ConfigSettingsUtility.GetMyResolutions();
#if UNITY_ANDROID && !UNITY_EDITOR
		current = ConfigSettingsUtility.GetCurrentResolutionIndex(modes, current, Screen.width, Screen.height);
#endif
		RefreshChoices(modes, current);
	}

	private void RefreshChoices(Resolution[] resolutions, int current)
	{
		if (resolutionButtonPrefab == null || resolutions == null) return;
		if (resolutionButtons == null) resolutionButtons = new List<MyButtonResolution>();
		if (resolutionButtons.Count == 0)
		{
			var template = resolutionButtonPrefab.GetComponent<MyButtonResolution>();
			if (template == null) return;
			resolutionButtons.Add(template);
		}
		foreach (var button in resolutionButtons) if (button != null) button.gameObject.SetActive(false);
		for (int i = 0; i < resolutions.Length; i++)
		{
			if (i >= resolutionButtons.Count)
			{
				var clone = Instantiate(resolutionButtonPrefab, resolutionButtonPrefab.transform.parent);
				var button = clone.GetComponent<MyButtonResolution>();
				if (button == null) { Destroy(clone); break; }
				resolutionButtons.Add(button);
			}
			var choice = resolutionButtons[i];
			if (choice == null) continue;
			choice.gameObject.SetActive(true);
			choice.SetResolution(resolutions[i], i == current, i);
		}
	}
}
