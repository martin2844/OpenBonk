using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DeathCameraPlayCheck
{
    const string Pending = "DeathCameraPlayCheck.pending";
    static DeathCameraPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("death camera runner").AddComponent<DeathCameraPlayRunner>().Begin(); } }
}
public class DeathCameraPlayRunner : MonoBehaviour
{
    static void Check(bool value, string message) { if (!value) throw new Exception("DeathCameraPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            var root = new GameObject("player state"); root.SetActive(false); root.transform.position = new Vector3(2, 3, 4);
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var visual = new GameObject("visual"); visual.transform.SetParent(root.transform); visual.transform.localRotation = Quaternion.Euler(0, 0, 30);
            player.playerRenderer = visual.AddComponent<PlayerRenderer>();
            var go = new GameObject("camera state"); go.SetActive(false); go.transform.position = new Vector3(12, 7, -4); go.transform.rotation = Quaternion.Euler(0, 35, 0);
            var camera = go.AddComponent<PlayerCamera>(); var method = typeof(PlayerCamera).GetMethod("DeathCam", BindingFlags.Instance | BindingFlags.NonPublic);
            Vector3 target = root.transform.position + visual.transform.up * .55f;
            Vector3 oldPosition = go.transform.position; Quaternion oldRotation = go.transform.rotation;
            Vector3 expectedPosition = Vector3.Lerp(oldPosition, target + Vector3.up * 4.5f, Time.deltaTime * .5f);
            Quaternion expectedRotation = Quaternion.Lerp(oldRotation, Quaternion.LookRotation(target - oldPosition, root.transform.up), Time.deltaTime);
            method.Invoke(camera, null);
            Check(Vector3.Distance(go.transform.position, expectedPosition) < .0001f, "original position interpolation");
            Check(Quaternion.Angle(go.transform.rotation, expectedRotation) < .02f, "original look rotation interpolation");
            player.playerRenderer = null; oldPosition = go.transform.position; method.Invoke(camera, null); Check(go.transform.position == oldPosition, "missing renderer guarded");
            Debug.Log("DeathCameraPlayCheck passed: original framing, position and rotation interpolation"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
