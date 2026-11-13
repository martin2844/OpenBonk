using System;
using UnityEngine;
using Assets.Scripts.Managers;

public class ObjectStageMaterial : MonoBehaviour
{
	[Serializable]
	public class StageMaterial
	{
		public int stageIndex;

		public Material material;
	}

	public StageMaterial[] stageMaterials;

	public MeshRenderer meshRenderer;

	private void Start()
	{
		if (stageMaterials == null || meshRenderer == null) return;
		foreach (var entry in stageMaterials)
		{
			if (entry == null || entry.stageIndex != MapController.index) continue;
			meshRenderer.sharedMaterial = entry.material;
			return;
		}
	}

	private void OnValidate()
	{
		if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
	}
}
