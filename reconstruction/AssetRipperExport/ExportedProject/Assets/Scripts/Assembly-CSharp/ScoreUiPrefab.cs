using TMPro;
using UnityEngine;

public class ScoreUiPrefab : MonoBehaviour
{
	public TextMeshProUGUI t_score;

	public TextMeshProUGUI t_desc;

	private float desiredScale;

	private float startFadeTime = 1.5f;

	private float fadeTime = 1f;

	public float bounceSpeed = 3.5f;

	public float scaleSpeed = 16f;

	private float scaleMultiplier = 1f;

	private bool moveDesc;

	private bool isActive;

	private void Update()
	{
		if (desiredScale > 1f) desiredScale -= Time.deltaTime * bounceSpeed;
		t_score.transform.localScale = Vector3.Lerp(t_score.transform.localScale, Vector3.one * desiredScale, Time.deltaTime * scaleSpeed) * scaleMultiplier;
		if (moveDesc) t_desc.rectTransform.anchoredPosition = Vector2.Lerp(t_desc.rectTransform.anchoredPosition, Vector2.zero, Time.deltaTime * scaleSpeed);
	}

	public void SetScore(string description, string header, float sizeMultiplier)
	{
		gameObject.SetActive(true);
		transform.localScale = Vector3.one * sizeMultiplier;
		t_score.text = header ?? "";
		t_score.transform.localScale = Vector3.one * .2f;
		t_desc.text = description;
		t_desc.rectTransform.anchoredPosition = new Vector2(0f, -t_desc.rectTransform.sizeDelta.y);
		moveDesc = false;
		desiredScale = 2f;
		t_score.CrossFadeAlpha(1f, 0f, true);
		CancelInvoke();
		Invoke(nameof(ShowDesc), .25f);
		Invoke(nameof(StartFade), startFadeTime);
	}

	private void StartFade()
	{
		t_score.CrossFadeAlpha(0f, fadeTime, true);
		t_desc.CrossFadeAlpha(0f, fadeTime, true);
		Invoke(nameof(DisableObject), fadeTime);
	}

	private void ShowDesc()
	{
		moveDesc = true;
		t_desc.CrossFadeAlpha(1f, 0f, true);
	}

	private void DisableObject()
	{
		gameObject.SetActive(false);
		isActive = false;
	}
}
