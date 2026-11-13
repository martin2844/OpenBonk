using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class HoverAnimationsPlayCheck
{
    const string Pending="HoverAnimationsPlayCheck.pending";
    static HoverAnimationsPlayCheck() { EditorApplication.playModeStateChanged+=OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); SessionState.SetBool(Pending,true); EditorApplication.isPlaying=true; }
    static void OnState(PlayModeStateChange state) { if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("Hover animation test").AddComponent<HoverAnimationsPlayRunner>().Begin(); } }
}
public class HoverAnimationsPlayRunner : MonoBehaviour
{
    static void Require(bool condition,string message) { if(!condition) throw new Exception("HoverAnimationsPlayCheck: "+message); }
    static bool Close(float a,float b) => Mathf.Abs(a-b)<.001f;
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/HoverGuy.prefab"); Require(prefab!=null,"prefab missing");
            var root=new GameObject("inactive parent"); root.SetActive(false); var obj=Instantiate(prefab,root.transform);
            var hover=obj.GetComponentInChildren<HoverAnimations>(true);
            Require(hover!=null && hover.player!=null && hover.audioHoverLoop!=null && hover.audioSpin!=null,"prefab bindings");
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var start=typeof(HoverAnimations).GetMethod("Start",flags); var update=typeof(HoverAnimations).GetMethod("Update",flags);
            var late=typeof(HoverAnimations).GetMethod("LateUpdate",flags); var fixedUpdate=typeof(HoverAnimations).GetMethod("FixedUpdate",flags);
            var landingOffset=typeof(HoverAnimations).GetField("landingOffset",flags); var pitch=typeof(HoverAnimations).GetField("currentPitch",flags);
            var defaultPosition=hover.player.localPosition; start.Invoke(hover,null);
            var playerObject=new GameObject("controlled player"); playerObject.SetActive(false); var rb=playerObject.AddComponent<Rigidbody>();
            var movement=playerObject.AddComponent<PlayerMovement>(); typeof(PlayerMovement).GetField("rb",flags).SetValue(movement,rb);
            var player=playerObject.AddComponent<MyPlayer>(); player.playerMovement=movement; movement.enabled=false; player.enabled=false;
            playerObject.SetActive(true); rb.useGravity=false; MyPlayer.Instance=player;
            MyTime.time=1f; MyTime.deltaTime=.1f; movement.grounded=false; rb.velocity=Vector3.zero;
            update.Invoke(hover,null);
            Require(Close((float)pitch.GetValue(hover),.2f),"idle pitch interpolation");
            Require(Close(hover.audioSpin.volume,.063f),"airborne spin volume");
            late.Invoke(hover,null);
            float bob=(Mathf.Sin(MyTime.time*hover.sinSpeed)*.5f+.5f)*hover.height;
            Require(Close(hover.player.localPosition.y,defaultPosition.y+bob),"floating position");
            rb.velocity=new Vector3(20,0,0); MyTime.deltaTime=.2f; late.Invoke(hover,null);
            float target=hover.player.InverseTransformDirection(Vector3.right).x;
            Require(Mathf.Abs(Mathf.DeltaAngle(hover.player.localEulerAngles.z,0f))>1f,"sideways lean");
            rb.velocity=new Vector3(0,-10,0); MyTime.time=2f; fixedUpdate.Invoke(hover,null);
            Require(Close((float)landingOffset.GetValue(hover),0f),"negative acceleration is not a landing");
            rb.velocity=Vector3.zero; MyTime.time=2.3f; fixedUpdate.Invoke(hover,null);
            float amount=(10f-4f)/16f; Require(Close((float)landingOffset.GetValue(hover),Mathf.Lerp(1f,2.3f,amount)),"landing strength");
            float before=hover.player.localPosition.y; MyTime.deltaTime=.1f; late.Invoke(hover,null); late.Invoke(hover,null);
            Require(hover.player.localPosition.y<before-.1f,"landing dip");
            movement.grounded=true; hover.audioSpin.volume=.09f; update.Invoke(hover,null); Require(hover.audioSpin.volume<.04f,"grounded spin fade");
            MyPlayer.Instance=null;
            Debug.Log("HoverAnimationsPlayCheck passed: original prefab, hover, pitch, spin, lean, landing and recovery");
            EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
