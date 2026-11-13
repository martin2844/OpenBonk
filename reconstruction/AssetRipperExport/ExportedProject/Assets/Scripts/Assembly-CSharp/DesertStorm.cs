using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

public class DesertStorm : MonoBehaviour
{
	public MeshRenderer fogOfWarRenderer;

	private Material fogOfWarMaterial;

	public ParticleSystem[] stormParticles;

	public AudioSource audio;

	private float fadeOverTime = 3f;

	private float fadeTime;

	private bool isStorm;

	private float audioVolume;

	private float oldFogValue;

	private Color oldFogColor;

	private Color stormColor;

	private float stormFogIntensity = 0.03f;

	private void TryInit()
	{
		if (fogOfWarMaterial != null) return;
		fogOfWarMaterial = fogOfWarRenderer.material;
		audioVolume = audio.volume;
	}

	public void FadeIn()
	{
		TryInit();
		gameObject.SetActive(true);
		isStorm = true;
		fogOfWarRenderer.enabled = true;
		oldFogValue = RenderSettings.fogDensity;
		oldFogColor = RenderSettings.fogColor;
		stormColor = Color.Lerp(oldFogColor, new Color(0f, 0f, 0f, 1f), 0.65f);
		fadeTime = 0f;
		audio.volume = 0f;
		audio.Play();
		foreach (ParticleSystem particle in stormParticles) { var emission = particle.emission; emission.enabled = true; }
	}

	public void FadeOut()
	{
		fadeTime = 0f;
		isStorm = false;
		foreach (ParticleSystem particle in stormParticles) { var emission = particle.emission; emission.enabled = false; }
	}

	private void Update()
	{
		transform.position = MyPlayer.Instance.transform.position;
		if (fadeTime < fadeOverTime)
		{
			fadeTime = Mathf.Clamp(fadeTime + MyTime.deltaTime, 0f, fadeOverTime);
			float progress = fadeTime / fadeOverTime;
			float opacity = isStorm ? progress : 1f - progress;
			fogOfWarMaterial.SetColor("_Color", new Color(1f, 1f, 1f, opacity));
			audio.volume = audioVolume * opacity;
			RenderSettings.fogDensity = isStorm ? Mathf.Lerp(oldFogValue, stormFogIntensity, progress) : Mathf.Lerp(stormFogIntensity, oldFogValue, progress);
			RenderSettings.fogColor = isStorm ? Color.Lerp(oldFogColor, stormColor, progress) : Color.Lerp(stormColor, oldFogColor, progress);
		}
		if (fadeTime >= fadeOverTime && !isStorm) { gameObject.SetActive(false); audio.Stop(); }
	}
}
