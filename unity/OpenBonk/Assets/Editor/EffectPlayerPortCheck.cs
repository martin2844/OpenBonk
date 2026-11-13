using System;
using Assets.Scripts.Utility;
using UnityEngine;

public static class EffectPlayerPortCheck
{
	public static void Run()
	{
		GameObject root = new GameObject("effect port check unique"); root.SetActive(false);
		float oldTime = MyTime.time;
		try
		{
			AudioSource source = root.AddComponent<AudioSource>();
			RandomSfx random = root.AddComponent<RandomSfx>(); random.s = source; random.sounds = Array.Empty<AudioClip>();
			AudioSpamFilter filter = root.AddComponent<AudioSpamFilter>(); filter.audioSource = source; filter.randomSfx = random; filter.spamDelay = 0.2f;
			EffectPlayer effect = root.AddComponent<EffectPlayer>(); effect.ps = root.AddComponent<ParticleSystem>(); effect.audioSpamFilter = filter;
			int played = 0; effect.A_Played += () => played++;
			MyTime.time = 10f; effect.Play();
			Check(played == 1 && source.enabled, "first effect plays");
			MyTime.time = 10.05f; effect.Play();
			Check(played == 1, "particle interval suppresses repeat");
			MyTime.time = 10.11f; effect.Play();
			Check(played == 2 && !source.enabled, "audio spam filter suppresses rapid sound");
			MyTime.time = 10.25f; effect.Play();
			Check(played == 3 && source.enabled, "audio resumes after spam interval");
			Check(Mathf.Abs(AudioSpamFilter.FindVolumeMultiplier(0.1f, 1f, 0.1f, 0.5f) - 0.5f) < 0.001f && AudioSpamFilter.FindVolumeMultiplier(0.1f, 1f, 1f, 0.5f) == 1f, "volume scaling endpoints");
			Debug.Log("EffectPlayerPortCheck passed");
		}
		finally { UnityEngine.Object.DestroyImmediate(root); MyTime.time = oldTime; }
	}
	private static void Check(bool value, string name) { if (!value) throw new Exception("EffectPlayerPortCheck failed: " + name); }
}
