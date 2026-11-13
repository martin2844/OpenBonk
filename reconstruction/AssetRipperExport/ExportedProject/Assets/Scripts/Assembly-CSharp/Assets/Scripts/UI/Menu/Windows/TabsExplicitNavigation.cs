using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Assets.Scripts.UI.Menu.Windows
{
	public class TabsExplicitNavigation : MonoBehaviour
	{
		private enum NavDirection
		{
			Up = 0,
			Down = 1,
			Left = 2,
			Right = 3
		}

		public Selectable topButton;

		public Selectable[] bottomButtons;

		public Transform content;

		public bool manualRefresh;

		private void Start()
		{
			if (!manualRefresh) Refresh();
		}

		public void Refresh()
		{
			var buttons = new List<Selectable>();
			for (int i = 0; i < content.childCount; i++)
			{
				Transform child = content.GetChild(i);
				if (!child.gameObject.activeSelf) continue;
				Selectable button = child.GetComponent<Selectable>();
				if (button != null) buttons.Add(button);
			}
			if (buttons.Count == 0) return;
			if (topButton != null) SetNavigation(topButton, buttons[0], NavDirection.Down);
			for (int i = 0; i < buttons.Count - 1; i++) SetNavigation(buttons[i], buttons[i + 1], NavDirection.Down);
			Navigation lastNavigation = buttons[buttons.Count - 1].navigation;
			lastNavigation.selectOnDown = null;
			buttons[buttons.Count - 1].navigation = lastNavigation;
			if (bottomButtons != null)
				foreach (Selectable bottom in bottomButtons)
					if (bottom.gameObject.activeInHierarchy) SetNavigation(buttons[buttons.Count - 1], bottom, NavDirection.Down);
		}

		private void SetNavigation(Selectable parent, Selectable toButton, NavDirection direction)
		{
			Navigation parentNavigation = parent.navigation;
			Navigation targetNavigation = toButton.navigation;
			switch (direction)
			{
				case NavDirection.Up: parentNavigation.selectOnUp = toButton; targetNavigation.selectOnDown = parent; break;
				case NavDirection.Down: parentNavigation.selectOnDown = toButton; targetNavigation.selectOnUp = parent; break;
				case NavDirection.Left: parentNavigation.selectOnLeft = toButton; break;
				case NavDirection.Right: parentNavigation.selectOnRight = toButton; break;
			}
			parent.navigation = parentNavigation;
			toButton.navigation = targetNavigation;
		}
	}
}
