using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.UI.HUD;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class OriginalObjectivePlayCheck
{
	private const string SessionKey = "OriginalObjectivePlayCheck.pending";
	private const string TestScene = "Assets/OriginalObjectiveCheckTemporary.unity";
	static OriginalObjectivePlayCheck() { EditorApplication.playModeStateChanged += OnPlayModeChanged; }
	public static void Run()
	{
		Scene source = EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Single);
		ObjectiveUi original = UnityEngine.Object.FindAnyObjectByType<ObjectiveUi>(FindObjectsInactive.Include);
		if (original == null || original.objectivePrefab == null) throw new Exception("Original objective template is missing");
		Scene test = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
		GameObject canvasObject = new GameObject("objective test canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		SceneManager.MoveGameObjectToScene(canvasObject, test);
		canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
		GameObject template = UnityEngine.Object.Instantiate(original.objectivePrefab); template.name = "original objective template";
		SceneManager.MoveGameObjectToScene(template, test); template.transform.SetParent(canvasObject.transform, false); template.SetActive(false);
		GameObject controller = new GameObject("objective test controller"); SceneManager.MoveGameObjectToScene(controller, test);
		ObjectiveUi ui = controller.AddComponent<ObjectiveUi>(); ui.objectivePrefab = template;
		ui.a_new = controller.AddComponent<AudioSource>(); ui.a_complete = controller.AddComponent<AudioSource>();
		ui.defeatBoss = new LocalizedString("OriginalObjectivePlay", "BOSS");
		EditorSceneManager.SetActiveScene(test); EditorSceneManager.CloseScene(source, true); EditorSceneManager.SaveScene(test, TestScene);
		SessionState.SetBool(SessionKey, true); EditorApplication.isPlaying = true;
	}
	private static void OnPlayModeChanged(PlayModeStateChange state)
	{
		if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
		SessionState.SetBool(SessionKey, false);
		new GameObject("objective test runner").AddComponent<OriginalObjectivePlayRunner>().StartCheck();
	}
}

public class OriginalObjectivePlayRunner : MonoBehaviour
{
	private sealed class Provider : ITableProvider
	{
		public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
		{
			var table = ScriptableObject.CreateInstance<StringTable>(); table.SharedData = ScriptableObject.CreateInstance<SharedTableData>();
			table.SharedData.TableCollectionName = name; table.LocaleIdentifier = locale.Identifier; table.AddEntry("BOSS", "Defeat the boss");
			return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
		}
	}
	public void StartCheck() { StartCoroutine(Check()); }
	private IEnumerator Check()
	{
		var settings = ScriptableObject.CreateInstance<LocalizationSettings>(); var locale = Locale.CreateLocale("en");
		LocalizationSettings.Instance = settings; LocalizationSettings.SelectedLocale = locale; LocalizationSettings.StringDatabase.TableProvider = new Provider();
		ObjectiveUi ui = UnityEngine.Object.FindAnyObjectByType<ObjectiveUi>();
		ui.OnBossSpawned();
		float elapsed = 0f;
		while (elapsed < 2f) { MyTime.deltaTime = Time.deltaTime; elapsed += Time.deltaTime; yield return null; }
		try
		{
			var current = (ObjectivePrefabUi)typeof(ObjectiveUi).GetField("currentObjective", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
			if (current == null || current.t_objective.text != "Defeat the boss" || !current.gameObject.activeInHierarchy) throw new Exception("Original objective did not appear");
			if (current.textSizer.Text == null || current.textSizer.GetComponent<RectTransform>().rect.width <= 0f) throw new Exception("Original objective text was not measured");
			ui.CompleteCurrentObjective();
			if (!current.checkMark.activeSelf) throw new Exception("Original objective completion failed");
			Debug.Log("OriginalObjectivePlayCheck passed: original template appears and completes");
			EditorApplication.Exit(0);
		}
		catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
	}
}
