using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Managers;

public class Window : MonoBehaviour
{
	protected MyButton savedBtn;

	public MyButton startBtn;

	public bool alwaysUseStartBtn;

	public float openDelay;

	public List<MyButton> allButtons;

	public HashSet<GameObject> allButtonsHashed;

	private bool isFocused;

	public static Action A_WindowOpenedFirstTime;

	private float lastHadButtonTime;

	protected void Awake()
	{
		ButtonManager.A_ButtonHover += OnButtonHover;
	}

	protected void OnDestroy()
	{
		ButtonManager.A_ButtonHover -= OnButtonHover;
	}

	public void FindAllButtonsInWindow()
	{
		allButtons = new List<MyButton>(GetComponentsInChildren<MyButton>(true));
		allButtonsHashed = new HashSet<GameObject>();
		foreach (MyButton button in allButtons) if (button != null) allButtonsHashed.Add(button.gameObject);
	}

	protected void Start()
	{
		FindAllButtonsInWindow();
		A_WindowOpenedFirstTime?.Invoke();
	}

	protected void OnEnable()
	{
		WindowManager.WindowOpened(this);
	}

	protected void OnDisable()
	{
		WindowManager.WindowClosed(this);
		CancelInvoke(nameof(DelayedButtonFocus));
	}

	public void Close()
	{
		if (gameObject != null) gameObject.SetActive(false);
	}

	private void OnButtonHover(MyButton btn)
	{
		if (btn != null && allButtonsHashed != null && allButtonsHashed.Contains(btn.gameObject)) savedBtn = btn;
	}

	public void FocusWindow()
	{
		isFocused = true;
		FindAllButtonsInWindow();
		foreach (MyButton button in allButtons) if (button != null) button.SetFocus(true);
		CancelInvoke(nameof(DelayedButtonFocus));
		Invoke(nameof(DelayedButtonFocus), Mathf.Max(0f, openDelay));
	}

	private void DelayedButtonFocus()
	{
		MyButton target = savedBtn != null && !alwaysUseStartBtn ? savedBtn : startBtn;
#if UNITY_ANDROID && !UNITY_EDITOR
		if (target == null || !target.gameObject.activeInHierarchy || target.GetButton() == null || !target.GetButton().IsInteractable())
		{
			target = null;
			foreach (MyButton candidate in GetComponentsInChildren<MyButton>())
			{
				if (candidate.GetButton() != null && candidate.GetButton().IsInteractable()) { target = candidate; break; }
			}
		}
		if (target == null && UnityEngine.EventSystems.EventSystem.current != null)
		{
			foreach (UnityEngine.UI.Selectable candidate in GetComponentsInChildren<UnityEngine.UI.Selectable>())
			{
				if (candidate.IsInteractable()) { UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(candidate.gameObject); break; }
			}
		}
#endif
		ButtonManager.SetFirstButton(target);
		ButtonManager.ForceHoverButton(target);
	}

	public void UnfocusWindow()
	{
		isFocused = false;
		if (allButtons == null) FindAllButtonsInWindow();
		foreach (MyButton button in allButtons) if (button != null) button.SetFocus(false);
	}

	protected void Update()
	{
#if UNITY_ANDROID
		// Inventory slots are regular Selectables rather than MyButtons. While one
		// has focus, do not let recovery move selection back to the menu buttons.
		var eventSystem = UnityEngine.EventSystems.EventSystem.current;
		var selectedObject = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
		if (isFocused && selectedObject != null && selectedObject.transform.IsChildOf(transform) &&
			selectedObject.GetComponent<InventoryItemPrefabUI>() != null) return;
#endif
		if (ButtonManager.GetCurrentButton() != null)
		{
			lastHadButtonTime = Time.time;
			return;
		}
		if (!isFocused || Time.time - lastHadButtonTime < 0.5f) return;
		if (Mathf.Abs(MyInputManager.GetAxis(MyInputManager.UIHorizontal)) <= 0.1f &&
			Mathf.Abs(MyInputManager.GetAxis(MyInputManager.UIVertical)) <= 0.1f &&
			!MyInputManager.GetButtonDown(MyInputManager.UISubmit) &&
			!MyInputManager.GetButtonDown(MyInputManager.UICancel)) return;
		RecoverButtonFocus();
	}

	private void RecoverButtonFocus()
	{
		MyButton selected = ButtonManager.GetCurrentButton();
		if (selected != null && allButtonsHashed != null && allButtonsHashed.Contains(selected.gameObject)) return;
		if (startBtn != null && startBtn.gameObject.activeInHierarchy)
		{
			ButtonManager.ForceHoverButton(startBtn);
			return;
		}
		if (allButtons == null) return;
		foreach (MyButton button in allButtons)
			if (button != null && button.gameObject.activeInHierarchy) ButtonManager.ForceHoverButton(button);
	}
}
