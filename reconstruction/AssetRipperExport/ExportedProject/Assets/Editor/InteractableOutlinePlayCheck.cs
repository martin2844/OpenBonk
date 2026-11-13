using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class InteractableOutlinePlayCheck
{
    const string Pending = "InteractableOutlinePlayCheck.pending";
    static InteractableOutlinePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("interactable outline runner").AddComponent<InteractableOutlinePlayRunner>().Begin(); } }
}
public class InteractableOutlinePlayRunner : MonoBehaviour
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ShrineMaoi.prefab");
        Check(prefab != null, "Moai prefab missing"); var obj = Instantiate(prefab);
        var interactable = obj.GetComponent<InteractableShrineMoai>(); var renderer = obj.GetComponent<Renderer>();
        Check(interactable != null && interactable.showOutline && renderer != null, "original Moai root configuration");
        yield return null;
        int initial = renderer.sharedMaterials.Length;
        interactable.StartHover(null); yield return null;
        var outline = obj.GetComponent<Outline>();
        Check(outline != null && outline.enabled && renderer.sharedMaterials.Length == initial + 2, "hover attaches actual outline materials");
        Check(outline.OutlineMode == Outline.Mode.OutlineVisible && outline.OutlineColor == interactable.GetColor() && Mathf.Approximately(outline.OutlineWidth, 5f), "original mode/color/width");
        var fill = renderer.sharedMaterials[initial + 1]; Check(fill.GetColor("_OutlineColor") == interactable.GetColor() && Mathf.Approximately(fill.GetFloat("_OutlineWidth"), 5f), "outline material updated");
        interactable.StartHover(null); yield return null; Check(obj.GetComponents<Outline>().Length == 1 && renderer.sharedMaterials.Length == initial + 2, "repeated hover is idempotent");
        interactable.StopHover(); yield return null; Check(obj.GetComponent<Outline>() == null && renderer.sharedMaterials.Length == initial, "leave destroys outline and restores materials");
        interactable.showOutline = false; interactable.StartHover(null); yield return null; Check(obj.GetComponent<Outline>() == null && renderer.sharedMaterials.Length == initial, "disabled outline does not add component");
        Destroy(obj); Debug.Log("InteractableOutlinePlayCheck passed: original Moai hover outline, material lifecycle, repeated hover and opt-out"); EditorApplication.Exit(0);
    }
}
