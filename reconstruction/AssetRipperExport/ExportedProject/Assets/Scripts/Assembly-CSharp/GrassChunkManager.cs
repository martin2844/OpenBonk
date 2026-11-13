using System.Collections.Generic;
using UnityEngine;

public class GrassChunkManager : MonoBehaviour
{
	private struct GrassInstance
	{
		public Vector3 position;

		public float rotationY;
	}

	private struct GrassData
	{
		public Matrix4x4 transform;
	}

	public Mesh grassMesh;

	public Material grassMaterial;

	public int grassPerChunk = 500;

	public int chunkSize = 16;

	public int renderDistance = 3;

	public float yThreshold = 7f;

	public LayerMask layerMask;

	public bool testWithoutPlayer;

	private int currentGrassPerChunk;

	private Vector2Int currentChunk;

	private Dictionary<Vector2Int, List<GrassInstance>> precomputedGrassPositions = new Dictionary<Vector2Int, List<GrassInstance>>();

	private List<GrassData> allGrassDataList = new List<GrassData>();

	private ComputeBuffer allGrassBuffer;

	private ComputeBuffer argsBuffer;

	private readonly uint[] args = new uint[5];

	private Matrix4x4[] fallbackMatrices = new Matrix4x4[0];
	private readonly List<Mesh> fallbackMeshes = new List<Mesh>();
	private bool fallbackDirty = true;
	private bool fallbackMeshErrorLogged;
	private Material fallbackMaterial;
	private Material fallbackSourceMaterial;
#if UNITY_ANDROID && !UNITY_EDITOR
	private int androidGrassDrawChecks;
#endif

	public void Set(Material material, int grassPerChunk)
	{
		if (grassMaterial != material && fallbackMaterial != null)
		{
			DestroyFallbackObject(fallbackMaterial);
			fallbackMaterial = null;
			fallbackSourceMaterial = null;
		}
		grassMaterial = material;
		this.grassPerChunk = grassPerChunk;
	}

	private void Awake()
	{
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
		currentGrassPerChunk = grassPerChunk;
		if (SystemInfo.supportsInstancing && SystemInfo.supportsComputeShaders)
			argsBuffer = new ComputeBuffer(args.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
	}

	private void Start()
	{
		if (SaveManager.Instance != null && SaveManager.Instance.config != null && SaveManager.Instance.config.cfVideoSettings != null)
			UpdateGrassQuality(SaveManager.Instance.config.cfVideoSettings.grass_quality);
	}

	private void Update()
	{
		if (!testWithoutPlayer)
		{
			if (PlayerCamera.Instance == null) return;
			transform.position = PlayerCamera.Instance.transform.position;
		}
		if (chunkSize <= 0) return;
		Vector2Int chunk = GetPlayerChunk();
		if (chunk != currentChunk) { currentChunk = chunk; UpdateChunks(); }
		RenderGrass();
		if (grassMaterial != null)
		{
			grassMaterial.SetVector("_PlayerPos", transform.position);
			grassMaterial.SetFloat("_RenderDistance", renderDistance * chunkSize);
		}
	}

	private Vector2Int GetPlayerChunk()
	{
		return new Vector2Int(Mathf.FloorToInt(transform.position.x / chunkSize), Mathf.FloorToInt(transform.position.z / chunkSize));
	}

	private void UpdateChunks()
	{
		var remove = new List<Vector2Int>();
		foreach (var chunk in precomputedGrassPositions.Keys)
			if (Vector2Int.Distance(chunk, currentChunk) > renderDistance) remove.Add(chunk);
		foreach (var chunk in remove) precomputedGrassPositions.Remove(chunk);
		for (int x = -renderDistance; x <= renderDistance; x++)
			for (int z = -renderDistance; z <= renderDistance; z++)
			{
				var chunk = currentChunk + new Vector2Int(x, z);
				if (!precomputedGrassPositions.ContainsKey(chunk)) GenerateGrassForChunk(chunk);
			}
		RebuildGrassBuffer();
	}

	private void GenerateGrassForChunk(Vector2Int chunkCoord)
	{
		var positions = new List<GrassInstance>();
		for (int i = 0; i < currentGrassPerChunk; i++)
		{
			float x = chunkCoord.x * chunkSize + Random.Range(0f, (float)chunkSize);
			float z = chunkCoord.y * chunkSize + Random.Range(0f, (float)chunkSize);
			if (GetTerrainHeight(x, z, out float height))
				positions.Add(new GrassInstance { position = new Vector3(x, height, z), rotationY = Random.Range(0f, 360f) });
		}
		precomputedGrassPositions[chunkCoord] = positions;
	}

	private void RebuildGrassBuffer()
	{
		allGrassDataList.Clear();
		foreach (var chunk in precomputedGrassPositions.Values)
			foreach (var grass in chunk)
				allGrassDataList.Add(new GrassData { transform = Matrix4x4.TRS(grass.position, Quaternion.Euler(0f, grass.rotationY, 0f), Vector3.one) });
		fallbackMatrices = new Matrix4x4[allGrassDataList.Count];
		for (int i = 0; i < fallbackMatrices.Length; i++) fallbackMatrices[i] = allGrassDataList[i].transform;
		fallbackDirty = true;
		ClearFallbackMeshes();
		if (allGrassBuffer != null) { allGrassBuffer.Release(); allGrassBuffer = null; }
		if (allGrassDataList.Count == 0 || grassMesh == null || !SystemInfo.supportsComputeShaders || !SystemInfo.supportsInstancing) return;
		allGrassBuffer = new ComputeBuffer(allGrassDataList.Count, 64);
		allGrassBuffer.SetData(allGrassDataList);
		if (argsBuffer == null) argsBuffer = new ComputeBuffer(args.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
		args[0] = grassMesh.GetIndexCount(0);
		args[1] = (uint)allGrassDataList.Count;
		args[2] = grassMesh.GetIndexStart(0);
		args[3] = (uint)grassMesh.GetBaseVertex(0);
		args[4] = 0;
		argsBuffer.SetData(args);
	}

	private void RenderGrass()
	{
		if (grassMesh == null || grassMaterial == null || allGrassDataList.Count == 0) return;
		if (allGrassBuffer == null || argsBuffer == null || !grassMaterial.shader.isSupported)
		{
			RenderGrassFallback();
			return;
		}
		var properties = new MaterialPropertyBlock();
		properties.SetBuffer("_GrassBuffer", allGrassBuffer);
		grassMaterial.SetBuffer("_GrassBuffer", allGrassBuffer);
		float size = renderDistance * chunkSize;
		Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMaterial, new Bounds(transform.position, new Vector3(size, 25f, size)), argsBuffer, 0, properties);
	}

	private void RenderGrassFallback()
	{
		if (fallbackMaterial == null || fallbackSourceMaterial != grassMaterial)
		{
			if (fallbackMaterial != null) DestroyFallbackObject(fallbackMaterial);
			Shader shader = Resources.Load<Shader>("GrassBatched");
			if (shader == null || !shader.isSupported) return;
			fallbackMaterial = new Material(grassMaterial) { shader = shader, enableInstancing = false };
			fallbackSourceMaterial = grassMaterial;
		}
		if (fallbackDirty) RebuildFallbackMeshes();
		fallbackMaterial.SetVector("_PlayerPos", transform.position);
		fallbackMaterial.SetFloat("_RenderDistance", renderDistance * chunkSize);
#if UNITY_ANDROID && !UNITY_EDITOR
		if (androidGrassDrawChecks > 0)
		{
			androidGrassDrawChecks--;
			var cam = PlayerCamera.Instance != null ? PlayerCamera.Instance.camera : null;
			Vector3 position = fallbackMatrices.Length > 0 ? (Vector3)fallbackMatrices[0].GetColumn(3) : Vector3.zero;
			Debug.Log($"Grass draw diagnostic: matrices={fallbackMatrices.Length}, cameraViewport={(cam != null ? cam.WorldToViewportPoint(position).ToString("F2") : "none")}, world={position}, debug={fallbackMaterial.GetFloat("_GrassDebug")}, meshBounds={grassMesh.bounds}, instancing={fallbackMaterial.enableInstancing}, range={renderDistance * chunkSize}");
		}
#endif
		foreach (var mesh in fallbackMeshes) Graphics.DrawMesh(mesh, Matrix4x4.identity, fallbackMaterial, 0);
	}

	private static void DestroyFallbackObject(Object item)
	{
		if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
	}

	private void ClearFallbackMeshes()
	{
		foreach (var mesh in fallbackMeshes) if (mesh != null) DestroyFallbackObject(mesh);
		fallbackMeshes.Clear();
	}

	// Bake each transform into world-space positions and keep source vertex height in UV1.
	// A 16-bit index buffer is available on GLES devices that cannot use 32-bit indices.
	private void RebuildFallbackMeshes()
	{
		ClearFallbackMeshes();
		fallbackDirty = false;
		if (grassMesh == null || fallbackMatrices.Length == 0) return;
		if (!grassMesh.isReadable)
		{
			if (!fallbackMeshErrorLogged) Debug.LogError("Grass fallback needs readable GrassQuads mesh data");
			fallbackMeshErrorLogged = true;
			return;
		}
		var sourceVertices = grassMesh.vertices;
		var sourceUv = grassMesh.uv;
		var sourceIndices = grassMesh.GetTriangles(0);
		int verticesPerInstance = sourceVertices.Length;
		if (verticesPerInstance == 0 || verticesPerInstance > 65535 || sourceUv.Length != verticesPerInstance || sourceIndices.Length == 0)
		{
			if (!fallbackMeshErrorLogged) Debug.LogError("Grass fallback source mesh has invalid geometry or UVs");
			fallbackMeshErrorLogged = true;
			return;
		}
		int instancesPerMesh = 65535 / verticesPerInstance;
		float windMargin = grassMaterial != null && grassMaterial.HasProperty("_WindStrength") ? Mathf.Abs(grassMaterial.GetFloat("_WindStrength")) * 2f : 2f;
		for (int offset = 0; offset < fallbackMatrices.Length; offset += instancesPerMesh)
		{
			int count = Mathf.Min(instancesPerMesh, fallbackMatrices.Length - offset);
			var vertices = new Vector3[count * verticesPerInstance];
			var uv = new Vector2[vertices.Length];
			var localHeight = new Vector2[vertices.Length];
			var indices = new int[count * sourceIndices.Length];
			for (int i = 0; i < count; i++)
			{
				int first = i * verticesPerInstance;
				var matrix = fallbackMatrices[offset + i];
				for (int v = 0; v < verticesPerInstance; v++)
				{
					vertices[first + v] = matrix.MultiplyPoint3x4(sourceVertices[v]);
					uv[first + v] = sourceUv[v];
					localHeight[first + v] = new Vector2(sourceVertices[v].y, 0f);
				}
				for (int t = 0; t < sourceIndices.Length; t++) indices[i * sourceIndices.Length + t] = first + sourceIndices[t];
			}
			var mesh = new Mesh { name = "Grass fallback batch" };
			mesh.vertices = vertices; mesh.uv = uv; mesh.uv2 = localHeight; mesh.triangles = indices;
			mesh.RecalculateBounds(); var bounds = mesh.bounds; bounds.Expand(windMargin); mesh.bounds = bounds;
			fallbackMeshes.Add(mesh);
		}
	}

	private bool GetTerrainHeight(float x, float z, out float height)
	{
		height = 0f;
		if (!Physics.Raycast(new Vector3(x, 200f, z), Vector3.down, out RaycastHit hit, 9999f, layerMask)) return false;
		if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Ground") || hit.normal.y < 0.8746197f || hit.point.y < yThreshold) return false;
		height = hit.point.y;
		return true;
	}

	private void UpdateGrassQuality(int quality)
	{
		if (quality == 0) currentGrassPerChunk = Mathf.RoundToInt(grassPerChunk * 0.05f);
		else if (quality == 1) currentGrassPerChunk = Mathf.RoundToInt(grassPerChunk * 0.35f);
		else if (quality == 2) currentGrassPerChunk = grassPerChunk;
		precomputedGrassPositions.Clear();
		UpdateChunks();
	}

	private void OnSettingUpdated(string setting, object oldValue, object newValue)
	{
		if (setting == "grass_quality" && newValue is int quality) UpdateGrassQuality(quality);
	}

	private void OnDestroy()
	{
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
		if (allGrassBuffer != null) { allGrassBuffer.Release(); allGrassBuffer = null; }
		if (argsBuffer != null) { argsBuffer.Release(); argsBuffer = null; }
		ClearFallbackMeshes();
		if (fallbackMaterial != null) { DestroyFallbackObject(fallbackMaterial); fallbackMaterial = null; }
	}

	private void OnDrawGizmosSelected()
	{
	}

#if UNITY_ANDROID && !UNITY_EDITOR
	public void PlaceAndroidDiagnosticGrass()
	{
		var camera = PlayerCamera.Instance != null ? PlayerCamera.Instance.camera : null;
		if (camera == null || fallbackMaterial == null) { Debug.Log("Grass forced diagnostic: camera or fallback unavailable"); return; }
		fallbackMatrices = new Matrix4x4[9];
		androidGrassDrawChecks = 5;
		for (int i = 0; i < fallbackMatrices.Length; i++)
		{
			Vector3 position = camera.transform.position + camera.transform.forward * 8f + camera.transform.right * ((i % 3 - 1) * 2f) + camera.transform.up * ((i / 3 - 1) * 2f);
			fallbackMatrices[i] = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * 2f);
		}
		fallbackDirty = true;
		Debug.Log("Grass forced diagnostic: nine enlarged instances placed in front of camera");
	}

	public void HighlightAndroidDiagnostic()
	{
		if (fallbackMaterial == null) { Debug.Log("Grass highlight diagnostic: fallback material unavailable"); return; }
		fallbackMaterial.SetFloat("_GrassDebug", 1f);
		Debug.Log("Grass highlight diagnostic: magenta enabled on fallback material");
	}

	public void LogAndroidDiagnostic()
	{
		Debug.Log($"Grass diagnostic: active={gameObject.activeInHierarchy}, enabled={enabled}, position={transform.position}, chunk={currentChunk}, chunks={precomputedGrassPositions.Count}, instances={allGrassDataList.Count}, density={currentGrassPerChunk}, buffer={(allGrassBuffer != null)}, args={(argsBuffer != null)}, compute={SystemInfo.supportsComputeShaders}, instancing={SystemInfo.supportsInstancing}, material={(grassMaterial != null ? grassMaterial.name : "none")}, shaderSupported={(grassMaterial != null && grassMaterial.shader != null && grassMaterial.shader.isSupported)}, fallback={(fallbackMaterial != null)}, camera={(PlayerCamera.Instance != null)}");
		if (grassMaterial != null && fallbackMaterial != null)
			Debug.Log($"Grass diagnostic colors: sourceBottom={grassMaterial.GetColor("_BottomColor").ToString("F4")}, fallbackBottom={fallbackMaterial.GetColor("_BottomColor").ToString("F4")}, sourceTop={grassMaterial.GetColor("_TopColor").ToString("F4")}, fallbackTop={fallbackMaterial.GetColor("_TopColor").ToString("F4")}, sourceTexture={(grassMaterial.mainTexture != null ? grassMaterial.mainTexture.name : "none")}, fallbackTexture={(fallbackMaterial.mainTexture != null ? fallbackMaterial.mainTexture.name : "none")}, fog={RenderSettings.fog}, fogMode={RenderSettings.fogMode}, fogColor={RenderSettings.fogColor.ToString("F4")}");
		var camera = PlayerCamera.Instance != null ? PlayerCamera.Instance.camera : null;
		float minY = float.PositiveInfinity, maxY = float.NegativeInfinity, nearest = float.PositiveInfinity;
		int inView = 0, inRange = 0, nearCameraHeight = 0, nearCameraHeightInView = 0;
		foreach (var data in allGrassDataList)
		{
			Vector3 p = data.transform.GetColumn(3);
			minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
			float distance = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(transform.position.x, transform.position.z));
			nearest = Mathf.Min(nearest, distance);
			if (distance <= renderDistance * chunkSize) inRange++;
			bool nearHeight = Mathf.Abs(p.y - transform.position.y) < 10f;
			if (nearHeight && distance <= renderDistance * chunkSize) nearCameraHeight++;
			if (camera != null) { Vector3 view = camera.WorldToViewportPoint(p); if (view.z > 0 && view.x >= 0 && view.x <= 1 && view.y >= 0 && view.y <= 1 && distance <= renderDistance * chunkSize) { inView++; if (nearHeight) nearCameraHeightInView++; } }
		}
		Debug.Log($"Grass diagnostic positions: y={minY:F2}..{maxY:F2}, nearestXZ={nearest:F2}, inRange={inRange}, inView={inView}, nearHeight={nearCameraHeight}, nearHeightInView={nearCameraHeightInView}, mesh={(grassMesh != null ? grassMesh.name : "none")}, fallbackSupported={(fallbackMaterial != null && fallbackMaterial.shader.isSupported)}, texture={(fallbackMaterial != null && fallbackMaterial.mainTexture != null ? fallbackMaterial.mainTexture.name : "none")}");
	}
#endif
}
