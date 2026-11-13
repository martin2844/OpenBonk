using UnityEngine;
using Assets.Scripts.UI.InGame.Levelup;

public class InventoryOverlay : MonoBehaviour
{
	public GameObject[] windows;

	public UpgradeInventoryUI inventoryUi;

	private bool active;

	private void Awake()
	{
		EncounterWindows.A_WindowOpened += OnWindowOpened;
		EncounterWindows.A_WindowClosed += OnWindowClosed;
	}

	private void OnDestroy()
	{
		EncounterWindows.A_WindowOpened -= OnWindowOpened;
		EncounterWindows.A_WindowClosed -= OnWindowClosed;
	}

	private void OnWindowOpened()
	{
		if (windows != null)
			foreach (GameObject window in windows)
			{
				if (window != null) window.SetActive(true);
				active = true;
			}
		if (inventoryUi != null) inventoryUi.Refresh();
	}

	private void OnWindowClosed()
	{
		if (windows == null) return;
		foreach (GameObject window in windows)
		{
			if (window != null) window.SetActive(false);
			active = false;
		}
	}

	private void Update()
	{
		if (UiManager.Instance != null && active && UiManager.Instance.encounterWindows != null &&
			!UiManager.Instance.encounterWindows.HasEncounter()) OnWindowClosed();
	}
}
