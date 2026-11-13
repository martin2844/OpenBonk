using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class ChainLightningPoolPlayCheck
{
    const string Pending = "ChainLightningPoolPlayCheck.pending";
    static ChainLightningPoolPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("chain pool check").AddComponent<ChainLightningPoolPlayRunner>().Begin();
        }
    }
}
public class ChainLightningPoolPlayRunner : MonoBehaviour
{
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ChainLightning.prefab");
        var go = new GameObject("pool manager"); go.SetActive(false);
        var manager = go.AddComponent<PoolManager>();
        var method = typeof(PoolManager).GetMethod("CreateObjectPoolTimeout", BindingFlags.Instance | BindingFlags.NonPublic);
        var pool = (ObjectPool<GameObject>)method.Invoke(manager, new object[] { prefab, 2, .18f });
        MyTime.time = 0f;
        var positions = new List<Vector3> { Vector3.zero, Vector3.one };
        for (int i = 0; i < 4; i++)
        {
            var obj = pool.Get();
            if (obj == null) { Debug.LogError("ChainLightningPoolPlayCheck: pool exhausted at " + i); EditorApplication.Exit(1); yield break; }
            obj.GetComponent<ChainLightning>().Set(positions);
            yield return new WaitForSeconds(.35f);
            if (pool.CountInactive != 1 || pool.CountActive != 0)
            { Debug.LogError($"ChainLightningPoolPlayCheck: object lost at {i}, active={pool.CountActive}, inactive={pool.CountInactive}"); EditorApplication.Exit(1); yield break; }
        }
        var early = pool.Get();
        early.GetComponent<ChainLightning>().Set(positions);
        MyTime.time += .2f;
        yield return new WaitForSeconds(.35f);
        if (pool.CountInactive != 1 || pool.CountActive != 0)
        { Debug.LogError("ChainLightningPoolPlayCheck: timed return or delayed callback failed"); EditorApplication.Exit(1); yield break; }

        var repeated = pool.Get();
        var line = repeated.GetComponent<ChainLightning>();
        line.Set(positions);
        yield return new WaitForSeconds(.12f);
        line.Set(positions);
        yield return new WaitForSeconds(.10f);
        if (!repeated.activeSelf) { Debug.LogError("ChainLightningPoolPlayCheck: old timer stopped repeated effect"); EditorApplication.Exit(1); yield break; }
        yield return new WaitForSeconds(.2f);
        if (pool.CountInactive != 1 || pool.CountActive != 0)
        { Debug.LogError("ChainLightningPoolPlayCheck: repeated effect not returned"); EditorApplication.Exit(1); yield break; }
        Debug.Log("ChainLightningPoolPlayCheck PASS: original prefab and pool, frozen clock, early timed return, four reuses and restarted timer");
        EditorApplication.Exit(0);
    }
}
