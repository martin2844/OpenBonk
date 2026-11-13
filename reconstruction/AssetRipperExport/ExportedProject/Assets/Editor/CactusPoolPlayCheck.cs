using System;
using System.Collections;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CactusPoolPlayCheck
{
    const string Pending = "CactusPoolPlayCheck.pending";
    static CactusPoolPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("pool test").AddComponent<CactusPoolPlayRunner>().Begin(); } }
}
public class CactusPoolPlayRunner : MonoBehaviour
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("CactusPoolPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        var host = new GameObject("original cactus pool"); host.SetActive(false); var pools = host.AddComponent<PoolManager>();
        pools.cactusFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/CactusProjectile.prefab"); host.SetActive(true);
        yield return null;
        GameObject first = null;
        try
        {
            Require(pools.cactusPool != null, "startup creates cactus pool");
            MyTime.time = 10f; first = pools.cactusPool.Get();
            Require(first != null && first.activeSelf && first.GetComponent<ReturnToPool>() != null && pools.cactusPool.CountActive == 1, "original prefab checkout");
            MyTime.time = 10.34f;
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try { Require(first.activeSelf && pools.cactusPool.CountActive == 1, "effect remains active before timeout"); MyTime.time = 10.36f; }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try
        {
            Require(!first.activeSelf && pools.cactusPool.CountInactive == 1, "effect returns after 0.35 game seconds");
            var reused = pools.cactusPool.Get(); Require(reused == first && reused.activeSelf, "pooled effect reused");
            Debug.Log("CactusPoolPlayCheck passed: manager startup, original prefab, timeout and reuse"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
