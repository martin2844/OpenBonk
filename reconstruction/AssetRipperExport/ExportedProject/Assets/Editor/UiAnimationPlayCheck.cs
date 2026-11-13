using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class UiAnimationPlayCheck
{
    const string Pending = "UiAnimationPlayCheck.pending";
    static UiAnimationPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("ui animation check").AddComponent<UiAnimationPlayRunner>().Begin(); } }
}

public class UiAnimationPlayRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("UiAnimationPlayCheck: " + reason); }
    static void Call(object obj, string name) => obj.GetType().GetMethod(name, Private).Invoke(obj, null);
    static float Get(object obj, string name) => (float)obj.GetType().GetField(name, Private).GetValue(obj);
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Test(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Test()
    {
        var a = new GameObject("alert"); a.SetActive(false); a.transform.localEulerAngles = new Vector3(0, 0, 37);
        var alert = a.AddComponent<AlertIcon>(); alert.enabled = false; Call(alert, "Awake");
        var b = new GameObject("animated"); b.SetActive(false); b.transform.localEulerAngles = new Vector3(0, 0, 21);
        var animated = b.AddComponent<AnimateUi>(); animated.enabled = false; Call(animated, "Awake");
        Check(Get(alert, "fps") == 7f && Get(alert, "rotationAmount") == 4f && Get(alert, "rotationSpeed") == 3f && Get(alert, "scaleAmount") == .05f && Get(alert, "scaleSpeed") == 6f, "alert defaults");
        Check(animated.rotationAmount == 4f && animated.rotationSpeed == 3f && animated.scaleAmount == .05f && animated.scaleSpeed == 6f && animated.animateScale, "UI defaults");
        yield return new WaitForSeconds(.31f);
        float now = Time.time;
        Call(alert, "Update"); Call(animated, "Update");
        float scale = 1f + Mathf.Sin(now * 6f) * .05f;
        float angle = Mathf.Sin(now * 3f) * 4f;
        Check(Mathf.Abs(a.transform.localScale.x - scale) < .001f && Mathf.Abs(Mathf.DeltaAngle(a.transform.localEulerAngles.z, 37f + angle)) < .01f, "alert rotation and scale");
        Check(Mathf.Abs(b.transform.localScale.x - scale) < .001f && Mathf.Abs(Mathf.DeltaAngle(b.transform.localEulerAngles.z, 21f + angle)) < .01f, "UI rotation and scale");
        a.transform.localScale = Vector3.one * 2f; Call(alert, "Update");
        Check(a.transform.localScale == Vector3.one * 2f, "seven fps gate");
        animated.animateScale = false; b.transform.localScale = Vector3.one * 3f;
        yield return new WaitForSeconds(.16f);
        Call(animated, "Update");
        Check(b.transform.localScale == Vector3.one * 3f, "disabled scale animation");
        Debug.Log("UiAnimationPlayCheck passed: defaults, seven fps gate, sinusoidal scale and rotation, scale opt-out");
        EditorApplication.Exit(0);
    }
}
