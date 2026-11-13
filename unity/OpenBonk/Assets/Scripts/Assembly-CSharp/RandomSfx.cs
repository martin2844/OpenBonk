using UnityEngine;

public class RandomSfx : MonoBehaviour
{
	public AudioClip[] sounds;

	[Range(0f, 3f)]
	public float maxPitch;

	[Range(0f, 3f)]
	public float minPitch;

	public float randomVolume;

	public AudioSource s;

	public bool playOnAwake;

	private float defaultVolume = -1f;

	private void Awake()
	{
		Init();
		if (playOnAwake) Play();
	}

	private void Init()
	{
		if (defaultVolume != -1f) return;
		if (s == null) s = GetComponent<AudioSource>();
		if (s != null) defaultVolume = s.volume;
	}

	public void Play(float delay = 0f, float volumeMultiplier = 1f)
	{
		Init();
		if (sounds == null || sounds.Length == 0 || s == null) return;
		s.volume = defaultVolume * volumeMultiplier * (1f + Random.Range(-randomVolume, randomVolume));
		s.clip = sounds[Random.Range(0, sounds.Length)];
		s.pitch = Random.Range(minPitch, maxPitch);
		if (s.enabled) s.PlayDelayed(delay);
	}

	public void Stop()
	{
		if (s != null) s.Stop();
	}

	private void OnValidate()
	{
	}
}
