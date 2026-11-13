using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RobinetteAudioPlayCheck
{
    const string Pending="RobinetteAudioPlayCheck.pending";
    static RobinetteAudioPlayCheck() { EditorApplication.playModeStateChanged+=OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); SessionState.SetBool(Pending,true); EditorApplication.isPlaying=true; }
    static void OnState(PlayModeStateChange state) { if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("Robinette audio check").AddComponent<RobinetteAudioPlayRunner>().Begin(); } }
}
public class RobinetteAudioPlayRunner : MonoBehaviour
{
    static void Require(bool value,string message) { if(!value) throw new Exception("RobinetteAudioPlayCheck: "+message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try {
            Require(Time.deltaTime>0,"nonzero frame delta");
            var playerObject=new GameObject("controlled player"); playerObject.SetActive(false); var player=playerObject.AddComponent<MyPlayer>(); player.enabled=false; playerObject.SetActive(true); MyPlayer.Instance=player;
            var method=typeof(RobinetteAudio).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance);
            foreach(var name in new[]{"Robinette","Ninja"}) {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/GameObject/{name}.prefab"); Require(prefab!=null,name+" prefab missing");
                var holder=new GameObject(name+" parent"); holder.SetActive(false); var obj=Instantiate(prefab,holder.transform);
                var component=obj.GetComponentInChildren<RobinetteAudio>(true); Require(component!=null && component.audioSource!=null && component.animator!=null,name+" bindings");
                foreach(var script in obj.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled=false;
                holder.SetActive(true); var source=component.audioSource; source.clip=AudioClip.Create("test hover",44100,1,44100,false); source.loop=true; source.volume=0;
                component.animator.SetBool("grounded",false); MyTime.paused=false;
                for(int i=0;i<100;i++) method.Invoke(component,null);
                Require(source.isPlaying && source.volume>.05f && Mathf.Abs(source.volume-component.desiredVolume)<.01f,name+" airborne playback");
                MyTime.paused=true; component.animator.SetBool("grounded",true); float old=source.volume; method.Invoke(component,null);
                Require(Mathf.Approximately(source.volume,old) && source.isPlaying,name+" pause guard");
                MyTime.paused=false; for(int i=0;i<100;i++) method.Invoke(component,null);
                Require(!source.isPlaying && source.volume==0f,name+" grounded fade and stop");
                MyPlayer.Instance=null; component.animator.SetBool("grounded",false); method.Invoke(component,null);
                Require(source.volume==0f && !source.isPlaying,name+" no-player guard"); MyPlayer.Instance=player;
            }
            MyPlayer.Instance=null; Debug.Log("RobinetteAudioPlayCheck passed: original Robinette and Ninja prefabs, airborne, grounded, pause and no-player paths"); EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
