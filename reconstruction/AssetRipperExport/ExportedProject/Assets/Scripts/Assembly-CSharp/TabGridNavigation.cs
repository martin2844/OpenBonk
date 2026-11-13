using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabGridNavigation : MonoBehaviour
{
	public GameObject buttonsParent;

	public void Set(Button tabButton)
	{
		if (buttonsParent == null) return;
		var buttons = new List<Button>();
		foreach (Transform child in buttonsParent.transform)
		{
			var button = child.GetComponent<Button>();
			if (button != null && button.gameObject.activeSelf) buttons.Add(button);
		}
		if (buttons.Count == 0) return;
		var grid = buttonsParent.GetComponent<GridLayoutGroup>();
		int columns = grid != null && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
			? grid.constraintCount : ComputeRowLengthByPosition(buttons);
		columns = Mathf.Max(1, columns);
		int rows = Mathf.CeilToInt((float)buttons.Count / columns);
		for (int i = 0; i < buttons.Count; i++)
		{
			int row = i / columns, column = i % columns;
			var navigation = new Navigation { mode = Navigation.Mode.Explicit };
			if (i >= columns) navigation.selectOnUp = buttons[i - columns];
			if (i + columns < buttons.Count) navigation.selectOnDown = buttons[i + columns];
			else if (row == rows - 2) navigation.selectOnDown = buttons[buttons.Count - 1];
			if (column > 0) navigation.selectOnLeft = buttons[i - 1];
			if (column < columns - 1 && i + 1 < buttons.Count) navigation.selectOnRight = buttons[i + 1];
			if (row == 0) navigation.selectOnUp = tabButton;
			buttons[i].navigation = navigation;
		}
	}

	private int ComputeRowLengthByPosition(List<Button> buttons)
	{
		if (buttons.Count == 0) return 0;
		if (buttonsParent.transform is RectTransform rect) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
		float top = float.NegativeInfinity;
		foreach (var button in buttons) top = Mathf.Max(top, Mathf.Round(button.transform.localPosition.y));
		int count = 0;
		foreach (var button in buttons) if (Mathf.Approximately(Mathf.Round(button.transform.localPosition.y), top)) count++;
		return count;
	}
}
