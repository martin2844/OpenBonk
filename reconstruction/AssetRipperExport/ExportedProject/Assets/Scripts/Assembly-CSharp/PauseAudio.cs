using Assets.Scripts.Utility;
using UnityEngine;

public class PauseAudio : MonoBehaviour
{
	public AudioSource source;
	private bool isPaused;

	private void Awake()
	{
		MyTime.A_Pause += OnPause;
	}

	private void OnDestroy()
	{
		MyTime.A_Pause -= OnPause;
	}

	private void OnValidate()
	{
		source = GetComponent<AudioSource>();
	}

	private void OnPause(bool paused)
	{
		if (source == null) return;
		if (source.isPlaying && paused)
		{
			source.Pause();
			isPaused = true;
		}
		else if (isPaused)
		{
			source.UnPause();
			isPaused = false;
		}
	}
}
