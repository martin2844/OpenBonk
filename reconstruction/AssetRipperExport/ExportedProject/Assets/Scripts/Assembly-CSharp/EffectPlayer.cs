using System;
using UnityEngine;
using Assets.Scripts.Utility;

public class EffectPlayer : MonoBehaviour
{
	public float psMinInterval = 0.1f;

	public ParticleSystem ps;

	public AudioSpamFilter audioSpamFilter;

	public RandomSfx randomSfx;

	private float nextPlayTime;

	public bool playOnEnable;

	public Action A_Played;

	private void OnEnable()
	{
		if (playOnEnable) Play();
	}

	public void Play()
	{
		if (nextPlayTime > MyTime.time) return;
		nextPlayTime = MyTime.time + psMinInterval;
		ps.Play();
		if (audioSpamFilter != null) audioSpamFilter.OnEnable();
		else if (randomSfx != null) randomSfx.Play();
		A_Played?.Invoke();
	}

	private void OnValidate()
	{
		if (ps == null) ps = GetComponent<ParticleSystem>();
		if (audioSpamFilter == null) audioSpamFilter = GetComponent<AudioSpamFilter>();
		if (randomSfx == null) randomSfx = GetComponent<RandomSfx>();
	}
}
