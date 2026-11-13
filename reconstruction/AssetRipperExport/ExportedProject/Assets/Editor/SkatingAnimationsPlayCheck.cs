using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SkatingAnimationsPlayCheck
{
    const string Pending="SkatingAnimationsPlayCheck.pending";
    static SkatingAnimationsPlayCheck() { EditorApplication.playModeStateChanged+=OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); SessionState.SetBool(Pending,true); EditorApplication.isPlaying=true; }
    static void OnState(PlayModeStateChange state) { if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("Calcium skating check").AddComponent<SkatingAnimationsPlayRunner>().Begin(); } }
}
public class SkatingAnimationsPlayRunner : MonoBehaviour
{
    static void Require(bool value,string message) { if(!value) throw new Exception("SkatingAnimationsPlayCheck: "+message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Calcium.prefab"); Require(prefab!=null,"prefab missing");
            var root=new GameObject("controlled player"); root.SetActive(false); root.transform.position=Vector3.up;
            var body=root.AddComponent<Rigidbody>(); body.useGravity=false; var movement=root.AddComponent<PlayerMovement>(); movement.enabled=false;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic; typeof(PlayerMovement).GetField("rb",flags).SetValue(movement,body);
            typeof(PlayerMovement).GetField("playerHeight",flags).SetValue(movement,2f); movement.orientation=root.transform; movement.whatIsGround=1; movement.whatIsGroundOnly=1;
            var obj=Instantiate(prefab,root.transform); var skating=obj.GetComponentInChildren<SkatingAnimations>(true);
            Require(skating!=null && skating.animator!=null && skating.skateSfx!=null,"prefab bindings");
            foreach(var script in obj.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled=false;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.position=new Vector3(0,-.5f,0); floor.transform.localScale=new Vector3(20,1,20);
            root.SetActive(true); Physics.SyncTransforms();
            var awake=typeof(SkatingAnimations).GetMethod("Awake",flags); var update=typeof(SkatingAnimations).GetMethod("Update",flags); var fixedUpdate=typeof(SkatingAnimations).GetMethod("FixedUpdate",flags);
            awake.Invoke(skating,null);
            Require(typeof(SkatingAnimations).GetField("playerMovement",flags).GetValue(skating)==movement,"root movement binding");
            fixedUpdate.Invoke(skating,null); Require(skating.animator.GetBool("idle"),"stationary idle");
            typeof(PlayerMovement).GetField("y",flags).SetValue(movement,1f); body.velocity=new Vector3(0,0,5);
            fixedUpdate.Invoke(skating,null); Require(!skating.animator.GetBool("idle") && skating.animator.GetBool("kicking"),"moving and initial kick");
            typeof(SkatingAnimations).GetField("nextKickTime",flags).SetValue(skating,float.MaxValue);
            for(int i=0;i<12;i++) fixedUpdate.Invoke(skating,null);
            Require(!skating.animator.GetBool("kicking"),"steady speed and direction");
            obj.transform.Rotate(0,90,0); fixedUpdate.Invoke(skating,null); Require(skating.animator.GetBool("kicking"),"turn kick");
            skating.Kick(); Require(Mathf.Approximately((float)typeof(SkatingAnimations).GetField("skateVolumeBoost",flags).GetValue(skating),3f),"kick sound boost");
            skating.skateSfx.clip=AudioClip.Create("test rolling",44100,1,44100,false); skating.skateSfx.loop=true;
            Require(movement.IsTouchingGround(),"test ground contact");
            update.Invoke(skating,null); Require(skating.skateSfx.isPlaying,"rolling audio begins");
            Debug.Log("SkatingAnimationsPlayCheck passed: original Calcium prefab, idle, kick, turn, boost and grounded audio");
            EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
