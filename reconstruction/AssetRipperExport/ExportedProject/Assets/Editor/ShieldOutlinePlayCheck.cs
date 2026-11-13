using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Inventory__Items__Pickups;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ShieldOutlinePlayCheck
{
    const string Pending = "ShieldOutlinePlayCheck.pending";
    static ShieldOutlinePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("shield outline runner").AddComponent<ShieldOutlinePlayRunner>().Begin(); } }
}

public class ShieldOutlinePlayRunner : MonoBehaviour
{
    string error;
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = message + "\n" + stack; }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture;
        var root = new GameObject("player visual"); var player = root.AddComponent<PlayerRenderer>();
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.transform.SetParent(root.transform);
        var skinned = sphere.AddComponent<SkinnedMeshRenderer>(); skinned.sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
        Destroy(sphere.GetComponent<MeshRenderer>()); var material = new Material(Shader.Find("Unlit/Color")); skinned.sharedMaterial = material; player.renderer = skinned; player.shieldColor = Color.cyan; yield return null;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.shield = 5f;
        PlayerHealth.A_Heal?.Invoke(health, 1f, true); yield return null;
        var outline = skinned.GetComponent<Outline>(); Check(outline != null && outline.enabled && skinned.sharedMaterials.Length == 3, "shield adds live outline materials");
        Check(outline.OutlineMode == Outline.Mode.OutlineVisible && outline.OutlineColor == Color.cyan && outline.OutlineWidth == 6f, "original shield outline configuration");
        var fill = skinned.sharedMaterials[2]; Check(fill.GetColor("_OutlineColor") == Color.cyan && Mathf.Approximately(fill.GetFloat("_OutlineWidth"), 6f), "live update applies material properties");
        health.shield = 0f; PlayerHealth.A_Heal?.Invoke(health, 1f, false); yield return null; Check(!outline.enabled && skinned.sharedMaterials.Length == 1, "shield loss removes outline materials");
        health.shield = 1f; PlayerHealth.A_Heal?.Invoke(health, 1f, true); yield return null; Check(outline.enabled && skinned.sharedMaterials.Length == 3, "shield regain restores materials once");
        PlayerHealth.A_Heal?.Invoke(health, 1f, true); yield return null; Check(skinned.sharedMaterials.Length == 3, "repeated heal does not duplicate materials");
        Check(error == null, "unexpected Unity error: " + error); Application.logMessageReceived -= Capture;
        Debug.Log("ShieldOutlinePlayCheck passed: health events, real component lifecycle, shader properties and shield reactivation"); EditorApplication.Exit(0);
    }
}
