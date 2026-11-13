using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LookTowardsPlayerPlayCheck
{
    const string Pending = "LookTowardsPlayerPlayCheck.pending";
    static LookTowardsPlayerPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("Shady look check").AddComponent<LookTowardsPlayerPlayRunner>().Begin(); } }
}

public class LookTowardsPlayerPlayRunner : MonoBehaviour
{
    static void Require(bool value, string message) { if (!value) throw new Exception("LookTowardsPlayerPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            Require(Time.deltaTime > 0, "nonzero frame delta");
            var playerObject = new GameObject("controlled player"); playerObject.SetActive(false);
            var player = playerObject.AddComponent<MyPlayer>(); player.enabled = false; playerObject.SetActive(true);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ShadyGuy.prefab"); Require(prefab != null, "prefab missing");
            var holder = new GameObject("prefab parent"); holder.SetActive(false);
            var obj = Instantiate(prefab, holder.transform);
            var component = Array.Find(obj.GetComponentsInChildren<LookTowardsPlayer>(true), c => c.enabled);
            Require(component != null && component.onlyXZ && component.lerp, "prefab component and settings");
            foreach (var script in obj.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            holder.SetActive(true);
            var method = typeof(LookTowardsPlayer).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            var t = component.transform; t.position = Vector3.zero; t.rotation = Quaternion.identity;
            player.transform.position = new Vector3(10, 10, 0);
            MyPlayer.Instance = null; method.Invoke(component, null);
            Require(Quaternion.Angle(t.rotation, Quaternion.identity) < .01f, "no-player guard");
            MyPlayer.Instance = player;
            var expected = Quaternion.Lerp(Quaternion.identity, Quaternion.LookRotation(Vector3.right), Time.deltaTime * 2f);
            method.Invoke(component, null);
            Require(Quaternion.Angle(t.rotation, expected) < .02f, "horizontal interpolation");
            component.lerp = false; t.rotation = Quaternion.identity; method.Invoke(component, null);
            Require(Vector3.Angle(t.forward, Vector3.right) < .02f, "horizontal direct rotation");
            component.onlyXZ = false; component.lerp = true; t.rotation = Quaternion.identity; method.Invoke(component, null);
            Require(Vector3.Angle(t.forward, new Vector3(1, 1, 0)) < .02f, "full direct rotation ignores interpolation");
            MyPlayer.Instance = null;
            Debug.Log("LookTowardsPlayerPlayCheck passed: ShadyGuy prefab, missing player, horizontal interpolation and direct rotation, full rotation");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
