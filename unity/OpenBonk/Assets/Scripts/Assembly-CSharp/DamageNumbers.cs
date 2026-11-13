using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using Assets.Scripts.Objects.Pooling;

public class DamageNumbers : MonoBehaviour
{
	public TextMeshProUGUI damageText;

	private Vector3 randomDir;

	private Vector3 defaultScale;

	private float fadeTime = .4f;

	private float startFadeoutTime = .5f;

	private bool started;

	private IEnumerator shakeRoutine;

	private static StringBuilder sb;

	private float moveMultiplier = 1f;

	private float speed = 8f;

	private Vector3 moveDir;

	private static string[] suffixes = { "", "K", "M", "B", "T", "Q", "Qi", "Sx", "Sp", "Oc", "No", "Dc", "Ud" };

	private float desiredScale = 4f;

	private void StartFadeOut()
	{
	}

	public void SetDamage(float dmg, Color color, Vector3 position, int textSize = 24)
	{
		SetDamage(FormatDamageNumber(dmg), color, position, textSize);
	}

	public void SetDamage(string text, Color color, Vector3 position, int textSize = 24)
	{
		damageText.fontSize = textSize;
		gameObject.SetActive(true);
		speed = 8f;
		desiredScale = 5f;
		if (defaultScale.sqrMagnitude < 1e-10f) defaultScale = transform.localScale;
		transform.localScale = defaultScale * .1f;
		transform.position = position;
		transform.LookAt(PlayerCamera.Instance.transform.position);
		randomDir = Random.insideUnitSphere;
		moveDir = Vector3.up + (randomDir + transform.forward) * moveMultiplier;
		damageText.color = color;
		damageText.text = text;
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
		desiredScale = Mathf.Lerp(desiredScale, 1f, Time.deltaTime * 4f);
	}

	public static string FormatDamageNumber(float number, string decimalFormat = "0.0")
	{
		if (number < 10000f) return number.ToString("N0");
		int index = 0;
		while (number >= 1000f && index < suffixes.Length - 1) { number /= 1000f; index++; }
		string format = Mathf.Abs(number - Mathf.Round(number)) < .001f ? "0" : decimalFormat;
		return number.ToString(format) + suffixes[index];
	}

	private void DestroySelf()
	{
		PoolManager.Instance.damageNumbersPool.Release(gameObject);
		gameObject.SetActive(false);
	}
}
