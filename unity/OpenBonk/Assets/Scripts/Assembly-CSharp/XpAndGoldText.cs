using Assets.Scripts.Utility;
using TMPro;
using UnityEngine;

public class XpAndGoldText : MonoBehaviour
{
	public string prefix;

	public TextMeshProUGUI text;

	private float timeout = 1f;

	private int amount;

	private float startFadeTime;

	private float fadeTime = 0.75f;

	private float timeToFade = 0.75f;

	public void Add(int amount)
	{
		if (amount < 0) return;
		if (!gameObject.activeInHierarchy)
		{
			gameObject.SetActive(true);
			this.amount = 0;
		}
		if (transform.localScale.x < 2f) transform.localScale *= 1.3f;
		this.amount += amount;
		if (text != null) text.text = prefix + this.amount;
		startFadeTime = MyTime.time + timeToFade;
	}

	private void Update()
	{
		transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 6f);
		if (text != null)
			text.alpha = MyTime.time > startFadeTime ? 1f - Mathf.Clamp01((MyTime.time - startFadeTime) / timeToFade) : 1f;
		if (MyTime.time > startFadeTime + fadeTime) gameObject.SetActive(false);
	}
}
