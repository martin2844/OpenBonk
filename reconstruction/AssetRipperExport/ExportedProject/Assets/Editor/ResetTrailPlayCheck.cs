using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ResetTrailPlayCheck
{
    const string Pending = "ResetTrailPlayCheck.pending";
    static ResetTrailPlayCheck() { EditorApplication.playModeStateChanged += StateChanged; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("trail reset runner").AddComponent<ResetTrailPlayRunner>().Begin();
    }
}

public class ResetTrailPlayRunner : MonoBehaviour
{
    public void Begin() { StartCoroutine(Check()); }
    IEnumerator Check()
    {
        yield return null;
        try
        {
            foreach (string name in new[] { "GhostProjectile", "AngrySoulProjectile" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + name + ".prefab");
                if (prefab == null) throw new Exception(name + " missing");
                var projectile = Instantiate(prefab);
                var reset = projectile.GetComponent<ResetTrail>();
                if (reset == null || reset.trailRenderer == null) throw new Exception(name + " trail wiring missing");
                var trail = reset.trailRenderer;
                trail.Clear(); trail.AddPosition(Vector3.zero); trail.AddPosition(Vector3.right);
                if (trail.positionCount < 2) throw new Exception(name + " did not retain fixture positions");
                reset.enabled = false;
                if (trail.positionCount != 0) throw new Exception(name + " trail was not cleared on disable");
                reset.enabled = true;
                reset.trailRenderer = null;
                typeof(ResetTrail).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(reset, null);
                if (reset.trailRenderer != trail) throw new Exception(name + " OnValidate did not find trail");
                Destroy(projectile);
            }
            Debug.Log("ResetTrailPlayCheck passed: real ghost projectiles clear reused trails and resolve their renderers");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
