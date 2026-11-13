using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DistanceVisibilityPlayCheck
{
    const string Pending = "DistanceVisibilityPlayCheck.pending";
    static DistanceVisibilityPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false); new GameObject("visibility runner").AddComponent<DistanceVisibilityPlayRunner>().Begin();
    }
}

public class DistanceVisibilityPlayRunner : MonoBehaviour
{
    public void Begin() { StartCoroutine(Check()); }
    IEnumerator Check()
    {
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Brazier.prefab");
            if (prefab == null) throw new Exception("Original brazier missing");
            var root = Instantiate(prefab);
            var visibility = root.GetComponent<DisableOutOfCameraRange>();
            if (visibility == null || visibility.objectsToUse == null || visibility.objectsToUse.Length == 0) throw new Exception("Brazier visibility references missing");
            visibility.range = 10f;
            var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var type = typeof(DisableOutOfCameraRange);
            type.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(visibility, null);
            var check = type.GetMethod("CheckVisibility", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var obj in visibility.objectsToUse) if (obj.activeSelf) throw new Exception("Objects initially visible");
            player.transform.position = root.transform.position + Vector3.right * 10f;
            check.Invoke(visibility, null);
            if (visibility.objectsToUse[0].activeSelf) throw new Exception("Exact boundary should retain hidden state");
            player.transform.position = root.transform.position + Vector3.right * 9f;
            check.Invoke(visibility, null);
            foreach (var obj in visibility.objectsToUse) if (!obj.activeSelf) throw new Exception("Nearby effect not shown");
            player.transform.position = root.transform.position + Vector3.right * 10f;
            check.Invoke(visibility, null);
            if (!visibility.objectsToUse[0].activeSelf) throw new Exception("Exact boundary should retain visible state");
            player.transform.position = root.transform.position + Vector3.up * 11f;
            check.Invoke(visibility, null);
            if (visibility.objectsToUse[0].activeSelf) throw new Exception("Vertical distance should hide in 3D mode");
            visibility.useXZVector = true;
            check.Invoke(visibility, null);
            if (!visibility.objectsToUse[0].activeSelf) throw new Exception("XZ mode should ignore height");
            Debug.Log("DistanceVisibilityPlayCheck passed: actual brazier, thresholds, 3D and XZ distance");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        yield break;
    }
}
