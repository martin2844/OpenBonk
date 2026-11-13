using System;
using Assets.Scripts.Game.Other;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Managers
{
	public class MapController
	{
		private static PlayerInventory inventory;

		public static int index;

		private static bool isFinalBossStage;

		private const string finalBossMapName = "FinalBossMap";

		private static bool reseting;

		public static Action A_NewRunStarted;

		public static RunConfig runConfig;

		private static string mainMenuSceneName = "MainMenu";

		public static MapData currentMap { get; private set; }

		public static StageData currentStage { get; private set; }

		public static void RestartRun()
		{
			reseting = true;
			StartNewMap(runConfig);
		}

		public static void StartNewMap(RunConfig newRunConfig)
		{
			Debug.Log($"MapController.StartNewMap: config={newRunConfig != null}, map={newRunConfig?.mapData?.name}, stage={newRunConfig?.stageData?.name}");
			SceneManager.sceneLoaded -= LogLoadedScene;
			SceneManager.sceneLoaded += LogLoadedScene;
		if (newRunConfig == null)
		{
			Debug.LogError("MapController received a null RunConfig.");
			return;
		}

		TryCleanupInventory();
		OpenBonkMapLuaHooks.Reset();
		inventory = null;
#if UNITY_ANDROID && !UNITY_EDITOR
		AndroidNoItemsChallengeProbe.Configure(newRunConfig);
		AndroidTiledMapProbe.Configure(newRunConfig);
		AndroidSpawnSurfaceProbe.Configure(newRunConfig);
#endif
		runConfig = newRunConfig;
		index = -1;
#if UNITY_ANDROID && !UNITY_EDITOR
		index = AndroidSpawnSurfaceProbe.InitialStageIndex(index);
#endif
		isFinalBossStage = false;
		currentMap = newRunConfig.mapData;
		if (currentMap != null && SaveManager.Instance != null && SaveManager.Instance.progression != null)
			SaveManager.Instance.progression.menuMeta.SetTier(currentMap.eMap, newRunConfig.mapTierIndex);
		if (newRunConfig.stageData == null && currentMap != null && currentMap.stages != null && currentMap.stages.Length > 0)
		{
			newRunConfig.stageData = currentMap.stages[0];
		}
		A_NewRunStarted?.Invoke();
		Debug.Log($"MapController initialized run: map={currentMap?.name}, stage={newRunConfig.stageData?.name}, stages={currentMap?.stages?.Length ?? 0}");
		LoadNextStage();
		}

		private static void LogLoadedScene(Scene scene, LoadSceneMode mode)
		{
			Debug.Log($"MapController observed loaded scene: {scene.name} ({mode})");
		}

		private static void TryCleanupInventory()
		{
			inventory?.Cleanup();
		}

		public static void LoadNextStage()
		{
			Debug.Log($"MapController.LoadNextStage: active={SceneManager.GetActiveScene().name}, config={runConfig != null}, map={currentMap?.name}, index={index}");
		if (runConfig == null || runConfig.mapData == null)
		{
			return;
		}

		// The original keeps the live inventory across ordinary stage transitions,
		// but a restarted run must not inherit the previous player's inventory.
		if (!reseting && Assets.Scripts.Actors.Player.MyPlayer.Instance != null &&
			GameManager.Instance != null && GameManager.Instance.GetPlayerInventory() != null)
			inventory = GameManager.Instance.GetPlayerInventory();
		reseting = false;

		currentMap = runConfig.mapData;
		index++;
		StageData[] stages = currentMap.stages;
		if (stages != null && stages.Length > 0)
		{
			currentStage = stages[Mathf.Clamp(index, 0, stages.Length - 1)];
			runConfig.stageData = currentStage;
		}
		else if (runConfig.stageData == null)
		{
			return;
		}

		Debug.Log("MapController requesting GeneratedMap scene");
		if (TransitionUI.Instance != null) TransitionUI.Instance.StartLoadingMap("GeneratedMap");
		else SceneManager.LoadScene("GeneratedMap");
		}

		public static void LoadFinalStage()
		{
			if (!reseting && GameManager.Instance != null && GameManager.Instance.GetPlayerInventory() != null)
				inventory = GameManager.Instance.GetPlayerInventory();
			isFinalBossStage = true;
			reseting = false;
			TransitionUI.Instance.StartLoadingMap(finalBossMapName);
		}

		public static bool IsFirstStage()
		{
			return index == 0;
		}

		public static int GetStageIndex()
		{
			return index;
		}

		public static bool IsLastStage()
		{
			return index == currentMap.stages.Length - 1;
		}

		public static bool IsFinalBossStage()
		{
			return isFinalBossStage;
		}

		public static void TestFinalBoss()
		{
		}

		public static bool IsTierFinalStage()
		{
			return runConfig != null && index == runConfig.mapTierIndex;
		}

		public static PlayerInventory GetPlayerInventory(CharacterData data)
		{
			if (inventory == null) inventory = new PlayerInventory(data);
			return inventory;
		}

		public static bool HasPlayerInventory()
		{
			return inventory != null;
		}

		public static void TestMap(MapData mapData, StageData stageData)
		{
		}

		public static void TestMap(RunConfig testConfig)
		{
		}

		public static bool IsMainMenu()
		{
			return SceneManager.GetActiveScene().name == mainMenuSceneName;
		}
	}
}
