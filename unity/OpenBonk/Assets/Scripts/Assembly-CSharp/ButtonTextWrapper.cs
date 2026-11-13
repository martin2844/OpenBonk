using TMPro;
using UnityEngine;

public class ButtonTextWrapper : MonoBehaviour
{
	public RectTransform rect;

	public TextMeshProUGUI t_text;

	public int paddingX;

	public int paddingY;

	public float yOffsetRatio;

	public float offsetLeftX;

	public void Refresh()
	{
		if (t_text == null || rect == null) return;
		RectTransform textRect = t_text.rectTransform;
		Vector2 size = textRect.sizeDelta;
		textRect.anchoredPosition = new Vector2(offsetLeftX, size.y * yOffsetRatio);
		rect.sizeDelta = new Vector2(size.x + paddingX * 2f, size.y + paddingY * 2f);
	}

	private void OnValidate()
	{
		if (t_text == null) t_text = GetComponentInChildren<TextMeshProUGUI>();
		if (rect == null) rect = GetComponent<RectTransform>();
		Refresh();
	}
}
