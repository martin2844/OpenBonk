using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ChainLightningPlayCheck
{
    const string Pending = "ChainLightningPlayCheck.pending";
    static ChainLightningPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("chain lightning check").AddComponent<ChainLightningPlayRunner>().Begin();
        }
    }
}

public class ChainLightningPlayRunner : MonoBehaviour
{
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ChainLightning.prefab");
        if (prefab == null) { Debug.LogError("ChainLightningPlayCheck: prefab missing"); EditorApplication.Exit(1); yield break; }
        var obj = Instantiate(prefab);
        var line = obj.GetComponentInChildren<ChainLightning>(true);
        if (line == null || line.lineRenderer == null) { Debug.LogError("ChainLightningPlayCheck: missing component or renderer"); EditorApplication.Exit(1); yield break; }
        foreach (var script in obj.GetComponentsInChildren<MonoBehaviour>(true)) if (!(script is ChainLightning)) script.enabled = false;
        var positions = new List<Vector3> { Vector3.zero, new Vector3(1, 2, 3), new Vector3(4, 5, 6) };
        for (int i = 0; i < 2; i++)
        {
            line.Set(positions);
            if (!line.gameObject.activeSelf || line.lineRenderer.positionCount != positions.Count || line.lineRenderer.GetPosition(2) != positions[2])
            { Debug.LogError("ChainLightningPlayCheck: positions or activation differ"); EditorApplication.Exit(1); yield break; }
            yield return new WaitForSeconds(.3f);
            if (line.gameObject.activeSelf) { Debug.LogError("ChainLightningPlayCheck: effect did not deactivate"); EditorApplication.Exit(1); yield break; }
        }
        // Vary point count to cover pooled lines reused for different chains.
        var two = new List<Vector3> { new Vector3(-2, 1, 0), new Vector3(7, 8, 9) };
        for (int i = 0; i < 8; i++)
        {
            var expected = i % 2 == 0 ? two : positions;
            line.Set(expected);
            if (line.lineRenderer.positionCount != expected.Count) { Debug.LogError("ChainLightningPlayCheck: changed point count"); EditorApplication.Exit(1); yield break; }
            for (int j = 0; j < expected.Count; j++) if (line.lineRenderer.GetPosition(j) != expected[j]) { Debug.LogError("ChainLightningPlayCheck: changed point positions"); EditorApplication.Exit(1); yield break; }
        }
        var longChain = new List<Vector3>();
        for (int i = 0; i < 65; i++) longChain.Add(new Vector3(i, -i, i * 2));
        line.Set(longChain);
        if (line.lineRenderer.positionCount != 65 || line.lineRenderer.GetPosition(64) != longChain[64]) { Debug.LogError("ChainLightningPlayCheck: long chain differs"); EditorApplication.Exit(1); yield break; }
        Debug.Log("ChainLightningPlayCheck passed: original prefab line, expiry and reuse");
        EditorApplication.Exit(0);
    }
}
