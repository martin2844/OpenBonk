using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
	public static class UiUtility
	{
		public static void RebuildUi(Transform root)
		{
			if (root == null) return;
			LayoutGroup[] groups = root.GetComponentsInChildren<LayoutGroup>();
			for (int i = groups.Length - 1; i >= 0; i--)
			{
				groups[i].CalculateLayoutInputHorizontal();
				groups[i].CalculateLayoutInputVertical();
				if (groups[i].transform is RectTransform rect) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
			}
			if (root is RectTransform rootRect) LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
		}
	}
}
