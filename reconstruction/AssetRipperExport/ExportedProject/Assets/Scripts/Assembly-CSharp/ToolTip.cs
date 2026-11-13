using TMPro;
using UnityEngine;

public class ToolTip : MonoBehaviour
{
	public TextMeshProUGUI t_tip;

	public CanvasGroup group;

	public Canvas parentCanvas;

	public RectTransform backDrop;

	public static ToolTip Instance;

	private bool visible;

	private bool useMouse;

	private float x;

	private float speed = 6f;

	private float offset = 4f;

	private void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
		TransitionUI.A_transitionStart += HideTip;
		TransitionUI.A_transitionEnd += HideTip;
	}

	private void OnDestroy()
	{
		TransitionUI.A_transitionStart -= HideTip;
		TransitionUI.A_transitionEnd -= HideTip;
		if (Instance == this) Instance = null;
	}

	public void SetTip(string text)
	{
		t_tip.text = text;
		visible = true;
		useMouse = true;
	}

	public void SetTip(string text, RectTransform uiElement)
	{
		t_tip.text = text;
		visible = true;
		useMouse = false;
		var corners = new Vector3[4];
		uiElement.GetWorldCorners(corners);
		transform.position = (corners[1] + corners[2]) * 0.5f + Vector3.up * 5f;
	}

	public void HideTip()
	{
		visible = false;
	}

	private void Update()
	{
		x = Mathf.Clamp01(x + (visible ? 1f : -1f) * Time.deltaTime * speed);
		float eased = x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;
		group.alpha = x;
		group.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, eased);
		if (useMouse)
		{
			var canvasTransform = parentCanvas.transform as RectTransform;
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTransform, Input.mousePosition, parentCanvas.worldCamera, out var local))
				transform.position = parentCanvas.transform.TransformPoint(local) + (Vector3)(Vector2.one * (offset * parentCanvas.scaleFactor));
		}
		var corners = new Vector3[4];
		backDrop.GetWorldCorners(corners);
		var adjustment = Vector3.zero;
		if (corners[2].x > Screen.width) adjustment.x = corners[2].x - Screen.width;
		else if (corners[0].x < 0f) adjustment.x = corners[0].x;
		if (corners[2].y > Screen.height) adjustment.y = corners[2].y - Screen.height;
		else if (corners[0].y < 0f) adjustment.y = corners[0].y;
		transform.position -= adjustment;
	}
}
