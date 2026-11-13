using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BoulderPlayCheck
{
    const string Pending = "BoulderPlayCheck.pending";
    static BoulderPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("boulder test runner").AddComponent<BoulderPlayRunner>().Begin();
        }
    }
}

public class BoulderPlayRunner : MonoBehaviour
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("BoulderPlayCheck: " + message); }
    static void Call(Boulder b, string name) => typeof(Boulder).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(b, null);
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Boulder.prefab");
            Require(prefab != null, "prefab missing");
            var obj = Instantiate(prefab); obj.SetActive(false);
            var b = obj.GetComponent<Boulder>();
            Require(b != null && b.rb != null && b.audio != null && Mathf.Approximately(b.frictionStrength, 3000f), "prefab wiring");
            foreach (var behaviour in obj.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            obj.transform.localScale = Vector3.one * 4f;
            var body = b.rb; float initialMass = body.mass;
            obj.SetActive(true); body.useGravity = false; body.drag = 0; body.angularDrag = 0;
            Call(b, "Start");
            Require(Mathf.Abs(body.mass - initialMass * 2f) < .01f, "mass scales with size");
            MyTime.paused = false; body.velocity = new Vector3(11f, 0, 0); Call(b, "Update");
            Require(Mathf.Abs(b.audio.volume - .425f) < .001f, "sound volume at intermediate speed");
            MyTime.paused = true; body.velocity = Vector3.zero; Call(b, "Update");
            Require(Mathf.Abs(b.audio.volume - .425f) < .001f, "pause leaves sound unchanged");
            MyTime.paused = false; Call(b, "Update"); Require(b.audio.volume == 0f, "silent at low speed");
            body.velocity = new Vector3(25f, 0, 0); Call(b, "Update"); Require(Mathf.Abs(b.audio.volume - .85f) < .001f, "volume capped");
            body.velocity = Vector3.right * 10f; body.angularVelocity = Vector3.up * 2f;
            MyTime.fixedDeltaTime = .02f; Call(b, "FixedUpdate"); Physics.Simulate(.02f);
            Require(body.velocity.x < 10f && body.velocity.x > 0f, "linear friction opposes motion");
            Require(body.angularVelocity.y < 2f && body.angularVelocity.y > 0f, "rotational friction opposes motion");
            Debug.Log("BoulderPlayCheck passed: original prefab mass, sound volume, pause, force and torque");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
