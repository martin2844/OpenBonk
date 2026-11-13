using System;
using System.Collections;
using Assets.Scripts.UI.Debug;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ConsoleInputGuardPlayCheck {
    const string Pending="ConsoleInputGuardPlayCheck.pending";
    static ConsoleInputGuardPlayCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;}
    static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("console guard runner").AddComponent<ConsoleInputGuardRunner>().Begin();}}
}
public class ConsoleInputGuardRunner:MonoBehaviour {
    static void Check(bool ok,string message){if(!ok)throw new Exception("ConsoleInputGuardPlayCheck: "+message);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    IEnumerator Run(){
        Check(!PlayerInput.IsConsoleOpen(),"no console allows input");
        var host=new GameObject("debug host");host.SetActive(false);var console=host.AddComponent<DebugConsole>();console.consoleTransform=new GameObject("console panel");console.consoleTransform.transform.SetParent(host.transform);console.consoleTransform.SetActive(false);host.SetActive(true);
        Check(DebugConsole.Instance==console && !PlayerInput.IsConsoleOpen(),"singleton with hidden panel allows input");
        console.consoleTransform.SetActive(true);Check(PlayerInput.IsConsoleOpen(),"visible console blocks gameplay input");
        host.SetActive(false);Check(!PlayerInput.IsConsoleOpen(),"inactive parent does not block input");host.SetActive(true);
        var duplicate=new GameObject("duplicate console");duplicate.AddComponent<DebugConsole>();yield return null;
        Check(duplicate==null && DebugConsole.Instance==console,"duplicate console is removed");
        console.consoleTransform.SetActive(false);Check(!PlayerInput.IsConsoleOpen(),"closing console restores input");
        UnityEngine.Debug.Log("ConsoleInputGuardPlayCheck PASSED: singleton, visible, hidden and inactive-parent states");EditorApplication.Exit(0);
    }
}
