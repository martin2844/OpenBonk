using System;
using UnityEngine;
using UnityEngine.UI;

public class ButtonNavigationSelectionOnly : MonoBehaviour
{
	private MyButtonTabs[] buttons;

	public int current = -1;

	public float cooldown;

	private float lastPressedTime;

	public Action<int> A_ButtonSelected;

	public int startButton;

	private void Start()
	{
		if (buttons != null) return;
		Init();
		ButtonPressed(startButton);
	}

	private void Init()
	{
		if (buttons != null) return;
		lastPressedTime = -cooldown;
		buttons = GetComponentsInChildren<MyButtonTabs>();
		for (int i = 0; i < buttons.Length; i++)
		{
			int index = i;
			buttons[i].GetButton().onClick.AddListener(() => ButtonPressed(index));
		}
	}

	private void ReInit()
	{
	}

	public void ButtonPressed(int index, bool force = false)
	{
		if (buttons == null) Init();
		if (!force && (!CanPress() || current == index)) return;
		if (current != -1) buttons[current].Deselect(buttons[index]);
		current = index;
		buttons[current].Select();
		lastPressedTime = Time.time;
		A_ButtonSelected?.Invoke(index);
	}

	private bool CanPress()
	{
		return Time.time - lastPressedTime >= cooldown;
	}

	public Button GetSelectedButton()
	{
		return buttons[current].GetButton();
	}

	public int GetNumButtons()
	{
		return buttons.Length;
	}
}
