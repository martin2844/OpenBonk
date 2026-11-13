using System;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Managers
{
	public class ButtonManager
	{
		private static MyButton selectedButton2;

		public static Action<MyButton> A_ButtonHover;

		public static bool enabled = true;

		public static void Refresh()
		{
			ForceHoverButton(selectedButton2);
		}

		public static void SetFirstButton(MyButton button)
		{
			if (EventSystem.current != null && button != null) ForceHoverButton(button);
		}

		public static void ForceHoverButton(MyButton btn)
		{
			if (!enabled || btn == null || EventSystem.current == null || !btn.gameObject.activeInHierarchy) return;
			EventSystem.current.SetSelectedGameObject(btn.gameObject);
			StartedHoveringButton(btn);
		}

		public static void StartedHoveringButton(MyButton button)
		{
			if (selectedButton2 != null) selectedButton2.StopHover();
			selectedButton2 = button;
			if (button != null) button.StartHover();
			A_ButtonHover?.Invoke(button);
		}

		public static void SetNull()
		{
			if (selectedButton2 != null) selectedButton2.StopHover();
			selectedButton2 = null;
			if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
		}

		public static MyButton GetCurrentButton()
		{
			return selectedButton2;
		}
	}
}
