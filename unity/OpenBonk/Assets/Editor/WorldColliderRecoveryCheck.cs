using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class WorldColliderRecoveryCheck
{
    const string Pending="WorldColliderRecoveryCheck.pending", Path="Assets/WorldColliderTemporary.unity";
    static WorldColliderRecoveryCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Single);
        var original=UnityEngine.Object.FindAnyObjectByType<WorldColliderTrigger>(FindObjectsInactive.Include);
        if(original==null || original.gameObject.name!="WorldCollider" || original.GetComponent<Collider>()==null) throw new Exception("actual scene WorldCollider binding absent");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive); EditorSceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source,true); EditorSceneManager.SaveScene(scene,Path);
        SessionState.SetBool(Pending,true); EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange state) { if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false);new GameObject("recovery runner").AddComponent<WorldColliderRecoveryRunner>().Begin(); } }
}
public class WorldColliderRecoveryRunner : MonoBehaviour
{
    static void Require(bool ok,string s) { if(!ok) throw new Exception("WorldColliderRecoveryCheck: "+s); }
    public void Begin()=>StartCoroutine(Guard());
    IEnumerator Guard() { var t=Run();while(true){bool more;try{more=t.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return t.Current;} }
    IEnumerator Run()
    {
        var world=new GameObject("WorldCollider"); world.layer=LayerMask.NameToLayer("Object"); world.transform.position=new Vector3(0f,-10f,0f); var box=world.AddComponent<BoxCollider>();box.size=new Vector3(20f,1f,20f);var component=world.AddComponent<WorldColliderTrigger>();
        var floor=typeof(WorldColliderTrigger).GetMethod("IsFloor",BindingFlags.Instance|BindingFlags.NonPublic);
        var ceiling=typeof(WorldColliderTrigger).GetMethod("IsCeiling",BindingFlags.Instance|BindingFlags.NonPublic);
        Require((bool)floor.Invoke(component,new object[]{Vector3.down}) && !(bool)floor.Invoke(component,new object[]{Vector3.up}) && !(bool)floor.Invoke(component,new object[]{new Vector3(1f,-.9f,0f)}),"floor normal filter");
        Require((bool)ceiling.Invoke(component,new object[]{Vector3.up}) && !(bool)ceiling.Invoke(component,new object[]{Vector3.down}),"ceiling angle");
        var safe=GameObject.CreatePrimitive(PrimitiveType.Cube);safe.name="safe ground";safe.layer=LayerMask.NameToLayer("Ground");safe.transform.position=new Vector3(100f,-.5f,100f);safe.transform.localScale=new Vector3(100f,1f,100f);
        var managerObject=new GameObject("test manager");managerObject.SetActive(false);var manager=managerObject.AddComponent<GameManager>();GameManager.Instance=manager;manager.whatIsGround=1<<safe.layer;
        var playerObject=new GameObject("test player");playerObject.SetActive(false);playerObject.layer=LayerMask.NameToLayer("Player");playerObject.transform.position=new Vector3(0f,-8.5f,0f);
        Physics.IgnoreLayerCollision(world.layer,playerObject.layer,false);
        playerObject.AddComponent<SphereCollider>();var body=playerObject.AddComponent<Rigidbody>();body.useGravity=true;body.constraints=RigidbodyConstraints.FreezeRotation;
        var player=playerObject.AddComponent<MyPlayer>();var movement=playerObject.AddComponent<PlayerMovement>();player.playerMovement=movement;
        typeof(PlayerMovement).GetField("rb",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(movement,body);
        typeof(PlayerMovement).GetProperty("lastGroundedPosition").GetSetMethod(true).Invoke(movement,new object[]{new Vector3(100f,1f,100f)});
        playerObject.SetActive(true);MyPlayer.Instance=player;Physics.SyncTransforms();
        yield return new WaitForSeconds(1f);
        Require(body.position.x>75f && body.position.z>75f && body.position.y>=.45f,"floor collision returned player to safe ground; actual="+body.position);
        Debug.Log("WorldColliderRecoveryCheck passed: original scene component, actual collision, floor/ceiling filters and player safe-ground teleport");EditorApplication.Exit(0);
    }
}
