using UnityEngine;

public class AttackMuzzle : MonoBehaviour
{
	public RandomSfx randomSfx;
	public ParticleSystem ps;
	private float cliplength;
	private float volumeMultiplier = 1f;
	public float minVolumeMultiplier = .5f;
	public float maxInterval = .4f;
	public float minAudioCooldown = .06f;
	private float lastPlayedTime;

	public void Set(int quantity, float burstInterval)
	{
		// The original reads the first sound's length, but volume depends on
		// the fixed step and interval rather than the clip or burst quantity.
		if (randomSfx != null && randomSfx.sounds != null && randomSfx.sounds.Length > 0 && randomSfx.sounds[0] != null)
			cliplength = randomSfx.sounds[0].length;
		volumeMultiplier = AudioSpamFilter.FindVolumeMultiplier(Time.fixedDeltaTime, maxInterval, burstInterval, minVolumeMultiplier);
	}

	public void Play()
	{
		if (lastPlayedTime + minAudioCooldown > Time.time) return;
		lastPlayedTime = Time.time;
		if (randomSfx != null && randomSfx.enabled) randomSfx.Play(0f, volumeMultiplier);
		if (ps != null) ps.Play();
	}

	private void OnValidate()
	{
		randomSfx = GetComponent<RandomSfx>();
		ps = GetComponent<ParticleSystem>();
	}
}
