using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectionGroupToggleSingle : MonoBehaviour
{
	public Action<SelectionGroupToggleSingleButton> A_ButtonSelected;

	public int startIndex;

	public bool canSelectMultiple;

	public bool canSelectNothing;

	public bool selectDefaultOnAwake;

	private SelectionGroupToggleSingleButton lastButton;

	private List<SelectionGroupToggleSingleButton> buttons;

	private HashSet<SelectionGroupToggleSingleButton> registeredButtons;

	public List<SelectionGroupToggleSingleButton> selectedButtons { get; private set; }

	private void Awake()
	{
		if (selectedButtons == null) selectedButtons = new List<SelectionGroupToggleSingleButton>();
		FindButtons();
		if (!canSelectNothing && selectDefaultOnAwake && buttons.Count > 0 && lastButton == null)
			ForceSelect(Mathf.Clamp(startIndex, 0, buttons.Count - 1));
	}

	private void OnTransformChildrenChanged()
	{
		FindButtons();
	}

	public void FindButtons()
	{
		if (registeredButtons == null) registeredButtons = new HashSet<SelectionGroupToggleSingleButton>();
		if (selectedButtons == null) selectedButtons = new List<SelectionGroupToggleSingleButton>();
		buttons = new List<SelectionGroupToggleSingleButton>(GetComponentsInChildren<SelectionGroupToggleSingleButton>(true));
		foreach (SelectionGroupToggleSingleButton item in buttons)
		{
			if (!registeredButtons.Add(item)) continue;
			var current = item;
			var button = current.GetButton();
			if (button != null) button.onClick.AddListener(() => OnButtonSelect(current));
			current.Deselect();
		}
	}

	private void SelectButton(SelectionGroupToggleSingleButton btn)
	{
		if (btn == null) return;
		btn.Select();
		if (!selectedButtons.Contains(btn)) selectedButtons.Add(btn);
		lastButton = btn;
	}

	private void DeselectButton(SelectionGroupToggleSingleButton btn)
	{
		if (btn == null) return;
		btn.Deselect();
		selectedButtons.Remove(btn);
	}

	public void SetNone()
	{
		if (selectedButtons == null) return;
		foreach (var btn in new List<SelectionGroupToggleSingleButton>(selectedButtons)) DeselectButton(btn);
		lastButton = null;
	}

	private void OnButtonSelect(SelectionGroupToggleSingleButton newBtn)
	{
		if (newBtn == null || !newBtn.canSelect) return;
		if (canSelectMultiple)
		{
			if (!newBtn.isSelected) SelectButton(newBtn);
			else if (canSelectNothing || selectedButtons.Count >= 2) DeselectButton(newBtn);
		}
		else if (!newBtn.isSelected)
		{
			DeselectButton(lastButton);
			SelectButton(newBtn);
		}
		else if (canSelectNothing) DeselectButton(newBtn);
		A_ButtonSelected?.Invoke(newBtn);
	}

	public SelectionGroupToggleSingleButton GetButton(int index)
	{
		return buttons != null && index >= 0 && index < buttons.Count ? buttons[index] : null;
	}

	public void ForceSelect(int index)
	{
		if (buttons == null) FindButtons();
		SelectionGroupToggleSingleButton btn = GetButton(index);
		if (btn != null) OnButtonSelect(btn);
	}

	public int GetSelectedIndex()
	{
		int index = buttons == null ? -1 : buttons.IndexOf(lastButton);
		return index < 0 ? 0 : index;
	}
}
