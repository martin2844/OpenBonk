using System.Collections.Generic;
using UnityEngine;

public class HatSelectionPopupWindow : Window
{
	public HatSelection hatSelection;

	private List<MyButtonHat> hatButtons;

	public GameObject hatButtonPrefab;

	private HatData selectedHatBeforeOpenWindow;

	public MyButton b_hats;

	private HatData hatDataHover;

	public RectTransform windowRect;

	private new void Update()
	{
		base.Update();
		if (Input.GetKeyDown(KeyCode.Mouse0) && !RectTransformUtility.RectangleContainsScreenPoint(windowRect, Input.mousePosition)) CloseWindow();
	}

	public void FindStartButton(HatData hatData)
	{
		if (hatButtons == null) return;
		startBtn = hatButtons[0];
		foreach (MyButtonHat button in hatButtons) if (button.hatData == hatData) { startBtn = button; break; }
		foreach (MyButtonHat button in hatButtons) button.SetSelected(button.hatData == hatData);
	}

	public void RefreshHatButtons(List<HatData> availableHats, HatData selectedHat)
	{
		if (hatButtons == null) hatButtons = new List<MyButtonHat> { hatButtonPrefab.GetComponent<MyButtonHat>() };
		for (int i = 0; i < availableHats.Count; i++)
		{
			if (i >= hatButtons.Count) hatButtons.Add(Instantiate(hatButtonPrefab, hatButtonPrefab.transform.parent).GetComponent<MyButtonHat>());
			hatButtons[i].Set(availableHats[i]);
			bool selected = availableHats[i] == selectedHat;
			if (selected) startBtn = hatButtons[i];
			hatButtons[i].SetSelected(selected);
		}
		FindAllButtonsInWindow();
	}

	public void HoverButton(HatData hatData)
	{
		hatDataHover = hatData;
		hatSelection.HoverHat(hatData);
	}

	public void StopHoverButton(HatData hatData)
	{
		if (hatDataHover != hatData) return;
		hatDataHover = null;
		hatSelection.HoverHat(selectedHatBeforeOpenWindow);
	}

	public void ClickButton(HatData hatData)
	{
		selectedHatBeforeOpenWindow = hatData;
		hatSelection.SelectHat(hatData);
		gameObject.SetActive(false);
	}

	private void CloseWindow()
	{
		gameObject.SetActive(false);
	}

	private new void OnDisable()
	{
		base.OnDisable();
		hatSelection.HoverHat(selectedHatBeforeOpenWindow);
		Assets.Scripts.Managers.ButtonManager.ForceHoverButton(b_hats);
	}
}
