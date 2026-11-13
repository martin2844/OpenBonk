using UnityEngine;
using UnityEngine.UI;

public class StartBlackOverlay : MonoBehaviour
{
	public RawImage overlay;

	private bool hasSubscribed;

	private void Awake()
	{
		if (SaveManager.Instance?.config?.cfGameSettings != null && SaveManager.Instance.config.cfGameSettings.super_quick_resets == 1)
		{
			gameObject.SetActive(false);
			if (overlay != null) overlay.enabled = false;
		}
	}

	private void Start()
	{
		if (!MapGenerationController.isGenerating || overlay == null) return;
		overlay.enabled = true;
		hasSubscribed = true;
		MapGenerationController.A_GenerationComplete += OnGenerationComplete;
	}

	private void OnGenerationComplete()
	{
		Invoke(nameof(HideBlackScreen), 0f);
	}

	private void HideBlackScreen()
	{
		if (overlay != null) overlay.enabled = false;
	}

	private void OnDestroy()
	{
		if (hasSubscribed) MapGenerationController.A_GenerationComplete -= OnGenerationComplete;
	}
}
