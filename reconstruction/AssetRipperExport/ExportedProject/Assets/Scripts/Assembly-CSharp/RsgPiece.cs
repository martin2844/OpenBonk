using UnityEngine;

public class RsgPiece : MonoBehaviour
{
	public bool mirror;

	public bool reverse;

	public Transform start;

	public Transform end;

	public Transform lowestPoint;

	public GameObject children;

	public MeshFilter meshFilter;

	public MeshCollider newCollider;

	[Range(0f, 1f)]
	public float complexity;

	public int traverseTime;

	private BoxCollider boundsCollider;

	private Bounds bounds;

	private bool mirrored;

	private void OnValidate()
	{
	}

	public Bounds GetBounds()
	{
		if (boundsCollider == null)
		{
			boundsCollider = gameObject.AddComponent<BoxCollider>();
			if (mirrored)
			{
				Vector3 center = boundsCollider.center;
				center.x = -center.x;
				boundsCollider.center = center;
			}
			bounds = boundsCollider.bounds;
			bounds.Expand(-0.05f);
			boundsCollider.enabled = false;
		}
		return bounds;
	}

	public float GetLowestCordY()
	{
		return lowestPoint != null ? lowestPoint.position.y : Mathf.Min(start.position.y, end.position.y);
	}

	public void Mirror()
	{
		mirrored = true;
		children.transform.localScale = new Vector3(-1f, 1f, 1f);
	}

	public void SetCollider()
	{
		newCollider.sharedMesh = mirrored ? MirrorMesh(meshFilter.sharedMesh) : meshFilter.sharedMesh;
	}

	private Mesh MirrorMesh(Mesh mesh)
	{
		Mesh result = new Mesh();
		Vector3[] vertices = mesh.vertices;
		for (int i = 0; i < vertices.Length; i++)
			vertices[i].x = -vertices[i].x;
		result.vertices = vertices;
		int[] triangles = mesh.triangles;
		for (int i = 0; i < triangles.Length; i += 3)
		{
			int first = triangles[i];
			triangles[i] = triangles[i + 2];
			triangles[i + 2] = first;
		}
		result.triangles = triangles;
		result.RecalculateNormals();
		result.RecalculateBounds();
		return result;
	}
}
