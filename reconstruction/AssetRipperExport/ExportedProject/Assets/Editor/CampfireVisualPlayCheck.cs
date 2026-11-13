using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CampfireVisualPlayCheck
{
    const string Pending = "CampfireVisualPlayCheck.pending";
    static CampfireVisualPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Campfire.prefab");
        if (prefab == null || prefab.GetComponent<Campfire>() == null) throw new Exception("Campfire prefab missing");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var obj = UnityEngine.Object.Instantiate(prefab); obj.name = "Campfire visual test"; obj.SetActive(false);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/CampfireVisualCheckTemporary.unity");
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("campfire test runner").AddComponent<CampfireVisualPlayRunner>().Begin();
        }
    }
}
public class CampfireVisualPlayRunner : MonoBehaviour
{
    string error;
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = message + "\n" + stack; }
    static void Check(bool ok, string message) { if (!ok) throw new Exception("CampfireVisualPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture;
        var fire = UnityEngine.Object.FindAnyObjectByType<Campfire>(FindObjectsInactive.Include);
        Check(fire != null && fire.createFx != null, "original campfire and particle effect");
        Vector3 position = new Vector3(4, 2, 6); fire.StartFire(position);
        Check(fire.gameObject.activeSelf && fire.createFx.gameObject.activeSelf && fire.createFx.transform.parent == null && Vector3.Distance(fire.createFx.transform.position, position + Vector3.up * .1f) < .001f, "start activates and detaches effect at position");
        yield return new WaitForSeconds(.35f);
        Check(Vector3.Distance(fire.transform.localScale, Vector3.one * 1.5f) < .01f, "fire expands to original scale");
        fire.EndFire(); yield return new WaitForSeconds(.35f);
        Check(!fire.gameObject.activeSelf && !fire.createFx.gameObject.activeSelf, "fire and effect deactivate after shrinking");
        fire.StartFire(position + Vector3.right); yield return new WaitForSeconds(.35f);
        Check(fire.gameObject.activeSelf && fire.createFx.gameObject.activeSelf && Vector3.Distance(fire.transform.localScale, Vector3.one * 1.5f) < .01f, "fire can be reused");
        Check(error == null, "unexpected Unity error: " + error); Application.logMessageReceived -= Capture;
        Debug.Log("CampfireVisualPlayCheck passed: original prefab start, animation, stop and reuse"); EditorApplication.Exit(0);
    }
}
