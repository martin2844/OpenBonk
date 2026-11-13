using UnityEngine;

public class Campfire : MonoBehaviour
{
	public ParticleSystem createFx;

	public AudioSource audioSource;

	public RandomSfx randomSfx;

	private float animationTimer;

	private bool isActive;

	private float animateTime = 0.2f;

	private Vector3 fromScale;

	public void StartFire(Vector3 pos)
	{
		if (createFx != null && createFx.transform.parent != null) createFx.transform.SetParent(null);
		gameObject.SetActive(true);
		transform.position = pos;
		if (createFx != null)
		{
			createFx.transform.position = pos + Vector3.up * 0.1f;
			createFx.gameObject.SetActive(true);
			createFx.Play();
		}
		if (audioSource != null) audioSource.enabled = true;
		if (randomSfx != null) randomSfx.Play();
		isActive = true;
		fromScale = Vector3.zero;
	}

	public void EndFire()
	{
		isActive = false;
	}

	private void Update()
	{
		animationTimer = Mathf.Clamp(animationTimer + (isActive ? Time.deltaTime : -Time.deltaTime), 0f, animateTime);
		float t = UnityEngine.UIElements.Experimental.Easing.InOutCirc(animationTimer / animateTime);
		transform.localScale = Vector3.Lerp(fromScale, Vector3.one * 1.5f, t);
		if (animationTimer <= 0f)
		{
			gameObject.SetActive(false);
			if (createFx != null) createFx.gameObject.SetActive(false);
		}
	}
}
