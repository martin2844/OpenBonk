using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine.UIElements.Experimental;

public class SlowmoPP : MonoBehaviour
{
	public PostProcessVolume volume;

	private ColorGrading cg;

	private LensDistortion lens;

	private bool isCgEnabled = true;

	private bool isTimeFreeze;

	private bool isSlowmo;

	private bool isDone;

	private float timer = 1f;

	private float transitionTime = 1f;

	private float dLens;

	private float dExposure;

	private float dSaturation;

	private float fromLens;

	private float fromExposure;

	private float fromSaturation;

	private Color desiredColor;

	private Color fromColor;

	private void Awake()
	{
		if (volume != null && volume.profile != null)
		{
			cg = volume.profile.GetSetting<ColorGrading>();
			lens = volume.profile.GetSetting<LensDistortion>();
			if (cg != null) { cg.colorFilter.value = Color.white; cg.postExposure.value = 0f; cg.saturation.value = 0f; }
			if (lens != null) lens.intensity.value = 0f;
		}
		PlayerStatusEffects.A_StatusEffectAdded += OnStatusEffectAdded;
		PlayerStatusEffects.A_StatusEffectRemoved += OnStatusEffectRemoved;
		MyTime.A_TimeScaleChange += RefreshTimeFreeze;
	}

	private void OnDestroy()
	{
		PlayerStatusEffects.A_StatusEffectAdded -= OnStatusEffectAdded;
		PlayerStatusEffects.A_StatusEffectRemoved -= OnStatusEffectRemoved;
		MyTime.A_TimeScaleChange -= RefreshTimeFreeze;
	}

	private void RefreshTimeFreeze()
	{
		bool previous = isTimeFreeze;
		var effects = MyPlayer.Instance != null && MyPlayer.Instance.inventory != null ? MyPlayer.Instance.inventory.statusEffects : null;
		isTimeFreeze = (effects != null && effects.HasStatusEffect(EStatusEffect.TimeFreeze)) || MyTime.timeScale < 1f;
		if (!previous && isTimeFreeze) StartTimeFreeze();
		else if (previous && !isTimeFreeze) EndTimeFreeze();
	}

	private void OnStatusEffectAdded(EStatusEffect eStatusEffect, bool newEffect)
	{
		if (eStatusEffect == EStatusEffect.TimeFreeze) RefreshTimeFreeze();
	}

	private void OnStatusEffectRemoved(EStatusEffect eStatusEffect)
	{
		if (eStatusEffect == EStatusEffect.TimeFreeze) RefreshTimeFreeze();
	}

	private void StartTimeFreeze()
	{
		if (cg == null || lens == null) return;
		if (isCgEnabled) { cg.enabled.value = true; cg.active = true; }
		lens.enabled.value = true; lens.active = true;
		dLens = 20f; dExposure = .6f; dSaturation = -35f;
		desiredColor = new Color(.5f, 1f, 1f, 1f);
		CaptureValues();
		timer = 0f; isSlowmo = true; isDone = false;
	}

	private void EndTimeFreeze()
	{
		if (cg == null || lens == null) return;
		dLens = dExposure = dSaturation = 0f;
		desiredColor = Color.white;
		CaptureValues();
		timer = 0f; isDone = true;
	}

	private void CaptureValues()
	{
		fromColor = cg.colorFilter.value;
		fromExposure = cg.postExposure.value;
		fromSaturation = cg.saturation.value;
		fromLens = lens.intensity.value;
	}

	private void Update()
	{
		if (timer >= 1f || cg == null || lens == null) return;
		timer = Mathf.Min(1f, timer + MyTime.deltaTime / transitionTime);
		float t = Easing.OutCirc(timer);
		cg.postExposure.value = Mathf.Lerp(fromExposure, dExposure, t);
		cg.saturation.value = Mathf.Lerp(fromSaturation, dSaturation, t);
		cg.colorFilter.value = Color.Lerp(fromColor, desiredColor, t);
		lens.intensity.value = Mathf.Lerp(fromLens, dLens, t);
		if (timer >= 1f && isDone)
		{
			cg.enabled.value = false; lens.enabled.value = false;
			cg.active = false; lens.active = false;
			isSlowmo = false;
		}
	}
}
