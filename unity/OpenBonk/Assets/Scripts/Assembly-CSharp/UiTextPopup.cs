using TMPro;
using UnityEngine;

public class UiTextPopup : MonoBehaviour
{
	public TextMeshProUGUI t_text;

	public GameObject parent;

	public CanvasGroup canvasGroup;

	private float startFadeTime = 1f;

	private float fadeTime = 0.5f;

	private float upSpeed = 100f;

	private float scaleSpeed = 30f;

	private Vector3 desiredScale;

	public RandomSfx sfx;

	public RectTransform overlayCanvas;

	private bool fading;

	private float fadeTimer;

	public Vector2 TransformCameraToOverlaySpace(Vector3 position)
	{
		return new Vector2(position.x, position.y);
	}

	public void SetTextCameraSpace(string text, Vector3 position, Color color, float desiredScale = 1f)
	{
		SetText(text, new Vector3(position.x, position.y, 0f), color, desiredScale);
	}

	public void SetText(string text, Vector3 position, Color color, float desiredScale = 1f)
	{
		parent.SetActive(true);
		sfx.Play();
		upSpeed = 70f;
		parent.transform.position = position;
		t_text.color = color;
		t_text.text = text;
		fadeTimer = 0f;
		fading = false;
		this.desiredScale = Vector3.one * desiredScale;
		parent.transform.localScale = Vector3.zero;
		CancelInvoke();
		Invoke(nameof(StartFade), startFadeTime);
	}

	private void StartFade()
	{
		fading = true;
		fadeTimer = 0f;
		Invoke(nameof(HideObject), fadeTime);
	}

	private void Update()
	{
		if (fadeTimer < 1f)
		{
			fadeTimer = Mathf.Clamp01(fadeTimer + Time.deltaTime / (fading ? fadeTime : fadeTime * 0.5f));
			canvasGroup.alpha = fading ? 1f - fadeTimer : fadeTimer;
		}
		upSpeed = Mathf.Lerp(upSpeed, 0f, Time.deltaTime * 2.5f);
		parent.transform.position += Vector3.up * Time.deltaTime * upSpeed;
		parent.transform.localScale = Vector3.Lerp(parent.transform.localScale, desiredScale, Time.deltaTime * scaleSpeed);
	}

	private void HideObject()
	{
		parent.SetActive(false);
	}
}
