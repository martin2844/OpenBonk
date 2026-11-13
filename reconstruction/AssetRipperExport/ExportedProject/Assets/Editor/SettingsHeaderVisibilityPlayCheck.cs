using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SettingsHeaderVisibilityPlayCheck
{
    const string Pending = "SettingsHeaderVisibilityPlayCheck.pending";
    static SettingsHeaderVisibilityPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false); new GameObject("header test runner").AddComponent<SettingsHeaderVisibilityPlayRunner>().Begin();
    }
}
public class SettingsHeaderVisibilityPlayRunner : MonoBehaviour
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("SettingsHeaderVisibilityPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard()
    {
        var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; }
    }
    IEnumerator Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/=HEADER=.prefab");
        Check(prefab != null, "original header prefab");
        var manager = new GameObject("save manager").AddComponent<SaveManager>(); manager.config = new ConfigSaveFile();
        manager.config.cfGameSettings.show_advanced_settings = 0;
        var advanced = Instantiate(prefab); advanced.GetComponent<SettingsHeaderMonoObject>().Set(new SettingHeader(25, "ADVANCED"));
        var basic = Instantiate(prefab); basic.GetComponent<SettingsHeaderMonoObject>().Set(new SettingHeader(0, "GAME"));
        yield return null;
        Check(!advanced.activeSelf && basic.activeSelf, "Start hides advanced and leaves ordinary header");
        manager.config.cfGameSettings.show_advanced_settings = 1; CurrentSettings.A_SettingUpdated?.Invoke("show_advanced_settings", 0, 1);
        Check(advanced.activeSelf && basic.activeSelf, "setting event enables inactive header");
        manager.config.cfGameSettings.show_advanced_settings = 0; CurrentSettings.A_SettingUpdated?.Invoke("show_advanced_settings", 1, 0);
        Check(!advanced.activeSelf && basic.activeSelf, "setting event hides advanced header again");
        Destroy(advanced); Destroy(basic); yield return null;
        Check(CurrentSettings.A_SettingUpdated == null, "destroy removed listeners");
        Debug.Log("SettingsHeaderVisibilityPlayCheck passed: original prefab, actual lifecycle and setting events");
        EditorApplication.Exit(0);
    }
}
