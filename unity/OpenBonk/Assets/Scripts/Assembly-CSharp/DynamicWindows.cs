using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using Assets.Scripts.UI.DynamicWindows;

public class DynamicWindows : MonoBehaviour
{
	public Transform contentParent;

	public Transform overlay;

	public GameObject dynamicWindowPrefab;

	public GameObject dynamicWindowPromptPrefab;

	private void Awake()
	{
		SceneManager.sceneLoaded += OnNewSceneLoaded;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnNewSceneLoaded;
	}

	private void Update()
	{
		bool show = contentParent.childCount > 1;
		if (overlay.gameObject.activeSelf != show) overlay.gameObject.SetActive(show);
	}

	public bool HasWindows()
	{
		return contentParent.childCount > 1;
	}

	private void OnNewSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		for (int i = contentParent.childCount - 1; i > 0; i--) Destroy(contentParent.GetChild(i).gameObject);
	}

	public void NewWindow(string header, string content)
	{
		Instantiate(dynamicWindowPrefab, contentParent).GetComponent<DWindow>().Set(header, content, "Okey");
	}

	public void NewWindowPrompt(string header, string content, Action A_Accept)
	{
		Instantiate(dynamicWindowPromptPrefab, contentParent).GetComponent<DWindowPrompt>().Set(header, content, A_Accept);
	}

	internal DWindowPrompt TryShowRunRetry(Action retry, Action endRun)
	{
		if (dynamicWindowPromptPrefab == null || contentParent == null) return null;
		var prompt = Instantiate(dynamicWindowPromptPrefab, contentParent).GetComponent<DWindowPrompt>();
		bool spanish = LocalizationSettings.SelectedLocale?.Identifier.Code.StartsWith("es", StringComparison.OrdinalIgnoreCase) == true;
		if (!prompt.SetChoices(spanish ? "Reintentar" : "Retry", spanish ? "Terminar partida" : "End run", endRun))
		{
			prompt.gameObject.SetActive(false);
			Destroy(prompt.gameObject);
			return null;
		}
		prompt.Set(spanish ? "Has caído" : "You died",
			spanish ? "Reviví con toda la vida y seguí desde donde caíste." : "Revive at full health and continue from where you fell.", retry);
		return prompt;
	}
}
