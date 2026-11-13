using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ScaleOnAwakePlayCheck
{
    const string Pending = "ScaleOnAwakePlayCheck.pending";
    static ScaleOnAwakePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("scale check").AddComponent<ScaleOnAwakePlayRunner>().Begin();
        }
    }
}

public class ScaleOnAwakePlayRunner : MonoBehaviour
{
    static void Require(bool value, string message) { if (!value) throw new Exception("ScaleOnAwakePlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        foreach (var name in new[] { "BossSpawner", "DynamicWindow", "DynamicWindowPrompt" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + name + ".prefab");
            if (prefab == null) { Debug.LogError("ScaleOnAwakePlayCheck: missing " + name); EditorApplication.Exit(1); yield break; }
            var obj = Instantiate(prefab);
            foreach (var script in obj.GetComponentsInChildren<MonoBehaviour>(true)) if (!(script is ScaleOnAwake)) script.enabled = false;
            var scale = obj.GetComponentInChildren<ScaleOnAwake>(true);
            if (scale == null) { Debug.LogError("ScaleOnAwakePlayCheck: missing component " + name); EditorApplication.Exit(1); yield break; }
            scale.gameObject.SetActive(false); scale.gameObject.SetActive(true);
            var target = (Vector3)typeof(ScaleOnAwake).GetField("defaultScale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scale);
            if (target == Vector3.zero || scale.transform.localScale != Vector3.zero) { Debug.LogError("ScaleOnAwakePlayCheck: invalid start " + name); EditorApplication.Exit(1); yield break; }
            yield return new WaitForSeconds(Mathf.Max(scale.scaleTime, 0f) + .3f);
            try
            {
                Require(Vector3.Distance(scale.transform.localScale, target) < .001f, name + " final scale");
                scale.gameObject.SetActive(false); scale.gameObject.SetActive(true);
                Require(scale.transform.localScale == Vector3.zero, name + " restart");
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
            Destroy(obj);
        }
        Debug.Log("ScaleOnAwakePlayCheck passed: original portal and windows animate and restart");
        EditorApplication.Exit(0);
    }
}
