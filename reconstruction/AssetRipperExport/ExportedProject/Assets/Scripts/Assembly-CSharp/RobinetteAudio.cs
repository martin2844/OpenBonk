using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

public class RobinetteAudio : MonoBehaviour
{
	public AudioSource audioSource;

	public float desiredVolume = .4f;

	public Animator animator;

	private void Awake()
	{
	}

	private void Update()
	{
		if (MyTime.paused || MyPlayer.Instance == null) return;
		float target = animator.GetBool("grounded") ? 0f : desiredVolume;
		audioSource.volume = Mathf.Lerp(audioSource.volume, target, Time.deltaTime * 20f);
		if (audioSource.volume > .01f)
		{
			if (!audioSource.isPlaying) audioSource.Play();
		}
		else
		{
			audioSource.volume = 0f;
			audioSource.Stop();
		}
	}
}
