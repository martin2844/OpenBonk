using UnityEngine;
using UnityEngine.UI;

public class AnimatedOutline : MonoBehaviour
{
	private void Start()
	{
		var image = GetComponent<Image>();
		if (image == null || image.material == null || image.rectTransform == null) return;
		Rect rect = image.rectTransform.rect;
		image.material.SetVector("_RectSize", new Vector4(rect.width, rect.height, 0f, 0f));
	}

	private void Update()
	{
	}
}
