using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SpawnRailsPlayCheck
{
    const string Pending = "SpawnRailsPlayCheck.pending";
    static SpawnRailsPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("Rail spawn check").AddComponent<SpawnRailsPlayRunner>().Begin(); } }
}

public class SpawnRailsPlayRunner : MonoBehaviour
{
    static void Require(bool value, string message) { if (!value) throw new Exception("SpawnRailsPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            var managerObject = new GameObject("controlled manager"); managerObject.SetActive(false);
            var manager = managerObject.AddComponent<GameManager>(); manager.enabled = false;
            GameManager.Instance = manager;
            int groundLayer = LayerMask.NameToLayer("Ground"); Require(groundLayer >= 0, "ground layer");
            manager.whatIsGroundAndObjects = 1 << groundLayer;
            manager.whatIsBlockingRails = 0;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "test ground"; ground.layer = groundLayer;
            ground.transform.position = new Vector3(0, -0.5f, 0); ground.transform.localScale = new Vector3(300, 1, 300);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Rail1.prefab"); Require(prefab != null, "Rail1 prefab");
            var spawner = new GameObject("spawner").AddComponent<SpawnInteractables>(); spawner.rails = new[] { prefab }; spawner.SetArea(new Vector3(100, 1, 100), Vector3.zero, 100);
            var run = typeof(SpawnInteractables).GetMethod("SpawnRails", BindingFlags.NonPublic | BindingFlags.Instance);
            var count = typeof(SpawnInteractables).GetField("numRails", BindingFlags.NonPublic | BindingFlags.Instance);
            Physics.SyncTransforms();
            run.Invoke(spawner, null);
            Require(FindObjectsOfType<Rail>().Length == 0, "default zero rail count");
            count.SetValue(spawner, 1);
            run.Invoke(spawner, null);
            var rails = FindObjectsOfType<Rail>();
            Require(rails.Length == 5, "five valid attempts must create five rails, got " + rails.Length);
            foreach (var rail in rails) { Require(rail.splineContainer != null, "real prefab spline"); Require(rail.IsValidPosition(), "placed rail validity"); }
            Debug.Log("SpawnRailsPlayCheck passed: zero default count and five attempts using the real Rail1 prefab on controlled ground");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
