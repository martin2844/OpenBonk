using System.Collections;
using Inventory__Items__Pickups.Pickups;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class XpEchoReusePlayCheck
{
    const string Pending = "XpEchoReusePlayCheck.pending";
    static XpEchoReusePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("XP echo reuse check").AddComponent<XpEchoReusePlayRunner>().Begin();
        }
    }
}
public class XpEchoReusePlayRunner : MonoBehaviour
{
    public void Begin() => StartCoroutine(Run());
    static bool Close(Color a, Color b) => Vector4.Distance(a, b) < .001f;
    IEnumerator Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/XpPickup.prefab");
        var go = Instantiate(prefab);
        var visuals = go.GetComponent<XpVisuals>();
        var pickup = visuals.pickup;
        pickup.Set(Vector3.zero, 1, 0f);
        visuals.SetEchoXp();
        if (!Close(visuals.ps.main.startColor.color, visuals.c_echo)) { Debug.LogError("XpEchoReusePlayCheck: echo color missing"); EditorApplication.Exit(1); yield break; }
        pickup.AddValue(1);
        if (!Close(visuals.ps.main.startColor.color, visuals.c_echo)) { Debug.LogError("XpEchoReusePlayCheck: same-tier stacking lost echo color"); EditorApplication.Exit(1); yield break; }
        go.SetActive(false);
        go.SetActive(true);
        pickup.Set(Vector3.zero, 1, 0f);
        yield return null;
        if (!Close(visuals.ps.main.startColor.color, visuals.c_low)) { Debug.LogError("XpEchoReusePlayCheck: pooled low-tier XP retains echo color"); EditorApplication.Exit(1); yield break; }
        pickup.AddValue(9);
        if (!Close(visuals.ps.main.startColor.color, visuals.c_mid)) { Debug.LogError("XpEchoReusePlayCheck: medium tier missing"); EditorApplication.Exit(1); yield break; }
        Debug.Log("XpEchoReusePlayCheck PASS: low-tier echo reuse and medium-tier update");
        EditorApplication.Exit(0);
    }
}
