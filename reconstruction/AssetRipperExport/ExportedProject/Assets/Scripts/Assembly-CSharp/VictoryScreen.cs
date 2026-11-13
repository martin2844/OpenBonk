using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.UI.Animation;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;

public class VictoryScreen : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CShowNewItem_003Ed__27 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public VictoryScreen _003C_003E4__this;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CShowNewItem_003Ed__27(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	public GameObject content;

	public RawImage background;

	public RawImage itemDisplay;

	public TextMeshProUGUI itemNameText;

	public TextMeshProUGUI extraText;

	public TextMeshProUGUI t_score;

	public ParticleSystem ps;

	public GameObject btnContinue;

	public GameObject coinFx;

	public UiAnimation buttonAnimation;

	private float fadeInTime = 0.6f;

	private float fadeOutTime = 0.2f;

	private float cardDesiredScale;

	private float textDesiredScale;

	private float desiredAlpha = 0.99f;

	private float yRotation = 1000f;

	public AudioSource silverSfx;

	public AudioSource silverSfxFinish;

	private float animatorTime;

	private float animatorSpeed = 0.8f;

	public AudioSource sfx;

	private bool silverStarted;

	private int maxSilver;

	private float silverTimer;

	private float silverAnimateTime = 1.75f;

	private void Start()
	{
		if (background != null)
		{
			background.color = GetBackgroundColor();
			background.CrossFadeAlpha(0f, 0f, true);
			background.CrossFadeAlpha(desiredAlpha, fadeInTime, false);
		}
		if (itemNameText != null) itemNameText.transform.localScale = Vector3.zero;
		if (btnContinue != null) btnContinue.SetActive(false);
		StartCoroutine(ShowNewItem());
	}

	private void Update()
	{
		Animate();
	}

	private Color GetBackgroundColor()
	{
		int tier = MapController.runConfig != null ? MapController.runConfig.mapTierIndex : 0;
		return Color.Lerp(MyColorUtility.GetTierColor(tier), Color.black, 0.85f);
	}

	private void Animate()
	{
		float card = UnityEngine.UIElements.Experimental.Easing.OutElastic(Mathf.Clamp01(animatorTime * 0.85f));
		float cardTurn = UnityEngine.UIElements.Experimental.Easing.OutElastic(Mathf.Clamp01(animatorTime * 0.5f));
		float textTime = Mathf.Clamp01((animatorTime - 0.1f) * 0.5f);
		float textScale = UnityEngine.UIElements.Experimental.Easing.OutPower(textTime, 20);
		float textTurn = UnityEngine.UIElements.Experimental.Easing.OutElastic(textTime);
		if (itemDisplay != null)
		{
			itemDisplay.transform.localScale = Vector3.one * card;
			itemDisplay.transform.localRotation = Quaternion.Euler(0f, (cardTurn - 1f) * 10f, 0f);
		}
		if (itemNameText != null)
		{
			itemNameText.transform.localScale = Vector3.one * textScale;
			itemNameText.transform.localRotation = Quaternion.Euler(0f, (textTurn - 1f) * 5f, 0f);
		}
		yRotation = Mathf.Lerp(yRotation, 0f, Time.deltaTime * 3f);
		animatorTime += Time.deltaTime * animatorSpeed;

		if (extraText != null && extraText.gameObject.activeInHierarchy)
			extraText.transform.localScale = Vector3.Lerp(extraText.transform.localScale, Vector3.one, Time.deltaTime * 6f);

		if (silverStarted && silverTimer < silverAnimateTime)
		{
			silverTimer = Mathf.Clamp(silverTimer + Time.deltaTime, 0f, silverAnimateTime);
			float progress = silverTimer / silverAnimateTime;
			int shown = Mathf.Max(Mathf.RoundToInt(maxSilver * Mathf.Clamp01(progress)), Mathf.FloorToInt(silverTimer / 0.12f));
			if (extraText != null) extraText.text = string.Format("<sprite name=silver> {0}", shown) + "\n<size=60%>" + LocalizationUtility.GetLocalizedString("Game_RoundOver", "VICTORY_SILVER");
			if (silverSfx != null)
			{
				silverSfx.pitch = 1f + progress * 0.25f;
				silverSfx.volume = 0.3f + progress * 0.2f;
			}
			if (silverTimer >= silverAnimateTime || shown >= maxSilver) FinishSilverCounter();
		}

		if (silverTimer > 0f && silverTimer < silverAnimateTime && MyInputManager.GetButtonDown(MyInputManager.Interact))
			FinishSilverCounter();
	}

	private void FinishSilverCounter()
	{
		silverTimer = silverAnimateTime;
		if (silverSfxFinish != null) silverSfxFinish.Play();
		if (silverSfx != null) silverSfx.Stop();
		if (extraText != null)
		{
			extraText.transform.localScale = Vector3.one * 1.6f;
			extraText.text = string.Format("<sprite name=silver> {0}", maxSilver) + "\n<size=60%>"
				+ LocalizationUtility.GetLocalizedString("Game_RoundOver", "VICTORY_SILVER");
		}
		if (coinFx != null) coinFx.SetActive(true);
		Invoke(nameof(ShowButton), 0.25f);
	}

	private void ShowButton()
	{
		if (btnContinue == null) return;
		btnContinue.SetActive(true);
		if (buttonAnimation != null) buttonAnimation.CrossFadeAndScaleIn(0.5f, EEasing.InOutCirc);
		ButtonManager.ForceHoverButton(btnContinue.GetComponent<MyButton>());
	}

	private IEnumerator ShowNewItem()
	{
		if (content != null) content.SetActive(true);
		if (sfx != null) sfx.Play();
		int tier = MapController.runConfig != null ? MapController.runConfig.mapTierIndex : 0;
		Color tierColor = MyColorUtility.GetTierColor(tier);
		if (ps != null)
		{
			var main = ps.main;
			main.startColor = tierColor;
			ps.Play();
		}
		if (t_score != null)
		{
			var localizedTier = LocalizationUtility.GetLocalizedStringReference("Other", "TIER_SMART");
			t_score.color = tierColor;
			t_score.text = localizedTier.GetLocalizedString(new Dictionary<string, object> { ["tier"] = (tier + 1).ToString() })
				+ "\n<size=50%>" + LocalizationUtility.GetLocalizedString("Game_RoundOver", "VICTORY_COMPLETED");
		}
		if (extraText != null) extraText.text = string.Empty;
		animatorTime = 0f;
		yRotation = 720f;
		yield return new WaitForSeconds(1f);
		maxSilver = RunStats.GetStat(EMyStat.silverEarned);
		if (extraText != null)
		{
			extraText.gameObject.SetActive(true);
			extraText.transform.localScale = Vector3.zero;
		}
		silverStarted = true;
		if (silverSfx != null) silverSfx.Play();
	}
}
