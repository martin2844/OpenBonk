using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEditor;
using UnityEngine;

public static class MinimapIconPortCheck
{
    static void Check(bool b,string m) { if(!b) throw new Exception("MinimapIconPortCheck: "+m); }
    static void Call(object o,string n) => o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
    static void Rotation(Transform t,Quaternion expected,string m) { Check(Quaternion.Angle(t.rotation,expected)<.01f,m+" got "+t.eulerAngles); }
    public static void Run()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/MagnetShrine.prefab"); Check(prefab,"original shrine prefab"); var root=UnityEngine.Object.Instantiate(prefab); var icon=root.GetComponentInChildren<MinimapIcon>(true); Check(icon,"original icon component");
        var playerObject=new GameObject("player"); playerObject.SetActive(false); var player=playerObject.AddComponent<MyPlayer>(); var movement=playerObject.AddComponent<PlayerMovement>(); player.playerMovement=movement; var orientation=new GameObject("orientation"); movement.orientation=orientation.transform; orientation.transform.rotation=Quaternion.Euler(0,70,0);
        var cameraObject=new GameObject("map camera"); var camera=cameraObject.AddComponent<Camera>(); camera.transform.rotation=Quaternion.Euler(90,35,0); player.minimapCamera=camera; var previous=MyPlayer.Instance; MyPlayer.Instance=player;
        var markerObject=new GameObject("player marker"); var marker=markerObject.AddComponent<MinimapPlayerIcon>();
        try {
            icon.isStatic=false; icon.alignToCamera=false; Call(icon,"Start"); Rotation(icon.transform,Quaternion.Euler(90,0,0),"icon starts facing upwards");
            icon.isStatic=true; icon.alignToCamera=true; Call(icon,"Update"); Rotation(icon.transform,Quaternion.Euler(90,0,-camera.transform.eulerAngles.y),"camera-aligned icon");
            icon.isStatic=false; Call(icon,"Update"); Rotation(icon.transform,Quaternion.Euler(90,0,0),"non-static icon follows shipped reset");
            Call(marker,"Update"); Rotation(marker.transform,Quaternion.Euler(90,70,0),"player marker follows movement yaw");
            MyPlayer.Instance=null; Call(marker,"Update"); Rotation(marker.transform,Quaternion.Euler(90,70,0),"marker tolerates absent player");
            Debug.Log("MinimapIconPortCheck passed: original shrine icon and player marker orientation");
        } finally { MyPlayer.Instance=previous; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(orientation); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(markerObject); }
    }
}
