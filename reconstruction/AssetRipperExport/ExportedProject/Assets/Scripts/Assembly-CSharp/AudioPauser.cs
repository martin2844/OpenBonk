using UnityEngine;
using Assets.Scripts.Utility;

public class AudioPauser : MonoBehaviour
{
	public AudioSource audioSource;

	private void OnValidate()
	{
		FindAudioSource();
	}

	private void FindAudioSource()
	{
		if (audioSource == null) audioSource = GetComponent<AudioSource>();
	}

	private void Awake()
	{
		FindAudioSource();
		MyTime.A_Pause += OnPause;
	}

	private void OnDestroy()
	{
		MyTime.A_Pause -= OnPause;
	}

	private void OnPause(bool isPaused)
	{
		if (audioSource == null) return;
		if (isPaused) audioSource.Pause();
		else audioSource.UnPause();
	}
}
