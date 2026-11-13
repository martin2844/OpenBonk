using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine.AI;

[InitializeOnLoad]
public static class RsgOriginalRoomsPlayCheck
{
	private const string SessionKey = "RsgOriginalRoomsPlayCheck.pending";
	private const string TestScene = "Assets/RsgOriginalRoomsCheckTemporary.unity";
	static RsgOriginalRoomsPlayCheck() { EditorApplication.playModeStateChanged += OnPlayModeChanged; }
	public static void Run()
	{
		Scene source = EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Single);
		RsgController original = UnityEngine.Object.FindAnyObjectByType<RsgController>(FindObjectsInactive.Include);
		if (original == null) throw new Exception("Original generated-map scene has no RsgController");
		MapGenerationController originalMap = UnityEngine.Object.FindAnyObjectByType<MapGenerationController>(FindObjectsInactive.Include);
		if (originalMap == null) throw new Exception("Original generated-map scene has no MapGenerationController");
		Scene test = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
		GameObject controllerObject = new GameObject("original rooms check controller");
		SceneManager.MoveGameObjectToScene(controllerObject, test);
		RsgController controller = controllerObject.AddComponent<RsgController>();
		controller.prefabs = new GameObject[original.prefabs.Length];
		for (int i = 0; i < controller.prefabs.Length; i++) controller.prefabs[i] = Clone(original.prefabs[i], test);
		controller.roomStart = Clone(original.roomStart, test);
		controller.roomEnd = Clone(original.roomEnd, test);
		controller.roomBoss = Clone(original.roomBoss.gameObject, test).GetComponent<GraveyardBossRoom>();
		if (original.navmeshSurface != null)
		{
			controller.navmeshSurface = controllerObject.AddComponent<NavMeshSurface>();
			EditorUtility.CopySerialized(original.navmeshSurface, controller.navmeshSurface);
			controller.navmeshSurface.layerMask = 1 << LayerMask.NameToLayer("Ground");
		}
		GameObject mapObject = new GameObject("original crypt startup check");
		SceneManager.MoveGameObjectToScene(mapObject, test);
		MapGenerationController map = mapObject.AddComponent<MapGenerationController>();
		map.rsgController = controller;
		map.cryptParent = new GameObject("crypt parent");
		SceneManager.MoveGameObjectToScene(map.cryptParent, test);
		map.cryptParent.SetActive(false);
		map.gameManager = mapObject.AddComponent<GameManager>();
		map.proceduralTileGeneration = mapObject.AddComponent<ProceduralTileGeneration>();
		EditorUtility.CopySerialized(originalMap.proceduralTileGeneration, map.proceduralTileGeneration);
		map.testMapData = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapGraveyard.asset");
		map.testStageData = AssetDatabase.LoadAssetAtPath<StageData>("Assets/MonoBehaviour/StageGraveyard.asset");
		map.cryptExitOutside = Clone(originalMap.cryptExitOutside, test);
		controller.minPieces = original.minPieces;
		controller.maxPieces = original.maxPieces;
		controller.combineColliderMesh = original.combineColliderMesh;
		EditorSceneManager.SetActiveScene(test);
		EditorSceneManager.CloseScene(source, true);
		EditorSceneManager.SaveScene(test, TestScene);
		SessionState.SetBool(SessionKey, true);
		EditorApplication.isPlaying = true;
	}
	private static GameObject Clone(GameObject original, Scene destination)
	{
		if (original == null) throw new Exception("Original dungeon room is missing");
		GameObject copy = UnityEngine.Object.Instantiate(original);
		copy.name = original.name;
		SceneManager.MoveGameObjectToScene(copy, destination);
		copy.SetActive(false);
		return copy;
	}
	private static void OnPlayModeChanged(PlayModeStateChange state)
	{
		if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
		SessionState.SetBool(SessionKey, false);
		new GameObject("original rooms check runner").AddComponent<RsgOriginalRoomsPlayRunner>().StartCheck();
	}
}

public class RsgOriginalRoomsPlayRunner : MonoBehaviour
{
	public void StartCheck() { StartCoroutine(CheckGeneration()); }
	private IEnumerator CheckGeneration()
	{
		RsgController controller = UnityEngine.Object.FindAnyObjectByType<RsgController>();
		if (controller == null) { Debug.LogError("Original rooms check controller is missing"); EditorApplication.Exit(1); yield break; }
		MapGenerationController map = UnityEngine.Object.FindAnyObjectByType<MapGenerationController>();
		object[] arguments = { 484146104, Vector3.zero, Vector3.zero };
		try
		{
		typeof(Assets.Scripts.Managers.MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map.testMapData });
		typeof(Assets.Scripts.Managers.MapController).GetProperty("currentStage").GetSetMethod(true).Invoke(null, new object[] { map.testStageData });
		Vector3 outdoorStart = map.proceduralTileGeneration.Generate(out Vector3 outdoorDirection, map.testStageData, map.testStageData.mapParameters, true);
		CheckOutdoorCollision(map.proceduralTileGeneration);
		typeof(MapGenerationController).GetProperty("tileSpawnPosition").GetSetMethod(true).Invoke(null, new object[] { outdoorStart });
		typeof(MapGenerationController).GetProperty("tileSpawnDir").GetSetMethod(true).Invoke(null, new object[] { outdoorDirection });
		typeof(MapGenerationController).GetMethod("CryptGeneration", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(map, arguments);
		}
		catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); yield break; }
		float traversal = map.gameManager.dungeonTimeToComplete;
		yield return null;
		try
		{
			var pieces = (List<RsgPiece>)typeof(RsgController).GetField("allPieces", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
			Check(pieces.Count == 5, "five original rooms");
			Check(controller.rsgStart != null && controller.rsgStart.spawnTransform != null && controller.rsgEnd != null, "original start and exit");
			Check(map.gameManager.isCrypt && map.cryptParent.activeSelf, "crypt startup and dungeon timer state");
			Check((Vector3)arguments[1] == controller.rsgStart.spawnTransform.position && (Vector3)arguments[2] == controller.rsgStart.spawnTransform.forward, "crypt spawn handoff");
			Check(NavMesh.SamplePosition((Vector3)arguments[1], out NavMeshHit _, 15f, NavMesh.AllAreas), "crypt navigation near spawn");
			Check(traversal > 0f, "traversal time");
			for (int i = 1; i < pieces.Count; i++) Check(Vector3.Distance(pieces[i].start.position, pieces[i - 1].end.position) < 0.05f, "connected original rooms");
			Check(controller.combinedColliderMesh != null && controller.combinedColliderMesh.GetComponent<MeshCollider>().sharedMesh.vertexCount > 0, "original combined collision mesh");
			Debug.Log("RsgOriginalRoomsPlayCheck passed: pieces=" + pieces.Count + ", traversal=" + traversal);
			EditorApplication.Exit(0);
		}
		catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
	}
	private static void Check(bool value, string description) { if (!value) throw new Exception("RsgOriginalRoomsPlayCheck failed: " + description); }
	private static void CheckOutdoorCollision(ProceduralTileGeneration generator)
	{
		var parts = new List<CombineInstance>();
		var tiles = new List<GameObject>(generator.tiles);
		tiles.AddRange(generator.fillTiles);
		foreach (GameObject tile in tiles)
		{
			MeshCollider collider = tile.GetComponent<MeshCollider>();
			if (collider == null) continue;
			for (int submesh = 0; submesh < collider.sharedMesh.subMeshCount; submesh++)
				parts.Add(new CombineInstance { mesh = collider.sharedMesh, subMeshIndex = submesh, transform = collider.transform.localToWorldMatrix });
		}
		var expected = new Mesh();
		try
		{
			expected.CombineMeshes(parts.ToArray(), true);
			int inputVertices = expected.vertexCount;
			Mesh indexed = UnityEngine.Object.Instantiate(expected);
			typeof(MeshColliderWeldPortCheck).GetMethod("Reference", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { expected, .25f });
			typeof(Assets.Scripts.MapGeneration.ProceduralTiles.MeshColliderCombiner).GetMethod("AutoWeld", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { indexed, .25f });
			Vector3[] iv = indexed.vertices, ev = expected.vertices;
			int[] ii = indexed.triangles, ei = expected.triangles;
			Check(iv.Length == ev.Length && ii.Length == ei.Length, "same-input indexed lengths");
			for (int i = 0; i < iv.Length; i++) Check(iv[i].x == ev[i].x && iv[i].y == ev[i].y && iv[i].z == ev[i].z, "same-input indexed vertex " + i);
			for (int i = 0; i < ii.Length; i++) Check(ii[i] == ei[i], "same-input indexed triangle " + i);
			UnityEngine.Object.DestroyImmediate(indexed);
			Mesh actual = generator.collider.GetComponent<MeshCollider>().sharedMesh;
			Vector3[] a = actual.vertices, b = expected.vertices;
			int[] ai = actual.triangles, bi = expected.triangles;
			Check(a.Length == b.Length && ai.Length == bi.Length, "outdoor collision mesh lengths");
			for (int i = 0; i < a.Length; i++) Check(Vector3.Distance(a[i], b[i]) < .001f, $"outdoor vertex {i}: actual={a[i].ToString("F6")} expected={b[i].ToString("F6")}");
			for (int i = 0; i < ai.Length; i++) Check(ai[i] == bi[i], "outdoor triangle index " + i);
			Debug.Log($"Outdoor weld comparison passed: tiles={tiles.Count}, inputVertices={inputVertices}, outputVertices={a.Length}, triangleIndices={ai.Length}");
		}
		finally { UnityEngine.Object.DestroyImmediate(expected); }
	}
}
