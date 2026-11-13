using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Utility;

public class AudioSpamFilter : MonoBehaviour
{
	private class SpamFilterContainer
	{
		public bool isMuted;

		public float unmuteTime;

		public float lastPlayedTime;
	}

	public AudioSource audioSource;

	public RandomSfx randomSfx;

	private static Dictionary<string, SpamFilterContainer> spamFilter = new Dictionary<string, SpamFilterContainer>();

	public float spamDelay = 0.06f;

	private string id;

	private bool isStringInit;

	public float minVolumeMultiplier = 0.5f;

	public float maxInterval = 0.4f;

	public float overrideMinInterval;

	public void OnEnable()
	{
		if (!isStringInit) { isStringInit = true; id = gameObject.name; }
		if (!spamFilter.TryGetValue(id, out SpamFilterContainer entry))
			spamFilter.Add(id, entry = new SpamFilterContainer());
		if (entry.unmuteTime > MyTime.time)
		{
			audioSource.Stop();
			audioSource.enabled = false;
			return;
		}
		float interval = MyTime.time - entry.lastPlayedTime;
		entry.lastPlayedTime = MyTime.time;
		entry.unmuteTime = MyTime.time + spamDelay;
		audioSource.enabled = true;
		float minInterval = overrideMinInterval > 0f ? overrideMinInterval : spamDelay;
		randomSfx.Play(0f, FindVolumeMultiplier(minInterval, maxInterval, interval, minVolumeMultiplier));
	}

	public static float FindVolumeMultiplier(float minInterval, float maxInterval, float interval, float minVolumeMultiplierValue, bool log = false)
	{
		if (maxInterval <= interval) return 1f;
		float progress = (interval - minInterval) / (maxInterval - minInterval);
		if (log) Debug.LogError("Audio interval progress: " + progress);
		return Mathf.Lerp(minVolumeMultiplierValue, 1f, UnityEngine.UIElements.Experimental.Easing.OutCirc(progress));
	}

	private void OnValidate()
	{
		audioSource = GetComponent<AudioSource>();
		randomSfx = GetComponent<RandomSfx>();
	}
}
