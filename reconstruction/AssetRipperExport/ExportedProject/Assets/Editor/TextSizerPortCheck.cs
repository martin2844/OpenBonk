using System;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class TextSizerPortCheck
{
	public static void Run()
	{
		GameObject root = new GameObject("text sizing check", typeof(RectTransform)); root.SetActive(false);
		try
		{
			RectTransform panel = root.GetComponent<RectTransform>(); panel.sizeDelta = new Vector2(220f, 100f);
			GameObject textObject = new GameObject("text", typeof(RectTransform), typeof(TextMeshProUGUI)); textObject.transform.SetParent(root.transform);
			TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
			label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/fonts & materials/LiberationSans SDF.asset");
			label.fontSize = 24f; label.enableWordWrapping = false; label.text = "A";
			TextSizer sizer = root.AddComponent<TextSizer>(); sizer.Text = label;
			sizer.Padding = new Vector2(10f, 8f); sizer.MinSize = new Vector2(50f, 20f); sizer.MaxSize = new Vector2(120f, 60f);
			sizer.Refresh(); sizer.Recalculate();
			float shortWidth = panel.rect.width;
			Check(shortWidth >= 59.99f && shortWidth <= 130.01f && panel.rect.height >= 27.99f && panel.rect.height <= 68.01f, "minimum and maximum bounds with padding");
			Check(Mathf.Abs(label.rectTransform.rect.width - panel.rect.width) < 0.01f, "text object tracks panel width");
			label.text = "A much longer objective label"; sizer.Recalculate();
			Check(panel.rect.width > shortWidth && Mathf.Abs(panel.rect.width - 130f) < 0.01f, "changed text triggers maximum width");
			panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 180f); sizer.ControlAxes = TextSizer.Mode.Vertical;
			label.text = "Vertical sizing"; sizer.Recalculate();
			Check(Mathf.Abs(panel.rect.width - 180f) < 0.01f, "uncontrolled horizontal axis stays unchanged");
			sizer.Text = null; sizer.Refresh(); sizer.Recalculate();
			Check(Mathf.Abs(panel.rect.width - 180f) < 0.01f, "missing text leaves size unchanged");
			Debug.Log("TextSizerPortCheck passed");
		}
		finally { UnityEngine.Object.DestroyImmediate(root); }
	}
	private static void Check(bool value, string name) { if (!value) throw new Exception("TextSizerPortCheck failed: " + name); }
}
