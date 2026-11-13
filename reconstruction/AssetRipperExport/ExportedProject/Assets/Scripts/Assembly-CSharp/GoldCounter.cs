using TMPro;
using UnityEngine;
using Assets.Scripts.Saves___Serialization.SaveFiles;

public class GoldCounter : MonoBehaviour
{
	public TextMeshProUGUI t_silver;

	private void Start()
	{
		if (SaveManager.Instance != null && SaveManager.Instance.progression != null) Refresh(0);
		ProgressionSaveFile.A_SilverChanged += Refresh;
	}

	private void OnDestroy()
	{
		ProgressionSaveFile.A_SilverChanged -= Refresh;
	}

	private void Refresh(int delta)
	{
		t_silver.text = $"<sprite name=silver> {SaveManager.Instance.progression.silver:N0}";
		t_silver.transform.localScale = Vector3.one * 1.6f;
	}

	private void Update()
	{
		t_silver.transform.localScale = Vector3.Lerp(t_silver.transform.localScale, Vector3.one, Time.deltaTime * 8f);
	}
}
