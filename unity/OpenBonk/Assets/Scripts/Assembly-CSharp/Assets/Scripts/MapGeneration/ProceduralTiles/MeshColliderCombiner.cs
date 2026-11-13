using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MapGeneration.ProceduralTiles
{
	public class MeshColliderCombiner
	{
		public static GameObject CombineMeshes(List<GameObject> pieces)
		{
			var colliders = new List<MeshCollider>();
			foreach (GameObject piece in pieces)
			{
				MeshCollider collider = piece.GetComponent<MeshCollider>();
				if (collider != null) colliders.Add(collider);
			}
			var combine = new List<CombineInstance>();
			foreach (MeshCollider collider in colliders)
				for (int submesh = 0; submesh < collider.sharedMesh.subMeshCount; submesh++)
					combine.Add(new CombineInstance { mesh = collider.sharedMesh, subMeshIndex = submesh,
						transform = collider.transform.localToWorldMatrix });
			var result = new GameObject { name = "CombinedCollisionMesh" };
			MeshFilter filter = result.AddComponent<MeshFilter>();
			var mesh = new Mesh();
			mesh.CombineMeshes(combine.ToArray(), true);
			AutoWeld(mesh, 0.25f);
			filter.mesh = mesh;
			result.AddComponent<MeshCollider>().sharedMesh = mesh;
			result.SetActive(true);
			result.layer = LayerMask.NameToLayer("Ground");
			foreach (MeshCollider collider in colliders) Object.Destroy(collider);
			return result;
		}

		private static void AutoWeld(Mesh mesh, float threshold)
		{
			Vector3[] vertices = mesh.vertices;
			var unique = new Vector3[vertices.Length];
			var remap = new int[vertices.Length];
			// The original compares squared distance directly with threshold and
			// chooses the first matching vertex. Index nearby candidates while
			// retaining that exact ordering; fall back for unusual coordinates.
			// The shipped caller supplies .25; keep the original loop for any
			// other threshold, including floating-point edge cases.
			double cellSize = 0.500001;
			bool spatial = threshold == 0.25f;
			var cells = spatial ? new Dictionary<Vector3Int, List<int>>() : null;
			var keys = spatial ? new Vector3Int[vertices.Length] : null;
			if (spatial)
			{
				for (int i = 0; i < vertices.Length; i++)
				{
					double x = System.Math.Floor(vertices[i].x / cellSize);
					double y = System.Math.Floor(vertices[i].y / cellSize);
					double z = System.Math.Floor(vertices[i].z / cellSize);
					if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z) ||
						x < int.MinValue + 1.0 || x > int.MaxValue - 1.0 ||
						y < int.MinValue + 1.0 || y > int.MaxValue - 1.0 ||
						z < int.MinValue + 1.0 || z > int.MaxValue - 1.0)
					{ spatial = false; break; }
					keys[i] = new Vector3Int((int)x, (int)y, (int)z);
				}
			}
			int count = 0;
			for (int i = 0; i < vertices.Length; i++)
			{
				int match = count;
				if (spatial)
				{
					Vector3Int key = keys[i];
					for (int x = -1; x <= 1; x++)
					for (int y = -1; y <= 1; y++)
					for (int z = -1; z <= 1; z++)
						if (cells.TryGetValue(new Vector3Int(key.x + x, key.y + y, key.z + z), out var candidates))
							foreach (int candidate in candidates)
								if (candidate < match && (unique[candidate] - vertices[i]).sqrMagnitude < threshold) match = candidate;
					if (match == count)
					{
						if (!cells.TryGetValue(key, out var bucket)) cells.Add(key, bucket = new List<int>());
						bucket.Add(count);
					}
				}
				else
					for (int j = 0; j < count; j++)
						if ((unique[j] - vertices[i]).sqrMagnitude < threshold) { match = j; break; }
				if (match == count) unique[count++] = vertices[i];
				remap[i] = match;
			}
			int[] triangles = mesh.triangles;
			for (int i = 0; i < triangles.Length; i++) triangles[i] = remap[triangles[i]];
			var welded = new Vector3[count];
			System.Array.Copy(unique, welded, count);
			mesh.Clear();
			mesh.vertices = welded;
			mesh.triangles = triangles;
			mesh.RecalculateNormals();
			mesh.Optimize();
		}

		private static void SimplifyMesh(Mesh mesh)
		{
		}
	}
}
