using System;
using System.Collections;
using Assets.Scripts.UI.DynamicWindows;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DynamicPromptButtonsPlayCheck {
    const string Pending="DynamicPromptButtonsPlayCheck.pending";
    static DynamicPromptButtonsPlayCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;}
    static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("prompt check runner").AddComponent<DynamicPromptButtonsPlayRunner>().Begin();}}
}
public class DynamicPromptButtonsPlayRunner:MonoBehaviour {
    string error;
    void Capture(string text,string stack,LogType type){if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert)error=text+"\n"+stack;}
    static void Check(bool ok,string msg){if(!ok)throw new Exception("DynamicPromptButtonsPlayCheck: "+msg);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    static Button Bound(DWindowPrompt prompt,string name){foreach(var button in prompt.GetComponentsInChildren<Button>(true))for(int i=0;i<button.onClick.GetPersistentEventCount();i++)if(button.onClick.GetPersistentTarget(i)==prompt && button.onClick.GetPersistentMethodName(i)==name)return button;return null;}
    IEnumerator Run(){
        Application.logMessageReceived+=Capture;new GameObject("events").AddComponent<EventSystem>();new GameObject("audio").AddComponent<AudioListener>();
        var underlying=new GameObject("underlying window").AddComponent<Window>();
        var root=new GameObject("dynamic windows",typeof(RectTransform));root.SetActive(false);var manager=root.AddComponent<DynamicWindows>();manager.contentParent=root.transform;manager.overlay=new GameObject("overlay",typeof(RectTransform)).transform;manager.overlay.SetParent(root.transform);manager.overlay.gameObject.SetActive(false);manager.dynamicWindowPromptPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/DynamicWindowPrompt.prefab");Check(manager.dynamicWindowPromptPrefab!=null,"original prefab");root.SetActive(true);
        int accepted=0;manager.NewWindowPrompt("Exit","Leave?",()=>accepted++);yield return null;
        var prompt=root.GetComponentInChildren<DWindowPrompt>(true);Check(prompt!=null && manager.HasWindows() && manager.overlay.gameObject.activeSelf,"prompt and overlay visible");
        var cancel=Bound(prompt,"Close");var accept=Bound(prompt,"Accept");Check(cancel!=null && accept!=null && cancel!=accept,"original accept and cancel bindings");
        var modal=prompt.GetComponentInChildren<Window>(true);Check(modal!=null && WindowManager.activeWindow==modal,"prompt becomes active window");
        cancel.onClick.Invoke();yield return null;yield return null;
        Check(accepted==0 && !manager.HasWindows() && !manager.overlay.gameObject.activeSelf && WindowManager.activeWindow==underlying,"cancel closes prompt, keeps action untouched and restores window");
        manager.NewWindowPrompt("Restart","Restart?",()=>accepted++);yield return null;prompt=root.GetComponentInChildren<DWindowPrompt>(true);accept=Bound(prompt,"Accept");Check(accept!=null && WindowManager.activeWindow==prompt.GetComponentInChildren<Window>(true),"reopened prompt focuses");
        accept.onClick.Invoke();yield return null;yield return null;
        Check(accepted==1 && !manager.HasWindows() && !manager.overlay.gameObject.activeSelf && WindowManager.activeWindow==underlying,"accept calls action once and restores window");
        Check(error==null,"unexpected Unity error: "+error);Application.logMessageReceived-=Capture;Debug.Log("DynamicPromptButtonsPlayCheck PASSED: original prompt, bound cancel and accept, overlay and window focus");EditorApplication.Exit(0);
    }
}
