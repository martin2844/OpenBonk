using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScalingEntry : MonoBehaviour
{
	public TextMeshProUGUI t_text;

	public RawImage parentSquare;

	public RawImage timeSquare;

	public RawImage stageSquare;

	public RawImage finalSwarmSquare;

	public void Set(string text, float timeM, float stageM, float finalM)
	{
		if (t_text != null) t_text.text = text;
		if (parentSquare == null || timeSquare == null || stageSquare == null || finalSwarmSquare == null) return;
		float width = parentSquare.rectTransform.sizeDelta.x;
		float total = timeM + stageM + finalM;
		float timeWidth = width * timeM / total;
		float stageWidth = width * stageM / total;
		float finalWidth = width * finalM / total;
		timeSquare.rectTransform.sizeDelta = new Vector2(timeWidth, parentSquare.rectTransform.sizeDelta.y);
		stageSquare.rectTransform.sizeDelta = new Vector2(stageWidth, parentSquare.rectTransform.sizeDelta.y);
		stageSquare.rectTransform.anchoredPosition = new Vector2(timeWidth, stageSquare.rectTransform.anchoredPosition.y);
		finalSwarmSquare.rectTransform.sizeDelta = new Vector2(finalWidth, parentSquare.rectTransform.sizeDelta.y);
		finalSwarmSquare.rectTransform.anchoredPosition = new Vector2(timeWidth + stageWidth, finalSwarmSquare.rectTransform.anchoredPosition.y);
	}
}
