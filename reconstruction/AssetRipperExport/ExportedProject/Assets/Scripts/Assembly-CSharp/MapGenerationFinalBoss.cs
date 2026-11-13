using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using UnityEngine;

public class MapGenerationFinalBoss : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CGenerateMap_003Ed__15 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public MapGenerationFinalBoss _003C_003E4__this;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CGenerateMap_003Ed__15(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	public MapData testMapData;

	public StageData testStageData;

	public GameManager gameManager;

	public Transform spawnTransform;

	public GrassChunkManager grassRenderer;

	public GameObject colliderBox;

	public MinimapMesh minimapMesh;

	public GameObject spawnPortal;

	public MeshRenderer worldMeshRenderer;

	public MeshFilter worldMeshFilter;

	public MeshRenderer[] renderersForMaterial;

	public Light sunLight;

	private Vector3 spawnPosition;

	private Vector3 spawnDirection;

	private void Awake()
	{
		StartCoroutine(GenerateMap());
	}

	private IEnumerator GenerateMap()
	{
		UnityEngine.Random.Range(0, int.MaxValue);
		gameManager.CreateInstances();
		StageData stage = MapController.currentStage;
		MapData map = MapController.currentMap;
		spawnPosition = spawnTransform.position;
		spawnDirection = spawnTransform.forward;
		worldMeshRenderer.sharedMaterial = map.finalStageMaterial;
		foreach (MeshRenderer renderer in renderersForMaterial)
			renderer.sharedMaterial = map.finalStageMaterial;
		if (stage.grassMaterial != null && stage.grassPerChunk > 0)
		{
			grassRenderer.Set(stage.grassMaterial, stage.grassPerChunk);
			grassRenderer.gameObject.SetActive(true);
		}
		Vector3 size = Vector3.one * 300f;
		colliderBox.transform.position = Vector3.down * (size.y * 0.5f + 5f);
		colliderBox.transform.localScale = new Vector3(size.x, size.y + 200f, size.z);
		MapInfo.mapBoundsLower = -size * 0.5f;
		MapInfo.mapBoundsUpper = size * 0.5f;
		stage.ApplyFogAndSky(sunLight);
		stage.SpawnParticles();
		RenderSettings.fogDensity = 0.005f;
		minimapMesh.Set(worldMeshFilter.sharedMesh, stage.fogColor);
		yield break;
	}

	public GameObject Spawn()
	{
		GameObject portal = Instantiate(spawnPortal, spawnPosition, Quaternion.LookRotation(-spawnDirection));
		portal.GetComponent<SpawnPlayerPortal>().StartPortal();
		return portal;
	}
}
