using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PPController : MonoBehaviour
{
	private UnityEngine.Rendering.PostProcessing.Bloom bloom;

	private MotionBlur motionBlur;

	private AmbientOcclusion ao;

	private void Awake()
	{
		var volume = GetComponent<PostProcessVolume>();
		if (volume != null && volume.profile != null)
		{
			volume.profile.TryGetSettings(out bloom);
			volume.profile.TryGetSettings(out motionBlur);
			volume.profile.TryGetSettings(out ao);
		}
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
	}

	private void OnDestroy()
	{
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
	}

	private void Start()
	{
		var settings = SaveManager.Instance?.config?.cfVideoSettings;
		if (settings == null) return;
		SetBloom(settings.bloom);
		SetMotionBlur(settings.motion_blur);
		SetAO(settings.ambient_occlusion);
	}

	private void OnSettingUpdated(string name, object oldValue, object newValue)
	{
		if (!(newValue is int value)) return;
		switch (name)
		{
			case "bloom": SetBloom(value); break;
			case "motion_blur": SetMotionBlur(value); break;
			case "ambient_occlusion": SetAO(value); break;
		}
	}

	public void SetBloom(int value)
	{
		if (bloom == null) return;
		switch (value)
		{
			case 0: bloom.enabled.value = false; break;
			case 1: bloom.enabled.value = true; bloom.fastMode.value = true; break;
			case 2: bloom.enabled.value = true; bloom.fastMode.value = false; break;
		}
	}

	public void SetMotionBlur(int on)
	{
		if (motionBlur != null) motionBlur.enabled.value = on == 1;
	}

	public void SetAO(int on)
	{
		if (ao != null) ao.enabled.value = on == 1;
	}
}
