using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class RsgPiecePortCheck
{
	public static void Run()
	{
		GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
		GameObject controllerObject = new GameObject("controller");
		Mesh mirroredMesh = null;
		try
		{
			RsgPiece piece = root.AddComponent<RsgPiece>();
			piece.meshFilter = root.GetComponent<MeshFilter>();
			piece.newCollider = root.AddComponent<MeshCollider>();
			piece.children = new GameObject("children");
			piece.children.transform.SetParent(root.transform);
			piece.start = new GameObject("original start").transform;
			piece.start.SetParent(root.transform);
			piece.start.localPosition = new Vector3(0, 2, 0);
			piece.end = new GameObject("original end").transform;
			piece.end.SetParent(root.transform);
			piece.end.localPosition = new Vector3(0, 3, 1);
			Check(Mathf.Approximately(piece.GetLowestCordY(), 2), "lowest endpoint");
			piece.lowestPoint = piece.end;
			Check(Mathf.Approximately(piece.GetLowestCordY(), 3), "explicit lowest point");
			piece.SetCollider();
			Check(piece.newCollider.sharedMesh == piece.meshFilter.sharedMesh, "unmirrored collider");
			Vector3[] original = piece.meshFilter.sharedMesh.vertices;
			int[] originalTriangles = piece.meshFilter.sharedMesh.triangles;
			piece.Mirror();
			piece.SetCollider();
			mirroredMesh = piece.newCollider.sharedMesh;
			Check(piece.children.transform.localScale == new Vector3(-1, 1, 1), "mirrored children");
			Check(mirroredMesh != piece.meshFilter.sharedMesh, "separate mirrored mesh");
			Vector3[] vertices = mirroredMesh.vertices;
			int[] triangles = mirroredMesh.triangles;
			for (int i = 0; i < vertices.Length; i++)
				Check(vertices[i] == new Vector3(-original[i].x, original[i].y, original[i].z), "mirrored vertex");
			for (int i = 0; i < triangles.Length; i += 3)
				Check(triangles[i] == originalTriangles[i + 2] && triangles[i + 1] == originalTriangles[i + 1] && triangles[i + 2] == originalTriangles[i], "triangle winding");
			Bounds initial = piece.GetBounds();
			Check(Vector3.Distance(initial.size, Vector3.one * 0.95f) < 0.001f, "shrunk bounds");
			root.transform.position = new Vector3(10, 0, 0);
			Check(piece.GetBounds() == initial, "cached bounds");
			RsgController controller = controllerObject.AddComponent<RsgController>();
			FieldInfo bounds = typeof(RsgController).GetField("bounds", BindingFlags.Instance | BindingFlags.NonPublic);
			bounds.SetValue(controller, new List<Bounds>());
			MethodInfo overlap = Method("BoundsOverlap");
			Check(!(bool)overlap.Invoke(controller, new object[] { piece }), "empty bounds");
			Method("BoundsAdd").Invoke(controller, new object[] { piece });
			Check((bool)overlap.Invoke(controller, new object[] { piece }), "overlapping bounds");
			Transform oldStart = piece.start;
			Transform oldEnd = piece.end;
			Method("ReversePiece").Invoke(controller, new object[] { piece });
			Check(piece.start == oldEnd && piece.end == oldStart && piece.start.name == "start" && piece.end.name == "end", "reversed endpoints");
			Check(Vector3.Distance(piece.start.forward, Vector3.back) < 0.001f, "reversed orientation");
			Debug.Log("RsgPiecePortCheck passed");
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
			UnityEngine.Object.DestroyImmediate(controllerObject);
			if (mirroredMesh != null) UnityEngine.Object.DestroyImmediate(mirroredMesh);
		}
	}

	private static MethodInfo Method(string name) => typeof(RsgController).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
	private static void Check(bool condition, string description)
	{
		if (!condition) throw new Exception("RsgPiecePortCheck failed: " + description);
	}
}
