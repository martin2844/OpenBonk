using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AttackEndFxPlayCheck
{
    const string Pending = "AttackEndFxPlayCheck.pending";
    static AttackEndFxPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("attack effect check").AddComponent<AttackEndFxPlayRunner>().Begin();
        }
    }
}

public class AttackEndFxPlayRunner : MonoBehaviour
{
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            foreach (var name in new[] { "DiceExplosion", "DiceExplosion6", "FireballExplosion" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + name + ".prefab");
                if (prefab == null) throw new Exception(name + " prefab missing");
                var obj = Instantiate(prefab); obj.SetActive(false);
                foreach (var script in obj.GetComponentsInChildren<MonoBehaviour>(true)) if (!(script is AttackEndFx)) script.enabled = false;
                var effect = obj.GetComponentInChildren<AttackEndFx>(true);
                if (effect == null || effect.ps == null) throw new Exception(name + " effect binding missing");
                effect.ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                obj.SetActive(true);
                if (!effect.ps.isPlaying) throw new Exception(name + " did not play on activation");
                obj.SetActive(false);
                effect.ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                obj.SetActive(true);
                if (!effect.ps.isPlaying) throw new Exception(name + " did not replay");
                Destroy(obj);
            }
            Debug.Log("AttackEndFxPlayCheck passed: three original explosion prefabs play and replay");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
