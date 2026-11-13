using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

public class TimeFreezeUi : MonoBehaviour
{
	public RawImage swirl;

	public RawImage vignette;

	private bool isTimeFrozen;

	public AudioSource a_start;

	public AudioSource a_end;

	public AudioSource a_loop;

	private void Awake()
	{
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
		bool wasFrozen = isTimeFrozen;
		isTimeFrozen = (MyPlayer.Instance != null && MyPlayer.Instance.inventory != null &&
			MyPlayer.Instance.inventory.statusEffects != null &&
			MyPlayer.Instance.inventory.statusEffects.HasStatusEffect(EStatusEffect.TimeFreeze)) || MyTime.timeScale < 1f;
		if (!wasFrozen && isTimeFrozen) StartTimeFreeze();
		else if (wasFrozen && !isTimeFrozen) EndTimeFreeze();
	}

	private void StartTimeFreeze()
	{
		if (vignette != null)
		{
			vignette.CrossFadeAlpha(0f, 0f, true);
			vignette.CrossFadeAlpha(1f, 1f, true);
			vignette.gameObject.SetActive(true);
		}
		if (a_start != null) a_start.Play();
		if (a_loop != null) a_loop.Play();
	}

	private void EndTimeFreeze()
	{
		if (vignette != null) vignette.gameObject.SetActive(false);
		if (a_end != null) a_end.Play();
		if (a_loop != null) a_loop.Stop();
	}

	private void OnStatusEffectAdded(EStatusEffect eStatusEffect, bool newEffect)
	{
		if (eStatusEffect != EStatusEffect.TimeFreeze) return;
		RefreshTimeFreeze();
		if (swirl == null) return;
		swirl.gameObject.SetActive(true);
		swirl.CrossFadeAlpha(0f, 0f, true);
		swirl.CrossFadeAlpha(1f, 1f, true);
	}

	private void OnStatusEffectRemoved(EStatusEffect eStatusEffect)
	{
		if (eStatusEffect != EStatusEffect.TimeFreeze) return;
		RefreshTimeFreeze();
		if (swirl != null) swirl.gameObject.SetActive(false);
	}

	private void Update()
	{
		if (isTimeFrozen && swirl != null) swirl.transform.Rotate(Vector3.forward, 50f * Time.deltaTime);
	}
}
