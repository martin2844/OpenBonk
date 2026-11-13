using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FpsCounter : MonoBehaviour
{
	public TextMeshProUGUI text;

	private List<float> samples;

	private void Awake()
	{
		samples = new List<float>();
		InvokeRepeating(nameof(UpdateFps), 0.5f, 0.5f);
	}

	private void UpdateFps()
	{
		if (samples == null || samples.Count == 0 || text == null) return;
		float total = 0f;
		foreach (float sample in samples) total += sample;
		text.text = Mathf.RoundToInt(total / samples.Count).ToString();
		samples.Clear();
	}

	private void Update()
	{
		if (Time.smoothDeltaTime > 0f)
			samples.Add(Mathf.RoundToInt(1f / Time.smoothDeltaTime));
	}
}
