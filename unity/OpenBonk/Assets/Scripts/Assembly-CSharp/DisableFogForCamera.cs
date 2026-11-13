using UnityEngine;

public class DisableFogForCamera : MonoBehaviour
{
	private bool originalFogState;

	private void OnPreRender()
	{
		originalFogState = RenderSettings.fog;
		RenderSettings.fog = false;
	}

	private void OnPostRender()
	{
		RenderSettings.fog = originalFogState;
	}
}
