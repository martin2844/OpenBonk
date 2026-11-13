using UnityEngine;

public class DeathScreenBlocksUI : MonoBehaviour
{
	public Material material;

	public AudioSource audio;

	private bool started;

	private float transitionTime;

	private float timer;

	public void StartTransition(float transitionTime)
	{
		this.transitionTime = transitionTime;
		started = true;
		gameObject.SetActive(true);
		if (material != null) material.SetFloat("_Progress", 0f);
		if (audio != null) audio.Play();
	}

	private void Update()
	{
		if (!started) return;
		timer += Time.deltaTime / transitionTime;
		if (material != null) material.SetFloat("_Progress", timer * timer);
	}
}
