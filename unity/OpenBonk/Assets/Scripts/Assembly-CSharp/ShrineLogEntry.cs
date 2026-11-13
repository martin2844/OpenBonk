using TMPro;
using UnityEngine;

public class ShrineLogEntry : MonoBehaviour
{
	public TextMeshProUGUI text;

	public TextSizer textSizer;

	public void Set(string s)
	{
		if (text != null) text.text = s;
		if (textSizer != null)
		{
			textSizer.Refresh();
			textSizer.Recalculate();
		}
		Assets.Scripts.UI.UiUtility.RebuildUi(transform);
	}
}
