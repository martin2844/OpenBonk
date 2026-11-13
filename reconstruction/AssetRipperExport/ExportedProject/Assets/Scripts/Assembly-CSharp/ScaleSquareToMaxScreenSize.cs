using UnityEngine;
using UnityEngine.UI;

public class ScaleSquareToMaxScreenSize : MonoBehaviour
{
	private RectTransform rect;

	public CanvasScaler canvasScaler;

	private void OnEnable()
	{
		if (rect == null) rect = GetComponent<RectTransform>();
		if (rect == null || canvasScaler == null) return;
		float size = SelectSize(Screen.width, Screen.height, canvasScaler.referenceResolution);
		rect.sizeDelta = new Vector2(size, size);
	}

	private static float SelectSize(float width, float height, Vector2 referenceResolution)
	{
		return width / height > 1f ? referenceResolution.x : referenceResolution.y;
	}
}
