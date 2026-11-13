using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.Inventory__Items__Pickups;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DeathPostProcessingPlayCheck
{
    const string Pending = "DeathPostProcessingPlayCheck.pending", Path = "Assets/DeathPostProcessingTemporary.unity";
    static DeathPostProcessingPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        PostProcessProfile selected = null;
        foreach (string name in new[] { "GeneratedMap", "FinalBossMap" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/" + name + ".unity", OpenSceneMode.Single);
            var effect = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<DeathPP>(true)).Single();
            if (effect.volume == null || effect.volume.sharedProfile == null || !effect.volume.sharedProfile.HasSettings<ColorGrading>() || !effect.volume.sharedProfile.HasSettings<Vignette>()) throw new Exception(name + ": original death profile missing");
            if (selected == null) selected = effect.volume.sharedProfile;
        }
        var fresh = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("death effect test"); root.SetActive(false);
        var volume = root.AddComponent<PostProcessVolume>(); volume.sharedProfile = selected;
        root.AddComponent<DeathPP>().volume = volume;
        EditorSceneManager.SaveScene(fresh, Path);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("death effect runner").AddComponent<DeathPostProcessingPlayRunner>().Begin(); } }
}
public class DeathPostProcessingPlayRunner : MonoBehaviour
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception("DeathPostProcessingPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        var effect = UnityEngine.Object.FindAnyObjectByType<DeathPP>(FindObjectsInactive.Include);
        effect.gameObject.SetActive(true); yield return null;
        var grading = effect.volume.profile.GetSetting<ColorGrading>(); var vignette = effect.volume.profile.GetSetting<Vignette>();
        grading.contrast.value = 0; grading.saturation.value = 0; vignette.intensity.value = 0; vignette.smoothness.value = 0;
        PlayerHealth.A_Died?.Invoke();
        Check(grading.active && grading.enabled.value && Mathf.Abs(grading.postExposure.value - 2f) < .001f, "death event enables grading and exposure flash");
        yield return new WaitForSeconds(.3f);
        Check(grading.postExposure.value < 2f && grading.postExposure.value >= 0f, "exposure fades");
        Check(grading.contrast.value > 0 && grading.contrast.value < 20 && grading.saturation.value < 0 && grading.saturation.value > -40, "contrast and saturation approach targets");
        Check(vignette.intensity.value > 0 && vignette.intensity.value < .3f && vignette.smoothness.value > 0 && vignette.smoothness.value < .4f, "vignette approaches targets");
        Destroy(effect.gameObject); yield return null;
        PlayerHealth.A_Died?.Invoke();
        Debug.Log("DeathPostProcessingPlayCheck passed: scene bindings, death event, flash and interpolation, cleanup"); EditorApplication.Exit(0);
    }
}
