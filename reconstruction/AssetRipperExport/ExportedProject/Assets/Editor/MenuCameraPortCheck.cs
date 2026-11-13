using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MenuCameraPortCheck
{
    const string Pending = "MenuCameraPortCheck.pending", Path = "Assets/MenuCameraTemporary.unity";
    static MenuCameraPortCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        var source = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Single);
        var original = UnityEngine.Object.FindAnyObjectByType<MenuCamera>(FindObjectsInactive.Include);
        if (original == null || original.camera == null || original.defaultTransform == null || original.characterSelectionTransform == null)
            throw new Exception("MenuCamera original scene bindings absent");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var test = new GameObject("scene camera copy"); SceneManager.MoveGameObjectToScene(test, scene);
        var view = test.AddComponent<Camera>(); view.useOcclusionCulling = true;
        var component = test.AddComponent<MenuCamera>(); component.camera = view;
        var mainTarget = new GameObject("main camera target"); SceneManager.MoveGameObjectToScene(mainTarget, scene);
        var characterTarget = new GameObject("character camera target"); SceneManager.MoveGameObjectToScene(characterTarget, scene);
        mainTarget.transform.SetPositionAndRotation(original.defaultTransform.position, original.defaultTransform.rotation);
        characterTarget.transform.SetPositionAndRotation(original.characterSelectionTransform.position, original.characterSelectionTransform.rotation);
        component.defaultTransform = mainTarget.transform; component.characterSelectionTransform = characterTarget.transform;
        test.transform.SetPositionAndRotation(mainTarget.transform.position, mainTarget.transform.rotation);
        EditorSceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source, true); EditorSceneManager.SaveScene(scene, Path);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        { SessionState.SetBool(Pending, false); new GameObject("runner").AddComponent<MenuCameraPortRunner>().Begin(); }
    }
}
public class MenuCameraPortRunner : MonoBehaviour
{
    static void Require(bool ok, string msg) { if (!ok) throw new Exception("MenuCameraPortCheck: " + msg); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard()
    {
        var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; }
    }
    IEnumerator Run()
    {
        var component = FindAnyObjectByType<MenuCamera>(); yield return null;
        Require(component != null && !component.camera.useOcclusionCulling, "Start disables occlusion culling");
        Require(Vector3.Distance(component.defaultTransform.position, component.characterSelectionTransform.position) > .01f, "original targets differ");
        float initialDistance = Vector3.Distance(component.transform.position, component.characterSelectionTransform.position);
        component.GoToCharacters(); yield return new WaitForSeconds(.3f);
        float selectedDistance = Vector3.Distance(component.transform.position, component.characterSelectionTransform.position);
        Require(selectedDistance < initialDistance && selectedDistance > 0.001f, "camera interpolates toward character target");
        float returnDistance = Vector3.Distance(component.transform.position, component.defaultTransform.position);
        Require(returnDistance > .01f, "camera left main position");
        component.GoToMain(); yield return new WaitForSeconds(.3f);
        Require(Vector3.Distance(component.transform.position, component.defaultTransform.position) < returnDistance, "camera interpolates back");
        Debug.Log("MenuCameraPortCheck passed: scene target bindings, occlusion setting and both camera transitions"); EditorApplication.Exit(0);
    }
}
