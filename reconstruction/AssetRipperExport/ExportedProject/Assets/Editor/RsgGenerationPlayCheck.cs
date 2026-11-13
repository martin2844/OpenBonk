using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RsgGenerationPlayCheck
{
	private const string SessionKey = "RsgGenerationPlayCheck.pending";
	static RsgGenerationPlayCheck()
	{
		EditorApplication.playModeStateChanged += OnPlayModeChanged;
	}
	public static void Run()
	{
		SessionState.SetBool(SessionKey, true);
		EditorApplication.isPlaying = true;
	}
	private static void OnPlayModeChanged(PlayModeStateChange state)
	{
		if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
		SessionState.SetBool(SessionKey, false);
		new GameObject("rsg play check runner").AddComponent<RsgGenerationPlayRunner>().StartCheck();
	}
}

public class RsgGenerationPlayRunner : MonoBehaviour
{
	public void StartCheck() { StartCoroutine(CheckGeneration()); }
	private IEnumerator CheckGeneration()
	{
		GameObject start = MakePiece("start", 5);
		GameObject middle = MakePiece("middle", 10);
		GameObject end = MakePiece("end", 15);
		start.AddComponent<RsgStart>();
		end.AddComponent<InteractableCryptLeave>();
		RsgController controller = new GameObject("rsg play controller").AddComponent<RsgController>();
		controller.prefabs = new[] { middle };
		controller.roomStart = start; controller.roomEnd = end;
		controller.minPieces = 1; controller.maxPieces = 1;
		controller.combineColliderMesh = true;
		float traversal;
		controller.Generate(12345, RsgController.EDungeonType.Normal, out traversal);
		yield return null;
		try
		{
			var pieces = (List<RsgPiece>)typeof(RsgController).GetField("allPieces", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
			Check(pieces.Count == 3, "three accepted rooms");
			Check(Mathf.Approximately(traversal, 36f), "synchronous traversal time");
			Check(controller.rsgStart != null && controller.rsgEnd != null, "start and exit");
			Check(controller.combinedColliderMesh != null && controller.combinedColliderMesh.layer == LayerMask.NameToLayer("Ground"), "combined collider and ground layer");
			foreach (RsgPiece piece in pieces) Check(!piece.newCollider.enabled, "individual colliders disabled");
			Physics.SyncTransforms();
			Check(Physics.Raycast(pieces[1].transform.position + Vector3.up * 4f, Vector3.down, out RaycastHit hit, 10f, 1 << LayerMask.NameToLayer("Ground")) && hit.collider.gameObject == controller.combinedColliderMesh, "combined room collision");
			Check(Vector3.Distance(pieces[1].start.position, pieces[0].end.position) < 0.01f && Vector3.Distance(pieces[2].start.position, pieces[1].end.position) < 0.01f, "connected rooms");
			int cloneCount = 0;
			foreach (RsgPiece piece in UnityEngine.Object.FindObjectsByType<RsgPiece>(FindObjectsSortMode.None))
				if (piece.gameObject != start && piece.gameObject != middle && piece.gameObject != end) cloneCount++;
			Check(cloneCount == 3, "lookahead room removed");
			Debug.Log("RsgGenerationPlayCheck passed");
			EditorApplication.Exit(0);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			EditorApplication.Exit(1);
		}
	}
	private static GameObject MakePiece(string name, int traversal)
	{
		GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
		obj.name = name;
		RsgPiece piece = obj.AddComponent<RsgPiece>();
		piece.meshFilter = obj.GetComponent<MeshFilter>(); piece.newCollider = obj.AddComponent<MeshCollider>();
		piece.children = new GameObject("children"); piece.children.transform.SetParent(obj.transform, false);
		piece.start = new GameObject("start").transform; piece.start.SetParent(obj.transform, false);
		piece.end = new GameObject("end").transform; piece.end.SetParent(obj.transform, false); piece.end.localPosition = Vector3.forward * 2f;
		piece.traverseTime = traversal;
		return obj;
	}
	private static void Check(bool value, string description) { if (!value) throw new Exception("RsgGenerationPlayCheck failed: " + description); }
}
