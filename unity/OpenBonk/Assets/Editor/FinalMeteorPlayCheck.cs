using System;
using System.Collections;
using MilkShake;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FinalMeteorPlayCheck {
    const string Pending="FinalMeteorPlayCheck.pending",Path="Assets/FinalMeteorCheckTemporary.unity";
    static FinalMeteorPlayCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/FinalBossMap.unity",OpenSceneMode.Single);var cinematic=UnityEngine.Object.FindAnyObjectByType<FianlBossCinematic>(FindObjectsInactive.Include);
        if(cinematic==null || cinematic.meteor==null)throw new Exception("Original final-boss meteor reference missing");
        var original=cinematic.meteor.GetComponent<BossMeteor>();if(original==null || original.destination==null || original.explosionFx==null || original.cinematic!=cinematic)throw new Exception("Original meteor scene references missing");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var clone=UnityEngine.Object.Instantiate(cinematic.meteor);clone.SetActive(false);SceneManager.MoveGameObjectToScene(clone,scene);
        var destination=new GameObject("fixture destination");SceneManager.MoveGameObjectToScene(destination,scene);destination.transform.position=new Vector3(3,0,5);
        var cinematicHost=new GameObject("fixture cinematic");cinematicHost.SetActive(false);SceneManager.MoveGameObjectToScene(cinematicHost,scene);var replacement=cinematicHost.AddComponent<FianlBossCinematic>();replacement.shaker=cinematicHost.AddComponent<Shaker>();
        var effectParent=new GameObject("fixture effect parent");SceneManager.MoveGameObjectToScene(effectParent,scene);var explosion=UnityEngine.Object.Instantiate(original.explosionFx);explosion.SetActive(false);SceneManager.MoveGameObjectToScene(explosion,scene);explosion.transform.SetParent(effectParent.transform);
        var meteor=clone.GetComponent<BossMeteor>();meteor.destination=destination.transform;meteor.cinematic=replacement;meteor.explosionFx=explosion;meteor.speed=20f;clone.transform.position=destination.transform.position+Vector3.up*10f;
        EditorSceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true);EditorSceneManager.SaveScene(scene,Path);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("meteor test runner").AddComponent<FinalMeteorPlayRunner>().Begin();}}
}
public class FinalMeteorPlayRunner:MonoBehaviour {
    string error;void Capture(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)error=message+"\n"+stack;}
    static void Check(bool ok,string message){if(!ok)throw new Exception("FinalMeteorPlayCheck: "+message);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    IEnumerator Run(){
        Application.logMessageReceived+=Capture;new GameObject("audio").AddComponent<AudioListener>();var meteor=UnityEngine.Object.FindAnyObjectByType<BossMeteor>(FindObjectsInactive.Include);Check(meteor!=null,"original meteor clone");var explosion=meteor.explosionFx;Vector3 start=meteor.transform.position;meteor.gameObject.SetActive(true);
        yield return new WaitForSeconds(.2f);Check(meteor!=null && meteor.transform.position.y<start.y && meteor.transform.position.y>meteor.destination.position.y && !explosion.activeSelf,"meteor moves toward destination before impact");
        yield return new WaitForSeconds(.6f);Check(meteor==null && explosion!=null && explosion.activeSelf && explosion.transform.parent==null,"impact detaches active explosion and destroys meteor");Check(error==null,"unexpected Unity error: "+error);Application.logMessageReceived-=Capture;Debug.Log("FinalMeteorPlayCheck PASSED: original meteor motion and detached impact effect");EditorApplication.Exit(0);
    }
}
