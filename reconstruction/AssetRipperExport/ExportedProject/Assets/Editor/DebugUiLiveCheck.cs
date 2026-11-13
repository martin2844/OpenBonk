using System;
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DebugUiLiveCheck
{
    const string Pending="DebugUiLiveCheck.pending";
    static DebugUiLiveCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); SessionState.SetBool(Pending,true); EditorApplication.isPlaying=true; }
    static void State(PlayModeStateChange state) { if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("debug check runner").AddComponent<DebugUiLiveRunner>().Begin(); } }
}
public class DebugUiLiveRunner : MonoBehaviour
{
    static void Check(bool b,string m) { if(!b) throw new Exception("DebugUiLiveCheck: "+m); }
    public void Begin() { StartCoroutine(Run()); }
    static TextMeshProUGUI Label(Transform parent,string name) { var obj=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)); obj.transform.SetParent(parent); obj.GetComponent<TextMeshProUGUI>().text="placeholder"; return obj.GetComponent<TextMeshProUGUI>(); }
    IEnumerator Run()
    {
        var root=new GameObject("debug ui"); root.SetActive(false); var ui=root.AddComponent<DebugUI>(); ui.t_fps=Label(root.transform,"fps"); ui.t_speed=Label(root.transform,"speed"); ui.t_ram=Label(root.transform,"ram"); root.SetActive(true);
        try {
            Check(!ui.t_fps.gameObject.activeSelf && !ui.t_speed.gameObject.activeSelf && !ui.t_ram.gameObject.activeSelf,"default settings hide debug values");
            CurrentSettings.A_SettingUpdated?.Invoke("debug_fps",0,1); CurrentSettings.A_SettingUpdated?.Invoke("debug_speed",0,1); CurrentSettings.A_SettingUpdated?.Invoke("debug_ram",0,1);
            Check(ui.t_fps.gameObject.activeSelf && ui.t_speed.gameObject.activeSelf && ui.t_ram.gameObject.activeSelf,"setting events enable labels");
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return new WaitForSecondsRealtime(.8f);
        try {
            Check(ui.t_fps.text.StartsWith("FPS: ") && ui.t_fps.text!="placeholder","fps updated"); Check(ui.t_speed.text=="SPD: 0","no player speed"); Check(ui.t_ram.text.StartsWith("RAM: "),"memory updated");
            CurrentSettings.A_SettingUpdated?.Invoke("debug_fps",1,0); Check(!ui.t_fps.gameObject.activeSelf,"setting hides fps"); Destroy(root); Debug.Log("DebugUiLiveCheck passed: default hidden labels, settings events and live sample updates"); EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
