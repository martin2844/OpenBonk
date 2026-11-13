using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class Outline : MonoBehaviour
{
	public enum Mode
	{
		OutlineAll = 0,
		OutlineVisible = 1,
		OutlineHidden = 2,
		OutlineAndSilhouette = 3,
		SilhouetteOnly = 4
	}

	[Serializable]
	private class ListVector3
	{
		public List<Vector3> data;
	}

	private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

	[SerializeField]
	private Mode outlineMode;

	[SerializeField]
	private Color outlineColor = Color.white;

	[SerializeField]
	[Range(0f, 10f)]
	private float outlineWidth = 2f;

	[Header("Optional")]
	[SerializeField]
	[Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
	private bool precomputeOutline;

	[SerializeField]
	[HideInInspector]
	private List<Mesh> bakeKeys = new List<Mesh>();

	[SerializeField]
	[HideInInspector]
	private List<ListVector3> bakeValues = new List<ListVector3>();

	private Renderer[] renderers;

	private Material outlineMaskMaterial;

	private Material outlineFillMaterial;

	private bool needsUpdate;
	private Renderer proxySource;
	private Renderer[] proxyRenderers;
	private bool externalSubmeshesRequested;

	public Mode OutlineMode
	{
		get
		{
			return outlineMode;
		}
		set
		{
			outlineMode = value;
			needsUpdate = true;
		}
	}

	public Color OutlineColor
	{
		get
		{
			return outlineColor;
		}
		set
		{
			outlineColor = value;
			needsUpdate = true;
		}
	}

	public float OutlineWidth
	{
		get
		{
			return outlineWidth;
		}
		set
		{
			outlineWidth = value;
			needsUpdate = true;
		}
	}

	private void Awake()
	{
		if (gameObject.CompareTag("NoOutline")) { enabled = false; return; }
		renderers = new[] { GetComponent<Renderer>() };
		var mask = Resources.Load<Material>("materials/OutlineMask");
		var fill = Resources.Load<Material>("materials/OutlineFill");
		if (mask == null || fill == null) { enabled = false; return; }
		outlineMaskMaterial = Instantiate(mask);
		outlineFillMaterial = Instantiate(fill);
		outlineMaskMaterial.name = "OutlineMask (Instance)";
		outlineFillMaterial.name = "OutlineFill (Instance)";
		LoadSmoothNormals();
		needsUpdate = true;
	}

	private void OnEnable()
	{
		if (renderers == null || outlineMaskMaterial == null || outlineFillMaterial == null) return;
		if (proxyRenderers != null)
		{
			foreach (var proxy in proxyRenderers) if (proxy != null) proxy.gameObject.SetActive(true);
			return;
		}
		foreach (var item in renderers)
		{
			if (item == null) continue;
			var materials = new List<Material>(item.sharedMaterials) { outlineMaskMaterial, outlineFillMaterial };
			item.materials = materials.ToArray();
		}
	}

	private void OnValidate()
	{
		needsUpdate = true;
	}

	private void Start()
	{
		// An Outline added while its model part is inactive has not run Awake yet.
		if (externalSubmeshesRequested) ConfigureExternalSubmeshes();
	}

	private void Update()
	{
		if (!needsUpdate) return;
		needsUpdate = false;
		UpdateMaterialProperties();
	}

	private void OnDisable()
	{
		if (proxyRenderers != null)
		{
			foreach (var proxy in proxyRenderers) if (proxy != null) proxy.gameObject.SetActive(false);
			return;
		}
		if (renderers == null) return;
		foreach (var item in renderers)
		{
			if (item == null) continue;
			var materials = new List<Material>(item.sharedMaterials);
			materials.Remove(outlineMaskMaterial);
			materials.Remove(outlineFillMaterial);
			item.materials = materials.ToArray();
		}
	}

	private void OnDestroy()
	{
		if (proxyRenderers != null) foreach (var proxy in proxyRenderers) if (proxy != null) Destroy(proxy.gameObject);
		if (outlineMaskMaterial != null) Destroy(outlineMaskMaterial);
		if (outlineFillMaterial != null) Destroy(outlineFillMaterial);
	}

	// Non-readable external meshes cannot have their submeshes merged. Draw the
	// mask and fill on separate renderers with a material for every submesh.
	public void ConfigureExternalSubmeshes()
	{
		externalSubmeshesRequested = true;
		if (proxyRenderers != null || outlineMaskMaterial == null || outlineFillMaterial == null) return;
		var source = GetComponent<Renderer>();
		var skin = source as SkinnedMeshRenderer;
		var filter = source is MeshRenderer ? GetComponent<MeshFilter>() : null;
		var mesh = skin != null ? skin.sharedMesh : filter != null ? filter.sharedMesh : null;
		if (source == null || mesh == null || mesh.isReadable || mesh.subMeshCount <= 1) return;
		bool wasEnabled = enabled;
		if (wasEnabled) enabled = false; // Remove the single-submesh material pass first.
		proxySource = source;
		proxyRenderers = new Renderer[2];
		for (int i = 0; i < 2; i++)
		{
			var child = new GameObject(i == 0 ? "OpenBonkOutlineMask" : "OpenBonkOutlineFill");
			child.layer = gameObject.layer; child.transform.SetParent(transform, false);
			Renderer proxy;
			if (skin != null)
			{
				var skinnedProxy = child.AddComponent<SkinnedMeshRenderer>();
				skinnedProxy.sharedMesh = mesh; skinnedProxy.bones = skin.bones; skinnedProxy.rootBone = skin.rootBone;
				skinnedProxy.localBounds = skin.localBounds; skinnedProxy.quality = skin.quality;
				skinnedProxy.updateWhenOffscreen = skin.updateWhenOffscreen;
				proxy = skinnedProxy;
			}
			else
			{
				child.AddComponent<MeshFilter>().sharedMesh = mesh;
				proxy = child.AddComponent<MeshRenderer>();
			}
			proxy.shadowCastingMode = ShadowCastingMode.Off; proxy.receiveShadows = false;
			proxy.sortingLayerID = source.sortingLayerID; proxy.sortingOrder = source.sortingOrder;
			var materials = new Material[mesh.subMeshCount];
			for (int j = 0; j < materials.Length; j++) materials[j] = i == 0 ? outlineMaskMaterial : outlineFillMaterial;
			proxy.sharedMaterials = materials; proxyRenderers[i] = proxy;
			child.SetActive(false);
		}
		if (wasEnabled) enabled = true;
	}

	private void LateUpdate()
	{
		if (proxyRenderers == null || proxySource == null) return;
		var source = proxySource as SkinnedMeshRenderer;
		foreach (var item in proxyRenderers)
		{
			if (item == null) continue;
			item.enabled = proxySource.enabled;
			var proxy = item as SkinnedMeshRenderer;
			if (proxy == null || source == null) continue;
			if (source.sharedMesh != null)
				for (int i = 0; i < source.sharedMesh.blendShapeCount; i++) proxy.SetBlendShapeWeight(i, source.GetBlendShapeWeight(i));
		}
	}

	private void Bake()
	{
	}

	public void LoadSmoothNormals()
	{
		foreach (var filter in GetComponentsInChildren<MeshFilter>())
		{
			if (filter.CompareTag("NoOutline")) continue;
			Mesh mesh = filter.sharedMesh;
			if (mesh == null || !mesh.isReadable || !registeredMeshes.Add(mesh)) continue;
			int index = bakeKeys.IndexOf(mesh);
			mesh.SetUVs(3, index >= 0 ? bakeValues[index].data : SmoothNormals(mesh));
			var meshRenderer = filter.GetComponent<Renderer>();
			if (meshRenderer != null) CombineSubmeshes(mesh, meshRenderer.sharedMaterials);
		}
		foreach (var skinned in GetComponentsInChildren<SkinnedMeshRenderer>())
		{
			if (skinned.CompareTag("NoOutline")) continue;
			Mesh mesh = skinned.sharedMesh;
			if (mesh == null || !mesh.isReadable || !registeredMeshes.Add(mesh)) continue;
			mesh.uv4 = new Vector2[mesh.vertexCount];
			CombineSubmeshes(mesh, skinned.sharedMaterials);
		}
	}

	private List<Vector3> SmoothNormals(Mesh mesh)
	{
		var vertices = mesh.vertices;
		var normals = mesh.normals;
		var result = new List<Vector3>(normals);
		foreach (var group in vertices.Select((vertex, index) => new { vertex, index }).GroupBy(v => v.vertex))
		{
			if (group.Count() <= 1) continue;
			Vector3 sum = Vector3.zero;
			foreach (var item in group) sum += normals[item.index];
			sum.Normalize();
			foreach (var item in group) result[item.index] = sum;
		}
		return result;
	}

	private void CombineSubmeshes(Mesh mesh, Material[] materials)
	{
		if (mesh.subMeshCount == 1 || mesh.subMeshCount > materials.Length) return;
		mesh.subMeshCount++;
		mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
	}

	private void UpdateMaterialProperties()
	{
		if (outlineMaskMaterial == null || outlineFillMaterial == null) return;
		outlineFillMaterial.SetColor("_OutlineColor", outlineColor);
		CompareFunction maskTest, fillTest;
		switch (outlineMode)
		{
			case Mode.OutlineVisible: maskTest = CompareFunction.Always; fillTest = CompareFunction.LessEqual; break;
			case Mode.OutlineHidden: maskTest = CompareFunction.Always; fillTest = CompareFunction.Greater; break;
			case Mode.OutlineAndSilhouette: maskTest = CompareFunction.LessEqual; fillTest = CompareFunction.Always; break;
			case Mode.SilhouetteOnly: maskTest = CompareFunction.LessEqual; fillTest = CompareFunction.Greater; break;
			default: maskTest = CompareFunction.Always; fillTest = CompareFunction.Always; break;
		}
		outlineMaskMaterial.SetFloat("_ZTest", (float)maskTest);
		outlineFillMaterial.SetFloat("_ZTest", (float)fillTest);
		outlineFillMaterial.SetFloat("_OutlineWidth", outlineMode == Mode.SilhouetteOnly ? 0f : outlineWidth);
	}
}
