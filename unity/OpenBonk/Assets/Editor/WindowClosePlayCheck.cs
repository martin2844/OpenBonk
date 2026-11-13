using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class WindowClosePlayCheck {
    const string Pending="WindowClosePlayCheck.pending";
    static WindowClosePlayCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;}
    static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("window check runner").AddComponent<WindowClosePlayRunner>().Begin();}}
}
public class WindowClosePlayRunner:MonoBehaviour {
    static void Check(bool ok,string msg){if(!ok)throw new Exception("WindowClosePlayCheck: "+msg);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    IEnumerator Run(){
        new GameObject("events").AddComponent<EventSystem>();
        var first=new GameObject("first").AddComponent<Window>();var second=new GameObject("second").AddComponent<Window>();var third=new GameObject("third").AddComponent<Window>();yield return null;
        Check(WindowManager.GetNumOpenWindows()==3 && WindowManager.activeWindow==third,"windows open as a stack");
        third.Close();Check(!third.gameObject.activeSelf && WindowManager.GetNumOpenWindows()==2 && WindowManager.activeWindow==second,"Close deactivates top window and restores previous focus");
        third.gameObject.SetActive(true);Check(WindowManager.GetNumOpenWindows()==3 && WindowManager.activeWindow==third,"closed window reopens");
        WindowManager.CloseAll();Check(!first.gameObject.activeSelf && !second.gameObject.activeSelf && !third.gameObject.activeSelf && WindowManager.GetNumOpenWindows()==0 && WindowManager.activeWindow==null,"CloseAll safely closes all while callbacks mutate the stack");
        first.gameObject.SetActive(true);Check(WindowManager.GetNumOpenWindows()==1 && WindowManager.activeWindow==first,"window manager remains usable after close all");first.Close();
        Debug.Log("WindowClosePlayCheck PASSED: close, restore previous window, close all, reopen");EditorApplication.Exit(0);
    }
}
