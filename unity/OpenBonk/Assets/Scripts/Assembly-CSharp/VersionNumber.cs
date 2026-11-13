using TMPro;
using UnityEngine;

public class VersionNumber : MonoBehaviour
{
	public TextMeshProUGUI t_version;

	private void Start()
	{
		SteamManager.A_Initialized += Refresh;
		Refresh();
	}

	private void OnDestroy()
	{
		SteamManager.A_Initialized -= Refresh;
	}

	private void Refresh()
	{
		if (t_version != null) t_version.text = "version " + Application.version;
	}
}
