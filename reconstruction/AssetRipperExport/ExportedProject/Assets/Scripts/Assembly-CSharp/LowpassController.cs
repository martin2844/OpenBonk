using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Audio;

public class LowpassController : MonoBehaviour
{
	public AudioLowPassFilter filter;

	private float defaultCutoff = 22000f;

	private float desiredCutoff = 22000f;

	private float lowCutoff = 1400f;

	private bool isTimeFreeze;

	private bool isUnderwater;

	public AudioMixer audioMixer;

	private float lowpassFrequency = 22000f;

	private void Awake()
	{
		PlayerStatusEffects.A_StatusEffectAdded += OnStatusEffectAdded;
		PlayerStatusEffects.A_StatusEffectRemoved += OnStatusEffectRemoved;
		Water.A_CameraEnterWater += OnWaterFilterEnter;
		Water.A_CameraExitWater += OnWaterFilterExit;
		MyTime.A_TimeScaleChange += RefreshTimeFreeze;
		Lava.A_CameraEnterWater += OnLavaEnter;
		Lava.A_CameraExitWater += OnLavaExit;
	}

	private void OnDestroy()
	{
		PlayerStatusEffects.A_StatusEffectAdded -= OnStatusEffectAdded;
		PlayerStatusEffects.A_StatusEffectRemoved -= OnStatusEffectRemoved;
		Water.A_CameraEnterWater -= OnWaterFilterEnter;
		Water.A_CameraExitWater -= OnWaterFilterExit;
		MyTime.A_TimeScaleChange -= RefreshTimeFreeze;
		Lava.A_CameraEnterWater -= OnLavaEnter;
		Lava.A_CameraExitWater -= OnLavaExit;
	}

	private void OnWaterFilterEnter(Water filter)
	{
		isUnderwater = true;
	}

	private void OnWaterFilterExit(Water filter)
	{
		isUnderwater = false;
	}

	private void OnLavaEnter()
	{
		isUnderwater = true;
	}

	private void OnLavaExit()
	{
		isUnderwater = false;
	}

	private void OnStatusEffectAdded(EStatusEffect eStatusEffect, bool newEffect)
	{
		if (eStatusEffect == EStatusEffect.TimeFreeze) RefreshTimeFreeze();
	}

	private void OnStatusEffectRemoved(EStatusEffect eStatusEffect)
	{
		if (eStatusEffect == EStatusEffect.TimeFreeze) RefreshTimeFreeze();
	}

	private void RefreshTimeFreeze()
	{
		isTimeFreeze = (MyPlayer.Instance != null && MyPlayer.Instance.inventory != null && MyPlayer.Instance.inventory.statusEffects.HasStatusEffect(EStatusEffect.TimeFreeze)) || MyTime.timeScale < 1f;
	}

	private float GetDesiredCutoff()
	{
		if (isTimeFreeze || (UiManager.Instance != null && UiManager.Instance.pause != null && UiManager.Instance.pause.IsPaused()) || isUnderwater) return lowCutoff;
		float danger = MyPlayer.Instance != null && MyPlayer.Instance.playerEffects != null ? MyPlayer.Instance.playerEffects.GetDangerRatio() : 0f;
		return danger > 0.01f ? Mathf.Lerp(defaultCutoff, 900f, danger) : defaultCutoff;
	}

	private void Update()
	{
		if (filter == null || audioMixer == null) return;
		lowpassFrequency = Mathf.Lerp(filter.cutoffFrequency, GetDesiredCutoff(), Time.deltaTime * 6f);
		audioMixer.SetFloat("AmbienceLowpass", lowpassFrequency);
		audioMixer.SetFloat("GameSfxLowpass", lowpassFrequency);
		audioMixer.SetFloat("MusicLowpass", lowpassFrequency);
	}
}
