using UnityEngine;

[ExecuteInEditMode]
public class ColorblindSimulationImageEffect : MonoBehaviour
{
	public Material material;

	public ColorDeficiencyType selectedDeficiency;

	private ColorDeficiencyType currentDeficiency;

	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
	}

	private void Update()
	{
	}
}
