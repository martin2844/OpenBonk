using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEditor;
using UnityEngine;

public static class BossSpawnerArrowPortCheck
{
    static void Check(bool b, string m) { if (!b) throw new Exception("BossSpawnerArrowPortCheck: " + m); }
    static void Near(Vector3 a, Vector3 b, string m) { Check(Vector3.Distance(a,b)<.01f, m + " got " + a); }
    public static void Run()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BossSpawner.prefab"); Check(prefab,"original boss spawner prefab");
        var root=UnityEngine.Object.Instantiate(prefab); var arrow=root.GetComponentInChildren<global::MinimapArrow>(true); Check(arrow && arrow.target && arrow.meshRenderer,"original arrow references");
        var playerObject=new GameObject("player"); playerObject.SetActive(false); var player=playerObject.AddComponent<MyPlayer>(); var cameraObject=new GameObject("minimap camera"); var camera=cameraObject.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=10; cameraObject.transform.position=new Vector3(10,100,30); player.minimapCamera=camera; var previous=MyPlayer.Instance; MyPlayer.Instance=player;
        try {
            var update=typeof(global::MinimapArrow).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance); var visible=typeof(global::MinimapArrow).GetMethod("IsVisible",BindingFlags.NonPublic|BindingFlags.Instance);
            player.transform.position=Vector3.zero; arrow.target.position=new Vector3(20,75,0); arrow.meshRenderer.enabled=false; update.Invoke(arrow,null); Check((bool)visible.Invoke(arrow,null),"far target enables arrow");
            update.Invoke(arrow,null); Near(arrow.transform.position,new Vector3(18,80,30),"east placement relative to minimap camera"); Check(Mathf.Abs(Mathf.DeltaAngle(arrow.transform.eulerAngles.y,90))<.01f,"east rotation");
            arrow.target.position=new Vector3(0,-25,20); update.Invoke(arrow,null); Near(arrow.transform.position,new Vector3(10,80,38),"north placement ignoring height"); Check(Mathf.Abs(Mathf.DeltaAngle(arrow.transform.eulerAngles.y,0))<.01f,"north rotation");
            arrow.target.position=new Vector3(9,999,0); update.Invoke(arrow,null); Check(!(bool)visible.Invoke(arrow,null),"near target hides arrow");
            arrow.target.position=new Vector3(9.5f,0,0); update.Invoke(arrow,null); Check(!(bool)visible.Invoke(arrow,null),"threshold does not show hidden arrow");
            arrow.target.position=new Vector3(10,0,0); update.Invoke(arrow,null); Check((bool)visible.Invoke(arrow,null),"distant target re-enables arrow");
            Debug.Log("BossSpawnerArrowPortCheck passed: original prefab distance, visibility, position and rotation");
        } finally { MyPlayer.Instance=previous; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(cameraObject); }
    }
}
