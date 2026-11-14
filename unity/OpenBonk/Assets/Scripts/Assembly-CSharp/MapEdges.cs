using UnityEngine;

public class MapEdges : MonoBehaviour
{
	public MeshRenderer edgeBot;

	public MeshRenderer edgeTop;

	public void Set(Vector3 worldCenter, Vector3 worldSize, StageData stageData)
	{
		gameObject.SetActive(true);
		edgeBot.gameObject.SetActive(true);
		edgeTop.gameObject.SetActive(true);
		edgeBot.transform.position = worldCenter - Vector3.up * (worldSize.y * 0.5f);
		edgeTop.transform.position = worldCenter + Vector3.up * (worldSize.y * 0.5f);
		edgeBot.transform.localScale = worldSize * 0.5f;
		Vector3 topScale = edgeTop.transform.localScale;
		topScale.x = worldSize.x * 0.5f;
		topScale.z = worldSize.z * 0.5f;
		edgeTop.transform.localScale = topScale;
		edgeBot.sharedMaterial = stageData.triplanarMaterial;
		edgeTop.sharedMaterial = stageData.triplanarMaterial;
#if UNITY_ANDROID
		// The enlarged upper boundary produces stray outline lines across the sky.
		// Override only this renderer; the terrain and lower edge share its material.
		var properties = new MaterialPropertyBlock();
		edgeTop.GetPropertyBlock(properties);
		properties.SetFloat("_OutlineWidth", 0f);
		edgeTop.SetPropertyBlock(properties);
#endif
	}
}
