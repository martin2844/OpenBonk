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
public static class RsgBossDungeonPlayCheck
{
	private const string SessionKey = "RsgBossDungeonPlayCheck.pending";
	private const string TestScene = "Assets/RsgBossDungeonCheckTemporary.unity";
	static RsgBossDungeonPlayCheck() { EditorApplication.playModeStateChanged += OnPlayModeChanged; }
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
		new GameObject("original rooms check runner").AddComponent<RsgBossDungeonPlayRunner>().StartCheck();
	}
}

public class RsgBossDungeonPlayRunner : MonoBehaviour
{
	public void StartCheck() { StartCoroutine(CheckGeneration()); }
	private IEnumerator CheckGeneration()
	{
		RsgController controller = UnityEngine.Object.FindAnyObjectByType<RsgController>();
		int[] seeds = { 484146105, 1, 12345 };
		foreach (int seed in seeds)
		{
			try
			{
				controller.Generate(seed, RsgController.EDungeonType.BossDungeon, out float traversal);
				var pieces = (List<RsgPiece>)typeof(RsgController).GetField("allPieces", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
				Check(controller.rsgStart != null && controller.rsgStart.spawnTransform != null && controller.rsgEnd != null, "synchronous start and exit for seed " + seed);
				Check((RsgController.EDungeonType)typeof(InteractableCryptLeave).GetField("dungeonType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller.rsgEnd) == RsgController.EDungeonType.BossDungeon, "boss exit type");
				Check(controller.roomBoss.playerTeleportTransform != null, "original boss entrance");
				Check(pieces.Count >= 3 && traversal > 0f, "pieces and traversal time");
				for (int i = 1; i < pieces.Count; i++) Check(Vector3.Distance(pieces[i].start.position, pieces[i-1].end.position) < 0.05f, "connected rooms");
				Check(controller.combinedColliderMesh != null && controller.combinedColliderMesh.GetComponent<MeshCollider>().sharedMesh.vertexCount > 0, "combined collision mesh");
				Check(NavMesh.SamplePosition(controller.rsgStart.spawnTransform.position, out NavMeshHit _, 15f, NavMesh.AllAreas), "navigation near spawn");
				Debug.Log("RsgBossDungeonPlayCheck seed=" + seed + " pieces=" + pieces.Count + " traversal=" + traversal);
				controller.ClearMap();
			}
			catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); yield break; }
			yield return null;
		}
		Debug.Log("RsgBossDungeonPlayCheck passed");
		EditorApplication.Exit(0);
	}
	private static void Check(bool value, string description) { if (!value) throw new Exception("RsgBossDungeonPlayCheck failed: " + description); }
}
