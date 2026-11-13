using UnityEngine;

public class AlwaysUi : MonoBehaviour
{
	public UiTextPopup UiTextPopup;

	public SelectionArrow selectionArrow;

	public GameObject resolutionWindow;

	public GameObject languageWindow;

	public DynamicWindows dynamicWindows;

	public ConfigWarning configWarning;

	public TooltipNew tooltipNew;

	public static AlwaysUi Instance;

	private void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
	}

	public void OpenLanguageWindow()
	{
		if (languageWindow != null) languageWindow.SetActive(true);
	}

	private void OnGUI()
	{
	}

	private void OnApplicationFocus(bool hasFocus)
	{
	}

	private void OnApplicationPause()
	{
	}
}
