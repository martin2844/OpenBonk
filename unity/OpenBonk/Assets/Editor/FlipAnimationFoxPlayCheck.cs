using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FlipAnimationFoxPlayCheck
{
    const string Pending = "FlipAnimationFoxPlayCheck.pending";
    static FlipAnimationFoxPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("Fox flip test").AddComponent<FlipAnimationFoxPlayRunner>().Begin();
        }
    }
}

public class FlipAnimationFoxPlayRunner : MonoBehaviour
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("FlipAnimationFoxPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/FoxWizard.prefab");
            Require(prefab != null, "Fox prefab missing");
            var obj = Instantiate(prefab); var flip = obj.GetComponentInChildren<FlipAnimationFox>(true);
            Require(flip != null && flip.animator != null && flip.flipTransform != null && flip.defaultHandhelds.Length > 0 && flip.flippedHandhelds.Length > 0, "prefab bindings");
            flip.enabled = false;
            var update = typeof(FlipAnimationFox).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            flip.animator.Play("Jump", 0, 0.1f); flip.animator.Update(0f); update.Invoke(flip, null);
            Require(flip.flipTransform.localScale == Vector3.one && flip.defaultHandhelds[0].activeSelf && !flip.flippedHandhelds[0].activeSelf, "first jump");
            flip.animator.Play("Jump", 0, 0.7f); flip.animator.Update(0f); update.Invoke(flip, null);
            flip.animator.Play("Jump", 0, 0.1f); flip.animator.Update(0f); update.Invoke(flip, null);
            Require(flip.flipTransform.localScale == new Vector3(-1, 1, 1) && !flip.defaultHandhelds[0].activeSelf && flip.flippedHandhelds[0].activeSelf, "second jump flips");
            flip.animator.Play("Idle", 0, 0f); flip.animator.Update(0f); update.Invoke(flip, null);
            Require(flip.flipTransform.localScale == Vector3.one && flip.defaultHandhelds[0].activeSelf && !flip.flippedHandhelds[0].activeSelf, "interrupted jump resets");
            Debug.Log("FlipAnimationFoxPlayCheck passed: original prefab, alternating jump flips and interruption reset");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
