using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Assets.Scripts.Inventory__Items__Pickups;

public class DeathPP : MonoBehaviour
{
	public PostProcessVolume volume;

	private ColorGrading cg;

	private Vignette vignette;

	private bool isEnabled = true;

	private float deadVignetteIntensity = .3f;

	private float deadVignetteSmoothness = .4f;

	private float deadContrast = 20f;

	private float deadSaturation = -40f;

	private bool dead;

	private void Awake()
	{
		if (volume != null && volume.profile != null)
		{
			cg = volume.profile.GetSetting<ColorGrading>();
			vignette = volume.profile.GetSetting<Vignette>();
		}
		PlayerHealth.A_Died += OnPlayerDied;
	}

	private void OnDestroy()
	{
		PlayerHealth.A_Died -= OnPlayerDied;
	}

	private void OnPlayerDied()
	{
		dead = true;
		if (isEnabled && cg != null)
		{
			cg.enabled.value = true;
			cg.active = true;
			cg.postExposure.value = 2f;
		}
	}

	private void Update()
	{
		if (!dead) return;
		if (cg != null)
		{
			cg.postExposure.value = Mathf.Lerp(cg.postExposure.value, 0f, Time.deltaTime * 8f);
			cg.contrast.value = Mathf.Lerp(cg.contrast.value, deadContrast, Time.deltaTime * .5f);
			cg.saturation.value = Mathf.Lerp(cg.saturation.value, deadSaturation, Time.deltaTime * .5f);
		}
		if (vignette != null)
		{
			vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, deadVignetteIntensity, Time.deltaTime * .5f);
			vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, deadVignetteSmoothness, Time.deltaTime * .5f);
		}
	}
}
