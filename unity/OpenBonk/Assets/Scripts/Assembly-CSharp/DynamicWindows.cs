using System;
using UnityEngine;
using UnityEngine.SceneManagement;
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
}
