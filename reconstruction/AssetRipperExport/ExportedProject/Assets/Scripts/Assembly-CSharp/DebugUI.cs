using TMPro;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using UnityEngine.Profiling;

public class DebugUI : MonoBehaviour
{
	public TextMeshProUGUI t_fps;

	public TextMeshProUGUI t_speed;

	public TextMeshProUGUI t_ram;

	private float fpsTimer;

	private int frameCount;

	private float fpsUpdateInterval = 0.25f;

	private float[] speedSamples = new float[2];

	private int speedSampleIndex;

	private float sampleSpeedInterval = 0.1f;

	private float sampleRamInterval = 0.5f;

	private void Awake()
	{
		InvokeRepeating(nameof(SampleSpeed), 0f, sampleSpeedInterval);
		InvokeRepeating(nameof(SampleRam), 0f, sampleRamInterval);
		var settings = SaveManager.Instance != null && SaveManager.Instance.config != null ? SaveManager.Instance.config.cfGameSettings : null;
		if (t_fps != null) t_fps.gameObject.SetActive(settings != null && settings.debug_fps == 1);
		if (t_speed != null) t_speed.gameObject.SetActive(settings != null && settings.debug_speed == 1);
		if (t_ram != null) t_ram.gameObject.SetActive(settings != null && settings.debug_ram == 1);
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
	}

	private void OnDestroy()
	{
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
	}

	private void OnSettingUpdated(string setting, object oldValue, object newValue)
	{
		if (setting == "debug_fps") t_fps.gameObject.SetActive((int)newValue == 1);
		else if (setting == "debug_speed") t_speed.gameObject.SetActive((int)newValue == 1);
		else if (setting == "debug_ram") t_ram.gameObject.SetActive((int)newValue == 1);
	}

	private void Update()
	{
		UpdateFps();
	}

	private void UpdateFps()
	{
		if (!t_fps.gameObject.activeInHierarchy) return;
		fpsTimer += Time.unscaledDeltaTime;
		frameCount++;
		if (fpsTimer < fpsUpdateInterval) return;
		t_fps.text = $"FPS: {Mathf.RoundToInt(frameCount / fpsTimer)}";
		fpsTimer = 0f;
		frameCount = 0;
	}

	private void SampleSpeed()
	{
		if (!t_speed.gameObject.activeInHierarchy) return;
		var player = MyPlayer.Instance;
		if (player == null || player.playerMovement == null) { t_speed.text = "SPD: 0"; return; }
		speedSamples[speedSampleIndex++ % speedSamples.Length] = player.playerMovement.GetSpeedHorizontal();
		t_speed.text = $"SPD: {(speedSamples[0] + speedSamples[1]) / 2f:F1}";
	}

	private void SampleRam()
	{
		if (!t_ram.gameObject.activeInHierarchy) return;
		try
		{
			float usedMb = Profiler.GetTotalReservedMemoryLong() / 1048576f;
			int totalMb = SystemInfo.systemMemorySize > 0 ? SystemInfo.systemMemorySize : 16000;
			t_ram.text = $"RAM: {usedMb:N0} MB ({usedMb / totalMb * 100f:N0}%)";
		}
		catch (System.Exception) { t_ram.text = "RAM: ?"; }
	}
}
