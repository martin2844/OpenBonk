using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TabVerticalNavigation : MonoBehaviour
{
	public GameObject buttonsParent;

	public void Set(Button tabButton)
	{
		var buttons = new List<Button>();
		foreach (Transform child in buttonsParent.transform)
		{
			Button button = child.GetComponent<Button>();
			if (button != null && button.gameObject.activeSelf) buttons.Add(button);
		}
		for (int i = 0; i < buttons.Count; i++)
		{
			Navigation navigation = new Navigation { mode = Navigation.Mode.Explicit };
			navigation.selectOnUp = i == 0 ? tabButton : buttons[i - 1];
			if (i + 1 < buttons.Count) navigation.selectOnDown = buttons[i + 1];
			buttons[i].navigation = navigation;
		}
	}
}
