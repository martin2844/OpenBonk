using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PhotosensWarningPlayCheck
{
    const string Pending = "PhotosensWarningPlayCheck.pending";
    static PhotosensWarningPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        foreach (string name in new[] { "MainMenu", "GeneratedMap", "FinalBossMap" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/" + name + ".unity", OpenSceneMode.Single);
            var effects = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PhotosensWarning>(true)).ToArray();
            if (effects.Length != 1 || effects[0].window == null || effects[0].cg == null || effects[0].btn == null) throw new Exception(name + ": warning scene references");
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("warning runner").AddComponent<PhotosensWarningPlayRunner>().Begin(); } }
}
public class PhotosensWarningPlayRunner : MonoBehaviour
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("PhotosensWarning: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        new GameObject("event system", typeof(EventSystem));
        var managerObject = new GameObject("save manager"); managerObject.SetActive(false); var save = managerObject.AddComponent<SaveManager>(); save.config = new ConfigSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save }); SaveManager.loaded = true;
        var host = new GameObject("warning host"); host.SetActive(false); var warning = host.AddComponent<PhotosensWarning>();
        warning.window = new GameObject("warning window", typeof(CanvasGroup)); warning.window.transform.SetParent(host.transform); warning.window.SetActive(false); warning.cg = warning.window.GetComponent<CanvasGroup>();
        var button = new GameObject("confirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(MyButtonTabs)); button.transform.SetParent(warning.window.transform); button.SetActive(false);
        var tab = button.GetComponent<MyButtonTabs>(); tab.background = button.GetComponent<Image>(); warning.btn = tab;
        host.SetActive(true); yield return new WaitForSeconds(2.2f);
        Check(warning.window.activeSelf && button.activeSelf && warning.cg.alpha > .99f, "first launch shows warning and button");
        Check(EventSystem.current.currentSelectedGameObject == button, "confirmation focus");
        warning.Confirm(); Check(save.config.otherSettings.hasAcceptedPhotoSensitivity && !warning.window.activeSelf, "confirmation sets saved choice and closes");
        SaveManager.A_SavesLoaded?.Invoke(); yield return new WaitForSeconds(1f);
        Check(!warning.window.activeSelf, "accepted warning does not reopen");
        Debug.Log("PhotosensWarningPlayCheck passed: scene bindings, first display, focus, confirmation and accepted replay"); EditorApplication.Exit(0);
    }
}
