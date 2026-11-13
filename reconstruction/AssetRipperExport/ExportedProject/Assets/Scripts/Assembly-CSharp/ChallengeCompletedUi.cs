using System.Collections;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class ChallengeCompletedUi : MonoBehaviour
{
	public AudioSource sfx;
	public GameObject content;
	public GameObject text;
	public GameObject challengeBox;
	public TextMeshProUGUI t_header;
	public TextMeshProUGUI t_description;
	public TextSizer textSizer;
	public CanvasGroup canvasGroup;
	public GameObject particles;
	public RawImage shadow;
	private float fadeInTime = .6f;
	private float fadeOutTime = .2f;
	private float cardDesiredScale;
	private float textDesiredScale;
	private float desiredAlpha = .98f;
	private float animatorTime;
	private float animatorSpeed = .8f;
	public Image background;

	private void Awake()
	{
		ChallengesTracker.A_ChallengeCompleted += OnAchievementUnlocked;
	}

	private void OnDestroy()
	{
		ChallengesTracker.A_ChallengeCompleted -= OnAchievementUnlocked;
	}

	private void Test() { }

	private void OnAchievementUnlocked(ChallengeData challenge)
	{
		if (challenge == null) return;
		if (t_header != null) t_header.text = challenge.GetDisplayName();
		StartCoroutine(StartAnimate());
	}

	private void Update() => Animate();

	private void Animate()
	{
		if (content == null || !content.activeSelf) return;
		float textTime = Mathf.Clamp01((animatorTime - .1f) * .5f);
		if (text != null)
		{
			text.transform.localScale = Vector3.one * Easing.OutPower(textTime, 20);
			text.transform.localRotation = Quaternion.Euler(0f, (Easing.OutElastic(textTime) - 1f) * 5f, 0f);
		}
		if (challengeBox != null) challengeBox.transform.localScale = Vector3.one * Easing.OutPower(Mathf.Clamp01(animatorTime - .5f), 10);
		animatorTime += Time.deltaTime * animatorSpeed;
		if (animatorTime > 1.5f) animatorSpeed = -.8f;
		if (animatorSpeed < 0f && animatorTime < .55f && background != null) background.CrossFadeAlpha(0f, .3f, false);
		if (animatorTime < 0f)
		{
			animatorTime = 0f;
			content.SetActive(false);
			if (particles != null) particles.SetActive(false);
		}
	}

	private IEnumerator StartAnimate()
	{
		while (!Application.isFocused) yield return new WaitForSeconds(Time.deltaTime);
		if (content != null) content.SetActive(true);
		if (canvasGroup != null) canvasGroup.alpha = 1f;
		if (background != null)
		{
			background.CrossFadeAlpha(0f, 0f, true);
			background.CrossFadeAlpha(desiredAlpha, fadeInTime, false);
		}
		if (sfx != null) sfx.Play();
		if (text != null) text.transform.localScale = Vector3.zero;
		if (challengeBox != null) challengeBox.transform.localScale = Vector3.zero;
		animatorTime = 0f;
		animatorSpeed = .8f;
		if (particles != null) particles.SetActive(true);
	}
}
