using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AnimatedOutlineComponentCheck
{
    static void Check(bool ok,string msg) { if (!ok) throw new Exception(msg); }
    public static void Run()
    {
        try {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_animatedOutlineChallenge.mat");
            Check(source && source.shader.name=="UI/AnimatedOutline","challenge material");
            var canvas=new GameObject("outline canvas",typeof(RectTransform),typeof(Canvas));
            var go=new GameObject("outline",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(AnimatedOutline));go.transform.SetParent(canvas.transform,false);
            var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(240,60);
            var material=new Material(source);go.GetComponent<Image>().material=material;
            var start=typeof(AnimatedOutline).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic);
            Canvas.ForceUpdateCanvases();start.Invoke(go.GetComponent<AnimatedOutline>(),null);
            Vector4 value=material.GetVector("_RectSize");
            Check(Mathf.Abs(value.x-240)<.01f && Mathf.Abs(value.y-60)<.01f && value.z==0 && value.w==0,"rect size: "+value);
            rect.sizeDelta=new Vector2(85,170);start.Invoke(go.GetComponent<AnimatedOutline>(),null);value=material.GetVector("_RectSize");
            Check(Mathf.Abs(value.x-85)<.01f && Mathf.Abs(value.y-170)<.01f,"second rect size: "+value);
            var empty=new GameObject("missing image",typeof(AnimatedOutline));start.Invoke(empty.GetComponent<AnimatedOutline>(),null);
            int originalCount=0;
            foreach (string path in new[]{"Assets/ScenesAndroid/GeneratedMap.unity","Assets/ScenesAndroid/FinalBossMap.unity","Assets/ScenesAndroid/MainMenu.unity"}) {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                foreach(var outline in Resources.FindObjectsOfTypeAll<AnimatedOutline>()) {
                    if(outline.gameObject.scene != scene) continue;
                    var actual=outline.GetComponent<Image>(); var graphic=outline.GetComponent<Graphic>();
                    Debug.Log("AnimatedOutline original scene: "+path+" object="+outline.name+" graphic="+(graphic ? graphic.GetType().Name : "missing")+" material="+(graphic && graphic.material ? graphic.material.name : "missing"));
                    Check(actual,"scene outline has no Image: "+outline.name);originalCount++;
                }
            }
            Check(originalCount>0,"no original scene bindings");
            Debug.Log("AnimatedOutlineComponentCheck passed: actual challenge material, two rectangle aspect ratios, missing image guard, original scene Image bindings="+originalCount);EditorApplication.Exit(0);
        } catch(Exception e) {Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
