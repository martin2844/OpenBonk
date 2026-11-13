using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeWorldUi : MonoBehaviour
{
	public MaskableGraphic damageText;

	public MaskableGraphic otherText;

	private Vector3 randomDir;

	public CanvasGroup cGroup;

	private Vector3 defaultScale;

	private float fadeTime = 0.6f;

	public float startFadeoutTime = 0.5f;

	private bool started;

	private IEnumerator shakeRoutine;

	private float moveMultiplier = 1f;

	private float speed = 8f;

	private Vector3 moveDir;

	private float desiredScale = 4f;

	private void StartFadeOut()
	{
		if (damageText != null) damageText.CrossFadeAlpha(0f, fadeTime, true);
		if (otherText != null) otherText.CrossFadeAlpha(0f, fadeTime, true);
	}

	public void Start()
	{
		if (damageText != null) damageText.CrossFadeAlpha(1f, 0f, true);
		if (otherText != null) otherText.CrossFadeAlpha(1f, 0f, true);
		gameObject.SetActive(true);
		speed = 8f;
		desiredScale = 4f;
		if (defaultScale == Vector3.zero) defaultScale = transform.localScale;
		transform.localScale = defaultScale * .1f;
		randomDir = Random.insideUnitSphere;
		moveDir = Vector3.up + (transform.forward + randomDir) * moveMultiplier;
		Invoke(nameof(StartFadeOut), startFadeoutTime);
		Invoke(nameof(DestroySelf), startFadeoutTime + fadeTime);
		started = true;
	}

	private void Update()
	{
		if (!started) return;
		transform.position += moveDir * Time.deltaTime * speed;
		speed = Mathf.Lerp(speed, .02f, Time.deltaTime * 6f);
		transform.localScale = Vector3.Lerp(transform.localScale, defaultScale * desiredScale, Time.deltaTime * 10f);
		desiredScale = Mathf.Lerp(desiredScale, 3f, Time.deltaTime * 3f);
	}

	private void DestroySelf()
	{
		Destroy(gameObject);
	}
}
