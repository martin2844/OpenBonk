using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class XpBarEventsCheck
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static void Run()
    {
        try {
            var scene=EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity",OpenSceneMode.Single);
            XpBarUI original=null;foreach(var candidate in Resources.FindObjectsOfTypeAll<XpBarUI>()) if(candidate.gameObject.scene==scene){original=candidate;break;}
            Check(original && original.xpBar && original.rainbow && original.t_levelText,"actual GeneratedMap XP bar bindings");
            var source=original.rainbow;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var go=new GameObject("xp event check");go.SetActive(false);
            var imageGo=new GameObject("xp bar",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));imageGo.transform.SetParent(go.transform,false);
            var image=imageGo.GetComponent<RawImage>();var baseMaterial=new Material(Shader.Find("UI/Default"));image.material=baseMaterial;
            var component=go.AddComponent<XpBarUI>();component.xpBar=image;component.rainbow=source;
            var type=typeof(XpBarUI);type.GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(component,null);
            image.transform.localScale=new Vector3(.3f,.6f,.7f);
            LevelupScreen.A_LevelupEnabled?.Invoke();Check(image.material==source,"levelup event applies original rainbow material");
            LevelupScreen.isLevelingUp=true;type.GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(component,null);
            LevelupScreen.isLevelingUp=false;LevelupScreen.A_LevelUpClose?.Invoke();
            Check(image.material==baseMaterial && image.transform.localScale==new Vector3(0,1,1),"close restores material and resets bar");
            type.GetMethod("OnDestroy",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(component,null);
            LevelupScreen.A_LevelupEnabled?.Invoke();Check(image.material==baseMaterial,"unsubscribed on destruction");
            Debug.Log("XpBarEventsCheck passed: real scene bindings, levelup/close events, original material restoration, scale and cleanup");EditorApplication.Exit(0);
        } catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
