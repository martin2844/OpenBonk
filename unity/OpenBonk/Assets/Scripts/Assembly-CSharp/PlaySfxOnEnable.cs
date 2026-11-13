using UnityEngine;

public class PlaySfxOnEnable : MonoBehaviour
{
	public RandomSfx randomSfx;

	public AudioSource audioSource;

	private void OnEnable()
	{
		if (randomSfx != null) randomSfx.Play();
		else if (audioSource != null) audioSource.Play();
	}

	private void OnValidate()
	{
		randomSfx = GetComponent<RandomSfx>();
		audioSource = GetComponent<AudioSource>();
	}
}
