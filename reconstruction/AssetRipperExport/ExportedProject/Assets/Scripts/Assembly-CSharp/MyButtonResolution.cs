using TMPro;
using UnityEngine;

public class MyButtonResolution : MyButton
{
	public TextMeshProUGUI t_resolution;

	public GameObject selected;

	private int value;

	private new void Awake()
	{
		base.Awake();
		CurrentSettings.A_ResolutionChanged += OnResolutionChanged;
	}

	private void OnDestroy()
	{
		CurrentSettings.A_ResolutionChanged -= OnResolutionChanged;
	}

	public void SetResolution(Resolution resolution, bool isSelected, int value)
	{
		this.value = value;
		if (t_resolution != null) t_resolution.text = $"{resolution.width}x{resolution.height}";
		if (selected != null) selected.SetActive(isSelected);
	}

	public void ClickButton()
	{
		if (CurrentSettings.Instance == null || SaveManager.Instance == null || SaveManager.Instance.config == null) return;
		var video = SaveManager.Instance.config.cfVideoSettings;
		if (video != null) CurrentSettings.Instance.BetterUpdateCfSettings("resolution", value, video);
	}

	public void Select()
	{
		if (selected != null) selected.SetActive(true);
	}

	public void Deselect()
	{
		if (selected != null) selected.SetActive(false);
	}

	private void OnResolutionChanged(int resIndex)
	{
		if (selected != null) selected.SetActive(value == resIndex);
	}

	public override void StartHover()
	{
	}

	public override void StopHover()
	{
	}

	protected override void OnClick()
	{
	}
}
