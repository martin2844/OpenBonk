using System;
using System.Collections.Generic;
using Assets.Scripts.Managers;
using UnityEngine.EventSystems;

public static class WindowManager
{
	public static List<Window> openWindows = new List<Window>();

	public static Window activeWindow;

	public static Action<Window> A_WindowOpened;

	public static void Update()
	{
	}

	public static void CloseAll()
	{
		foreach (Window window in new List<Window>(openWindows))
			if (window != null && window.gameObject != null) window.Close();
		openWindows.Clear();
		RefreshCursor();
	}

	public static void WindowOpened(Window newWindow)
	{
		if (newWindow == null || openWindows.Contains(newWindow)) return;
		if (activeWindow != null) activeWindow.UnfocusWindow();
		activeWindow = newWindow;
		openWindows.Add(newWindow);
		newWindow.FocusWindow();
		RefreshCursor();
		A_WindowOpened?.Invoke(newWindow);
	}

	public static void WindowClosed(Window closedWindow)
	{
		if (closedWindow == null || !openWindows.Remove(closedWindow)) return;
		if (activeWindow != closedWindow) return;
		activeWindow = openWindows.Count == 0 ? null : openWindows[openWindows.Count - 1];
		if (activeWindow != null) activeWindow.FocusWindow();
		else
		{
			if (AlwaysUi.Instance != null && AlwaysUi.Instance.selectionArrow != null) AlwaysUi.Instance.selectionArrow.Hide();
			if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
			ButtonManager.SetNull();
		}
		RefreshCursor();
	}

	public static void RefreshCursor()
	{
	}

	public static bool HasOpenWindow()
	{
		return openWindows.Count > 0;
	}

	public static int GetNumOpenWindows()
	{
		return openWindows.Count;
	}

	public static void OnGUI()
	{
	}
}
