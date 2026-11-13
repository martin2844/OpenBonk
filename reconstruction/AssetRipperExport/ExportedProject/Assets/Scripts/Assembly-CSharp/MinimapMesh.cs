using UnityEngine;
using Assets.Scripts.MapGeneration;

public class MinimapMesh : MonoBehaviour
{
	public MeshRenderer meshRenderer;

	public MeshFilter meshFilter;

	public void Set(Mesh mesh, Color fogColor)
	{
		gameObject.SetActive(true);
		meshFilter.sharedMesh = mesh;
		meshRenderer.sharedMaterial.SetFloat("_MinDistance", MapInfo.mapBoundsLower.y);
		meshRenderer.sharedMaterial.SetFloat("_MaxDistance", MapInfo.mapBoundsUpper.y);
	}

	private Color GetSaturatedColor(Color color)
	{
		Color.RGBToHSV(color, out float hue, out _, out float value);
		return Color.HSVToRGB(hue, 1f, value, true);
	}
}
