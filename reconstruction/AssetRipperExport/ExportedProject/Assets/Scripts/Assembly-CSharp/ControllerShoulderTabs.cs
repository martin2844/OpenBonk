using UnityEngine;
using Assets.Scripts.Managers;

public class ControllerShoulderTabs : MonoBehaviour
{
	public ButtonNavigationSelectionOnly tabs;

	public Window parentWindow;

	private void Start()
	{
		if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
	}

	private void Update()
	{
		if (parentWindow == null || WindowManager.activeWindow != parentWindow) return;
		if (KeyListener.Instance != null && KeyListener.Instance.IsListening()) return;
		if (MyInputManager.GetButtonDown(MyInputManager.UIShoulderLeft)) ShoulderNav(-1);
		else if (MyInputManager.GetButtonDown(MyInputManager.UIShoulderRight)) ShoulderNav(1);
	}

	private void ShoulderNav(int dir)
	{
		if (tabs == null) return;
		int current = tabs.current;
		if (current < 0 || (dir < 0 && current <= 0) || (dir > 0 && current >= tabs.GetNumButtons() - 1) || dir == 0) return;
		tabs.ButtonPressed(current + dir);
		var selected = tabs.GetSelectedButton();
		if (selected != null) ButtonManager.ForceHoverButton(selected.GetComponent<MyButton>());
	}
}
