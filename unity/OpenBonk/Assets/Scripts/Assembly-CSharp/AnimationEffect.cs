using UnityEngine;

public class AnimationEffect : MonoBehaviour
{
	public RandomSfx[] audioSources;

	public ParticleSystem[] particles;

	public void PlayEffect(int index)
	{
		if (particles != null && index >= 0 && index < particles.Length && particles[index] != null)
			particles[index].Play();
	}

	public void PlayAudio(int index)
	{
		if (audioSources != null && index >= 0 && index < audioSources.Length && audioSources[index] != null)
			audioSources[index].Play();
	}
}
