using System;
using UnityEngine;
using UnityEngine.UI;

public static class ButtonHoverPortCheck
{
	public static void Run()
	{
		var go = new GameObject("hover check", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		try
		{
			var image = go.GetComponent<Image>();
			Color original = new Color(.5f, .4f, .3f, .8f);
			image.color = original;
			var button = go.AddComponent<MyButtonNormal>();
			button.background = image;
			button.StartHover();
			if ((Vector4)(image.color - Color.Lerp(original, Color.black, .2f)) != Vector4.zero) throw new Exception("Hover color mismatch");
			button.StopHover();
			if ((Vector4)(image.color - original) != Vector4.zero) throw new Exception("Original color was not restored");
			Debug.Log("ButtonHoverPortCheck passed");
		}
		finally { UnityEngine.Object.DestroyImmediate(go); }
	}
}
