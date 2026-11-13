using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StartBlackOverlayPlayCheck
{
    const string Pending = "StartBlackOverlayPlayCheck.pending";
    static StartBlackOverlayPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        foreach (string name in new[] { "GeneratedMap", "FinalBossMap" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/" + name + ".unity", OpenSceneMode.Single);
            var components = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<StartBlackOverlay>(true)).ToArray();
            if (components.Length != 1 || components[0].overlay == null) throw new Exception(name + " overlay binding missing");
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("overlay runner").AddComponent<StartBlackOverlayPlayRunner>().Begin(); } }
}
public class StartBlackOverlayPlayRunner : MonoBehaviour
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("StartBlackOverlay: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        var saveObj = new GameObject("save"); saveObj.SetActive(false); var save = saveObj.AddComponent<SaveManager>(); save.config = new ConfigSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
        MapGenerationController.isGenerating = true;
        var root = new GameObject("overlay"); root.SetActive(false); var image = root.AddComponent<RawImage>(); image.enabled = false; var overlay = root.AddComponent<StartBlackOverlay>(); overlay.overlay = image; root.SetActive(true); yield return null;
        Check(image.enabled, "generating shows overlay");
        MapGenerationController.isGenerating = false; MapGenerationController.A_GenerationComplete?.Invoke(); yield return null; yield return null;
        Check(!image.enabled, "generation completion hides overlay");
        Destroy(root); yield return null; Check(MapGenerationController.A_GenerationComplete == null, "unsubscribed after destroy");
        save.config.cfGameSettings.super_quick_resets = 1;
        var fast = new GameObject("quick reset"); fast.SetActive(false); var fastImage = fast.AddComponent<RawImage>(); var fastOverlay = fast.AddComponent<StartBlackOverlay>(); fastOverlay.overlay = fastImage; fast.SetActive(true); yield return null;
        Check(!fast.activeSelf && !fastImage.enabled, "quick reset disables overlay");
        Debug.Log("StartBlackOverlayPlayCheck passed: real scene bindings, generation show/hide, unsubscribe and quick reset"); EditorApplication.Exit(0);
    }
}
