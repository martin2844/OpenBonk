using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class RsgGenerationPortCheck
{
	public static void Run()
	{
		GameObject managerObject = new GameObject("rsg test controller");
		GameObject startPrefab = MakePiece("start prefab", 10);
		GameObject endPrefab = MakePiece("end prefab", 15);
		startPrefab.AddComponent<RsgStart>();
		endPrefab.AddComponent<InteractableCryptLeave>();
		RsgController oldInstance = RsgController.Instance;
		Action<float> oldFinished = RsgController.A_GenerationFinished;
		bool oldAutoSync = Physics.autoSyncTransforms;
		RsgController controller = null;
		try
		{
			Mesh welding = new Mesh();
			welding.vertices = new[] { Vector3.zero, Vector3.right * 0.49f, Vector3.right * 0.51f, Vector3.forward * 3f };
			welding.triangles = new[] { 0, 2, 3, 1, 2, 3 };
			RsgController.AutoWeld(welding, 0.25f);
			Check(welding.vertexCount == 3 && welding.triangles.Length == 6, "weld uses squared distance threshold");
			UnityEngine.Object.DestroyImmediate(welding);
			Physics.autoSyncTransforms = true;
			RsgController.Instance = null;
			controller = managerObject.AddComponent<RsgController>();
			controller.prefabs = Array.Empty<GameObject>();
			controller.roomStart = startPrefab;
			controller.roomEnd = endPrefab;
			controller.minPieces = 0; controller.maxPieces = 0;
			controller.combineColliderMesh = false;
			typeof(RsgController).GetField("random", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, new ConsistentRandom(123));
			float lowest = -1;
			RsgController.A_GenerationFinished = value => lowest = value;
			IEnumerator generation = (IEnumerator)typeof(RsgController).GetMethod("GenerateMap", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
			Check(!generation.MoveNext(), "synchronous generation without delay");
			var pieces = (List<RsgPiece>)typeof(RsgController).GetField("allPieces", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
			Check(pieces.Count == 2 && controller.rsgStart != null && controller.rsgEnd != null, "start and exit created");
			Check(Vector3.Distance(pieces[0].start.position, Vector3.one * 2000f) < 0.01f, "original starting position");
			Check(Vector3.Distance(pieces[1].start.position, pieces[0].end.position) < 0.01f, "room endpoints connected");
			float traversal = (float)typeof(RsgController).GetField("totalTraversalTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
			Check(Mathf.Approximately(traversal, 30f) && Mathf.Approximately(lowest, 2000f), "traversal time and lowest point");
			Check(!startPrefab.activeSelf && !endPrefab.activeSelf, "template rooms disabled after generation");
			Debug.Log("RsgGenerationPortCheck passed");
		}
		finally
		{
			if (controller != null)
			{
				var pieces = (List<RsgPiece>)typeof(RsgController).GetField("allPieces", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
				if (pieces != null) foreach (RsgPiece piece in pieces) if (piece != null) UnityEngine.Object.DestroyImmediate(piece.gameObject);
			}
			RsgController.Instance = oldInstance; RsgController.A_GenerationFinished = oldFinished;
			Physics.autoSyncTransforms = oldAutoSync;
			UnityEngine.Object.DestroyImmediate(managerObject); UnityEngine.Object.DestroyImmediate(startPrefab); UnityEngine.Object.DestroyImmediate(endPrefab);
		}
	}
	private static GameObject MakePiece(string name, int traversal)
	{
		GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
		obj.name = name;
		RsgPiece piece = obj.AddComponent<RsgPiece>();
		piece.meshFilter = obj.GetComponent<MeshFilter>();
		piece.newCollider = obj.AddComponent<MeshCollider>();
		piece.children = new GameObject("children"); piece.children.transform.SetParent(obj.transform, false);
		piece.start = new GameObject("start").transform; piece.start.SetParent(obj.transform, false);
		piece.end = new GameObject("end").transform; piece.end.SetParent(obj.transform, false); piece.end.localPosition = Vector3.forward * 2f;
		piece.traverseTime = traversal;
		return obj;
	}
	private static void Check(bool value, string description) { if (!value) throw new Exception("RsgGenerationPortCheck failed: " + description); }
}
